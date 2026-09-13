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

        foreach (var personGuid in requested)
        {
            if (!byGuid.TryGetValue(personGuid, out var person))
            {
                results.Add(Result(personGuid, WhatsAppNotifyOutcome.SkippedOtherElection));
                continue;
            }

            if (string.IsNullOrWhiteSpace(person.Phone))
            {
                results.Add(Result(personGuid, WhatsAppNotifyOutcome.SkippedNoPhone));
                continue;
            }

            var row = await OnlineVoterPhoneHelper.FindPhoneOnlineVoterAsync(
                db, person.Phone, cancellationToken);
            // Identifier gate: send only on VoterId + VoterIdType == "P". Do not convert.
            if (row == null || row.VoterIdType != OnlineVoterPhoneHelper.PhoneVoterIdType)
            {
                results.Add(Result(personGuid, WhatsAppNotifyOutcome.SkippedNonP));
                continue;
            }

            // SmsStatus is not the WhatsApp allow rule.
            if (!OnlineVoterWhatsAppStatus.AllowsNotify(row.WhatsAppStatus))
            {
                results.Add(Result(personGuid, WhatsAppNotifyOutcome.SkipForStatus(row.WhatsAppStatus)));
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
            results.Add(Result(personGuid, WhatsAppNotifyOutcome.Queued));
        }

        var run = new NotifyRun
        {
            QueueToken = Guid.NewGuid().ToString("N"),
            ElectionGuid = electionGuid,
            Results = results,
            Work = work,
            Queued = work.Count,
            Skipped = results.Count(r => WhatsAppNotifyOutcome.IsSkip(r.Outcome))
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
            var token = run.Cts.Token;
            run.ProcessTask = Task.Run(() => ProcessAsync(run, token), CancellationToken.None);
        }

        return Snapshot(run);
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

                GreenApiWhatsAppSendResult send;
                try
                {
                    send = await client.SendMessageAsync(item.Phone, item.Message, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    MarkRemainingCancelled(run, item.PersonGuid);
                    break;
                }

                if (!send.ProviderCalled || !send.Sent)
                {
                    lock (run.Sync)
                    {
                        SetOutcome(run, item.PersonGuid, WhatsAppNotifyOutcome.Failed);
                        run.Failed++;
                    }

                    providerCalls++;
                    continue;
                }

                await TryPersistSmsLogAsync(db, send.MessageId, item.Phone, run.ElectionGuid, item.PersonGuid);
                lock (run.Sync)
                {
                    SetOutcome(run, item.PersonGuid, WhatsAppNotifyOutcome.Sent);
                    run.Sent++;
                }

                providerCalls++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WhatsApp notify queue failed for election {ElectionGuid}", run.ElectionGuid);
            MarkRemainingCancelled(run, remainingFromFirstUnsent: true);
        }
        finally
        {
            run.Running = false;
            run.Cts.Dispose();
        }
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

    private static void SetOutcome(NotifyRun run, Guid personGuid, string outcome)
    {
        var row = run.Results.FirstOrDefault(r => r.PersonGuid == personGuid);
        if (row != null)
        {
            row.Outcome = outcome;
        }
    }

    private static WhatsAppNotifyPersonResultDto Result(Guid personGuid, string outcome) =>
        new() { PersonGuid = personGuid, Outcome = outcome };

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
                    Outcome = r.Outcome
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
        public bool Running { get; set; }
        public bool Cancelled { get; set; }
        public CancellationTokenSource Cts { get; } = new();
        public Task? ProcessTask { get; set; }
    }

    private sealed record NotifyWorkItem(Guid PersonGuid, string Phone, string Message);
}
