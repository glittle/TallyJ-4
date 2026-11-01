using System.Collections.Concurrent;
using Backend.Context;
using Backend.DTOs.People;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// In-memory WhatsApp notify queue. Sequential GreenAPI <c>sendMessage</c> with
/// 3–15s jitter between provider calls. Abort stops remaining sends only.
/// </summary>
public class WhatsAppNotifyQueue : IWhatsAppNotifyQueue
{
    /// <summary>
    /// Inclusive lower bound (seconds) for the pause between notify sends (v3 jitter).
    /// </summary>
    internal const int SendDelayMinSeconds = 3;

    /// <summary>
    /// Inclusive upper bound (seconds) for the pause between notify sends.
    /// </summary>
    internal const int SendDelayMaxSeconds = 15;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WhatsAppNotifyQueue> _logger;
    private readonly Func<CancellationToken, Task> _delayBetweenSends;
    private readonly Func<CancellationToken, Task>? _beforeClaim;
    private readonly ConcurrentDictionary<Guid, NotifyRun> _runs = new();
    private readonly ConcurrentDictionary<Guid, object> _electionLocks = new();

    public WhatsAppNotifyQueue(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<WhatsAppNotifyQueue> logger,
        Func<CancellationToken, Task>? delayBetweenSends = null,
        Func<CancellationToken, Task>? beforeClaim = null)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _delayBetweenSends = delayBetweenSends ?? DefaultDelayBetweenSends;
        _beforeClaim = beforeClaim;
    }

    private static Task DefaultDelayBetweenSends(CancellationToken cancellationToken)
    {
        var seconds = Random.Shared.Next(SendDelayMinSeconds, SendDelayMaxSeconds + 1);
        return Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<WhatsAppNotifyStatusDto> StartAsync(
        Guid electionGuid,
        IReadOnlyList<Guid> personGuids,
        CancellationToken cancellationToken = default)
    {
        if (personGuids.Count > StartWhatsAppNotifyDto.MaxSelectedPeople)
        {
            throw new InvalidOperationException(PeopleMessageKeys.PhoneWhatsAppTooMany);
        }

        var electionLock = _electionLocks.GetOrAdd(electionGuid, _ => new object());
        lock (electionLock)
        {
            if (_runs.TryGetValue(electionGuid, out var existing) && existing.Running)
            {
                throw new InvalidOperationException(PeopleMessageKeys.WhatsAppNotifyAlreadyRunning);
            }
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var client = scope.ServiceProvider.GetRequiredService<IGreenApiWhatsAppClient>();

        var election = await db.Elections
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid, cancellationToken);
        if (election == null)
        {
            throw new InvalidOperationException(PeopleMessageKeys.WhatsAppNotifyElectionNotFound);
        }

        if (string.IsNullOrWhiteSpace(election.SmsText))
        {
            throw new InvalidOperationException(PeopleMessageKeys.WhatsAppNotifyTextNotSet);
        }

        if (!client.IsConfigured())
        {
            throw new InvalidOperationException(PeopleMessageKeys.PhoneWhatsAppNotConfigured);
        }

        var requested = personGuids.Distinct().ToList();
        var people = await db.People
            .Where(p => p.ElectionGuid == electionGuid && requested.Contains(p.PersonGuid))
            .ToListAsync(cancellationToken);
        var byGuid = people.ToDictionary(p => p.PersonGuid);

        var hostSite = _configuration["ClientEnv:frontendUrl"]?.Trim() ?? "";
        var work = new List<NotifyWorkItem>();
        var results = new List<WhatsAppNotifyPersonResultDto>(requested.Count);
        var failedMasked = new List<string>();

        foreach (var personGuid in requested)
        {
            if (!byGuid.TryGetValue(personGuid, out var person))
            {
                results.Add(Result(personGuid, WhatsAppNotifyOutcome.SkippedOtherElection, null));
                continue;
            }

            if (string.IsNullOrWhiteSpace(person.Phone))
            {
                await RememberSkipAsync(db, electionGuid, personGuid, WhatsAppNotifyOutcome.SkippedNoPhone, null, results, cancellationToken);
                continue;
            }

            var row = await OnlineVoterPhoneHelper.FindPhoneOnlineVoterAsync(
                db, person.Phone, cancellationToken);
            // Identifier gate: send only on VoterId + VoterIdType == "P". Do not convert.
            if (row == null || row.VoterIdType != OnlineVoterPhoneHelper.PhoneVoterIdType)
            {
                await RememberSkipAsync(db, electionGuid, personGuid, WhatsAppNotifyOutcome.SkippedNonP, null, results, cancellationToken);
                continue;
            }

            // SmsStatus is not the WhatsApp allow rule.
            if (!OnlineVoterWhatsAppStatus.AllowsNotify(row.WhatsAppStatus))
            {
                var skip = WhatsAppNotifyOutcome.SkipForStatus(row.WhatsAppStatus);
                await RememberSkipAsync(db, electionGuid, personGuid, skip, row.WhatsAppStatus, results, cancellationToken);
                continue;
            }

            if (!PaidDestinationPhone.TryExplain(person.Phone, out var phoneReason))
            {
                var reason = phoneReason ?? PaidDestinationPhone.ReasonMalformedE164;
                results.Add(Result(personGuid, AnnouncementOutcomes.SkippedInvalidPhone, reason));
                failedMasked.Add(BulkSendSupport.Mask(person.Phone));
                await BulkSendSupport.LogUnreservedBlockAsync(
                    db, electionGuid, AnnouncementChannels.WhatsApp, person.Phone, reason, cancellationToken);
                await BulkSendSupport.UpsertLastAsync(
                    db, electionGuid, personGuid, AnnouncementChannels.WhatsApp,
                    AnnouncementOutcomes.SkippedInvalidPhone, reason, cancellationToken);
                continue;
            }

            work.Add(new NotifyWorkItem(
                personGuid,
                person.Phone,
                WhatsAppNotifyMessage.Fill(
                    election.SmsText,
                    person.FullNameFl,
                    person.FirstName,
                    person.Phone,
                    hostSite)));
            results.Add(Result(personGuid, WhatsAppNotifyOutcome.Queued, null));
        }

        var run = new NotifyRun
        {
            QueueToken = Guid.NewGuid().ToString("N"),
            ElectionGuid = electionGuid,
            Results = results,
            Work = work,
            Queued = work.Count,
            Skipped = results.Count(r => WhatsAppNotifyOutcome.IsSkip(r.Outcome)
                || r.Outcome == AnnouncementOutcomes.SkippedInvalidPhone)
        };

        if (_beforeClaim != null)
        {
            await _beforeClaim(cancellationToken);
        }

        var startProcess = false;
        lock (electionLock)
        {
            if (_runs.TryGetValue(electionGuid, out var raced) && raced.Running)
            {
                throw new InvalidOperationException(PeopleMessageKeys.WhatsAppNotifyAlreadyRunning);
            }

            // Claim before publish so a second overlapping Start cannot insert another pipeline.
            startProcess = work.Count > 0;
            run.Running = startProcess;
            _runs[electionGuid] = run;
        }

        if (startProcess)
        {
            run.FailedMasked.AddRange(failedMasked);
            var token = run.Cts.Token;
            run.ProcessTask = Task.Run(() => ProcessAsync(run, token), CancellationToken.None);
        }
        else
        {
            BulkSendSupport.LogFailures(_logger, electionGuid, AnnouncementChannels.WhatsApp, failedMasked);
        }

        return Snapshot(run);
    }

    /// <inheritdoc />
    public async Task<WhatsAppNotifyStatusDto> RetryFailedAsync(
        Guid electionGuid,
        CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var failed = await db.PersonLastAnnouncements
            .AsNoTracking()
            .Where(row => row.ElectionGuid == electionGuid
                && row.Channel == AnnouncementChannels.WhatsApp
                && row.Outcome == WhatsAppNotifyOutcome.Failed)
            .Select(row => row.PersonGuid)
            .Take(StartWhatsAppNotifyDto.MaxSelectedPeople + 1)
            .ToListAsync(cancellationToken);

        if (failed.Count == 0)
        {
            return new WhatsAppNotifyStatusDto();
        }

        return await StartAsync(electionGuid, failed, cancellationToken);
    }

    /// <inheritdoc />
    public WhatsAppNotifyStatusDto? Abort(Guid electionGuid, string? queueToken = null)
    {
        if (!_runs.TryGetValue(electionGuid, out var run))
        {
            return null;
        }

        if (!TokenMatches(run, queueToken))
        {
            return null;
        }

        if (run.Running)
        {
            try
            {
                run.Cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // run already finished
            }
        }

        return Snapshot(run);
    }

    /// <inheritdoc />
    public WhatsAppNotifyStatusDto? GetStatus(Guid electionGuid, string? queueToken = null)
    {
        if (!_runs.TryGetValue(electionGuid, out var run))
        {
            return null;
        }

        return TokenMatches(run, queueToken) ? Snapshot(run) : null;
    }

    private async Task ProcessAsync(NotifyRun run, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var client = scope.ServiceProvider.GetRequiredService<IGreenApiWhatsAppClient>();
            var guard = scope.ServiceProvider.GetRequiredService<ICodeSendGuard>();
            var providerCalls = 0;

            foreach (var item in run.Work)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    MarkRemainingCancelled(run, item.PersonGuid);
                    break;
                }

                if (providerCalls > 0)
                {
                    try
                    {
                        await _delayBetweenSends(cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        MarkRemainingCancelled(run, item.PersonGuid);
                        break;
                    }
                }

                // Reserve before GreenAPI. A blocked attempt is not a provider call and is not refunded
                // because nothing was consumed. A later provider failure keeps the slot.
                var reservation = await guard.ReserveAsync(
                    [run.ElectionGuid], AnnouncementChannels.WhatsApp, item.Phone, cancellationToken);
                if (!reservation.Allowed)
                {
                    var error = reservation.BlockReason ?? CodeSendBlockReason.NotApproved;
                    RememberFailure(run, item.PersonGuid, item.Phone, error);
                    await BulkSendSupport.UpsertLastAsync(
                        db, run.ElectionGuid, item.PersonGuid, AnnouncementChannels.WhatsApp,
                        WhatsAppNotifyOutcome.Failed, error, CancellationToken.None);
                    continue;
                }

                GreenApiWhatsAppSendResult send;
                try
                {
                    send = await client.SendMessageAsync(item.Phone, item.Message, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    await guard.LogOutcomeAsync(
                        reservation, AnnouncementChannels.WhatsApp, item.Phone, sent: false, CancellationToken.None);
                    RememberFailure(run, item.PersonGuid, item.Phone, "cancelled");
                    await BulkSendSupport.UpsertLastAsync(
                        db, run.ElectionGuid, item.PersonGuid, AnnouncementChannels.WhatsApp,
                        WhatsAppNotifyOutcome.Failed, "cancelled", CancellationToken.None);
                    MarkRemainingCancelled(run, NextQueued(run, item.PersonGuid));
                    break;
                }

                providerCalls++;
                if (!send.ProviderCalled || !send.Sent)
                {
                    var error = send.Error ?? "send-failed";
                    await guard.LogOutcomeAsync(
                        reservation, AnnouncementChannels.WhatsApp, item.Phone, sent: false, CancellationToken.None);
                    RememberFailure(run, item.PersonGuid, item.Phone, error);
                    await BulkSendSupport.UpsertLastAsync(
                        db, run.ElectionGuid, item.PersonGuid, AnnouncementChannels.WhatsApp,
                        WhatsAppNotifyOutcome.Failed, error, CancellationToken.None);
                    continue;
                }

                await guard.LogOutcomeAsync(
                    reservation, AnnouncementChannels.WhatsApp, item.Phone, sent: true, CancellationToken.None);
                await TryPersistSmsLogAsync(db, send.MessageId, item.Phone, run.ElectionGuid, item.PersonGuid);
                lock (run.Sync)
                {
                    SetOutcome(run, item.PersonGuid, WhatsAppNotifyOutcome.Sent, null);
                    run.Sent++;
                }

                await BulkSendSupport.UpsertLastAsync(
                    db, run.ElectionGuid, item.PersonGuid, AnnouncementChannels.WhatsApp,
                    WhatsAppNotifyOutcome.Sent, null, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp notify queue failed for election {ElectionGuid}", run.ElectionGuid);
            MarkRemainingCancelled(run, remainingFromFirstUnsent: true);
        }
        finally
        {
            BulkSendSupport.LogFailures(_logger, run.ElectionGuid, AnnouncementChannels.WhatsApp, run.FailedMasked);
            run.Running = false;
            run.Cts.Dispose();
        }
    }

    private static void RememberFailure(NotifyRun run, Guid personGuid, string phone, string error)
    {
        lock (run.Sync)
        {
            SetOutcome(run, personGuid, WhatsAppNotifyOutcome.Failed, error);
            run.Failed++;
            run.FailedMasked.Add(BulkSendSupport.Mask(phone));
        }
    }

    private static Guid NextQueued(NotifyRun run, Guid after)
    {
        lock (run.Sync)
        {
            var seen = false;
            foreach (var row in run.Results)
            {
                if (row.PersonGuid == after)
                {
                    seen = true;
                    continue;
                }

                if (seen && row.Outcome == WhatsAppNotifyOutcome.Queued)
                {
                    return row.PersonGuid;
                }
            }
        }

        return Guid.Empty;
    }

    private async Task TryPersistSmsLogAsync(
        MainDbContext db,
        string? sid,
        string phone,
        Guid electionGuid,
        Guid personGuid)
    {
        var log = SmsLogSendHelper.TryCreate(
            sid,
            phone,
            SmsLogSendHelper.DefaultLastStatus,
            electionGuid,
            personGuid);
        if (log == null)
        {
            _logger.LogWarning("WhatsApp notify send succeeded but SmsLog not written (missing SID)");
            return;
        }

        try
        {
            db.SmsLogs.Add(log);
            await db.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SmsLog insert failed after WhatsApp notify send");
        }
    }

    private static void MarkRemainingCancelled(NotifyRun run, Guid fromPersonGuid)
    {
        if (fromPersonGuid == Guid.Empty)
        {
            return;
        }

        lock (run.Sync)
        {
            MarkRemainingCancelledCore(run, fromPersonGuid);
        }
    }

    private static void MarkRemainingCancelledCore(NotifyRun run, Guid fromPersonGuid)
    {
        run.Cancelled = true;
        var seen = false;
        foreach (var row in run.Results)
        {
            if (row.PersonGuid == fromPersonGuid)
            {
                seen = true;
            }

            if (seen && row.Outcome == WhatsAppNotifyOutcome.Queued)
            {
                row.Outcome = WhatsAppNotifyOutcome.Cancelled;
            }
        }
    }

    private static void MarkRemainingCancelled(NotifyRun run, bool remainingFromFirstUnsent)
    {
        if (!remainingFromFirstUnsent)
        {
            return;
        }

        lock (run.Sync)
        {
            run.Cancelled = true;
            foreach (var row in run.Results.Where(r => r.Outcome == WhatsAppNotifyOutcome.Queued))
            {
                row.Outcome = WhatsAppNotifyOutcome.Cancelled;
            }
        }
    }

    private static void SetOutcome(NotifyRun run, Guid personGuid, string outcome, string? error)
    {
        var row = run.Results.FirstOrDefault(r => r.PersonGuid == personGuid);
        if (row == null)
        {
            return;
        }

        row.Outcome = outcome;
        row.Error = outcome == WhatsAppNotifyOutcome.Failed
            ? AnnouncementErrorText.Sanitize(error) ?? "send-failed"
            : AnnouncementErrorText.Sanitize(error);
    }

    private static async Task RememberSkipAsync(
        MainDbContext db,
        Guid electionGuid,
        Guid personGuid,
        string outcome,
        string? error,
        List<WhatsAppNotifyPersonResultDto> results,
        CancellationToken cancellationToken)
    {
        results.Add(Result(personGuid, outcome, error));
        await BulkSendSupport.UpsertLastAsync(
            db, electionGuid, personGuid, AnnouncementChannels.WhatsApp, outcome, error, cancellationToken);
    }

    private static WhatsAppNotifyPersonResultDto Result(Guid personGuid, string outcome, string? error) =>
        new()
        {
            PersonGuid = personGuid,
            Outcome = outcome,
            Error = AnnouncementErrorText.Sanitize(error)
        };

    private static bool TokenMatches(NotifyRun run, string? queueToken) =>
        string.IsNullOrWhiteSpace(queueToken)
        || string.Equals(run.QueueToken, queueToken.Trim(), StringComparison.OrdinalIgnoreCase);

    private static WhatsAppNotifyStatusDto Snapshot(NotifyRun run)
    {
        lock (run.Sync)
        {
            return new WhatsAppNotifyStatusDto
            {
                QueueToken = run.QueueToken,
                Running = run.Running,
                Cancelled = run.Cancelled,
                Queued = run.Queued,
                Sent = run.Sent,
                Skipped = run.Skipped,
                Failed = run.Failed,
                Results = run.Results.Select(r => new WhatsAppNotifyPersonResultDto
                {
                    PersonGuid = r.PersonGuid,
                    Outcome = r.Outcome,
                    Error = r.Error
                }).ToList()
            };
        }
    }

    private sealed class NotifyRun
    {
        public object Sync { get; } = new();
        public required string QueueToken { get; init; }
        public required Guid ElectionGuid { get; init; }
        public required List<WhatsAppNotifyPersonResultDto> Results { get; init; }
        public required List<NotifyWorkItem> Work { get; init; }
        public int Queued { get; init; }
        public int Skipped { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public List<string> FailedMasked { get; } = [];
        public bool Running { get; set; }
        public bool Cancelled { get; set; }
        public CancellationTokenSource Cts { get; } = new();
        public Task? ProcessTask { get; set; }
    }

    private sealed record NotifyWorkItem(Guid PersonGuid, string Phone, string Message);
}
