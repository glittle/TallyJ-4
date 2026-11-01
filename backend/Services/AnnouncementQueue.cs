using System.Collections.Concurrent;
using Backend.Context;
using Backend.DTOs.People;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Backend.Services;

/// <summary>
/// In-memory email and SMS announcement queue. SMS pauses 3–15s between Twilio calls.
/// Email pauses briefly. Abort stops remaining sends only.
/// Each SMS attempt calls <see cref="ICodeSendGuard.ReserveAsync"/> before Twilio.
/// A provider failure is not refunded. Email is not counted against the paid caps.
/// Freeze and a flagged election block both channels.
/// </summary>
public class AnnouncementQueue : IAnnouncementQueue
{
    internal const int SmsDelayMinSeconds = 3;
    internal const int SmsDelayMaxSeconds = 15;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AnnouncementQueue> _logger;
    private readonly Func<string, CancellationToken, Task> _delayBetweenSends;
    private readonly ConcurrentDictionary<string, NotifyRun> _runs = new();
    private readonly ConcurrentDictionary<string, object> _locks = new();

    public AnnouncementQueue(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<AnnouncementQueue> logger,
        Func<string, CancellationToken, Task>? delayBetweenSends = null)
    {
        _scopeFactory = scopeFactory;
        _configuration = configuration;
        _logger = logger;
        _delayBetweenSends = delayBetweenSends ?? DefaultDelayBetweenSends;
    }

    private static Task DefaultDelayBetweenSends(string channel, CancellationToken cancellationToken)
    {
        if (channel == AnnouncementChannels.Email)
        {
            return Task.Delay(TimeSpan.FromMilliseconds(200), cancellationToken);
        }

        var seconds = Random.Shared.Next(SmsDelayMinSeconds, SmsDelayMaxSeconds + 1);
        return Task.Delay(TimeSpan.FromSeconds(seconds), cancellationToken);
    }

    /// <inheritdoc />
    public async Task<AnnouncementStatusDto> StartAsync(
        Guid electionGuid,
        string channel,
        IReadOnlyList<Guid> personGuids,
        CancellationToken cancellationToken = default)
    {
        if (!AnnouncementChannels.IsDirect(channel))
        {
            throw new InvalidOperationException(PeopleMessageKeys.AnnouncementChannelInvalid);
        }

        if (personGuids.Count > StartAnnouncementDto.MaxSelectedPeople)
        {
            throw new InvalidOperationException(PeopleMessageKeys.AnnouncementTooMany);
        }

        var key = RunKey(electionGuid, channel);
        var runLock = _locks.GetOrAdd(key, _ => new object());
        lock (runLock)
        {
            if (_runs.TryGetValue(key, out var existing) && existing.Running)
            {
                throw new InvalidOperationException(PeopleMessageKeys.AnnouncementAlreadyRunning);
            }
        }

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        if (!await SenderConfiguredAsync(scope.ServiceProvider, channel))
        {
            throw new InvalidOperationException(channel == AnnouncementChannels.Email
                ? PeopleMessageKeys.AnnouncementEmailNotConfigured
                : PeopleMessageKeys.AnnouncementSmsNotConfigured);
        }

        var election = await db.Elections
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid, cancellationToken);
        if (election == null)
        {
            throw new InvalidOperationException(PeopleMessageKeys.AnnouncementElectionNotFound);
        }

        var template = channel == AnnouncementChannels.Email ? election.EmailText : election.SmsText;
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new InvalidOperationException(PeopleMessageKeys.AnnouncementTextNotSet);
        }

        var requested = personGuids.Distinct().ToList();
        var people = await db.People
            .Where(p => p.ElectionGuid == electionGuid && requested.Contains(p.PersonGuid))
            .ToListAsync(cancellationToken);
        var byGuid = people.ToDictionary(p => p.PersonGuid);
        var hostSite = _configuration["ClientEnv:frontendUrl"]?.Trim() ?? "";
        var subject = string.IsNullOrWhiteSpace(election.Name) ? "Announcement" : election.Name.Trim();

        var work = new List<WorkItem>();
        var results = new List<AnnouncementPersonResultDto>(requested.Count);
        var failedMasked = new List<string>();

        foreach (var personGuid in requested)
        {
            if (!byGuid.TryGetValue(personGuid, out var person))
            {
                results.Add(Result(personGuid, WhatsAppNotifyOutcome.SkippedOtherElection, null));
                continue;
            }

            if (channel == AnnouncementChannels.Email)
            {
                await ClassifyEmailAsync(
                    db, election, person, template, hostSite, subject, work, results, failedMasked, cancellationToken);
            }
            else
            {
                await ClassifySmsAsync(
                    db, electionGuid, person, template, hostSite, work, results, failedMasked, cancellationToken);
            }
        }

        var run = new NotifyRun
        {
            QueueToken = Guid.NewGuid().ToString("N"),
            ElectionGuid = electionGuid,
            Channel = channel,
            FromAddress = election.EmailFromAddress,
            Subject = subject,
            Results = results,
            Work = work,
            Queued = work.Count,
            Skipped = results.Count(r => WhatsAppNotifyOutcome.IsSkip(r.Outcome) || IsAnnouncementSkip(r.Outcome)),
            FailedMasked = failedMasked
        };

        var startProcess = false;
        lock (runLock)
        {
            if (_runs.TryGetValue(key, out var raced) && raced.Running)
            {
                throw new InvalidOperationException(PeopleMessageKeys.AnnouncementAlreadyRunning);
            }

            startProcess = work.Count > 0;
            run.Running = startProcess;
            _runs[key] = run;
        }

        if (!startProcess)
        {
            BulkSendSupport.LogFailures(_logger, electionGuid, channel, failedMasked);
        }
        else
        {
            var token = run.Cts.Token;
            run.ProcessTask = Task.Run(() => ProcessAsync(run, token), CancellationToken.None);
        }

        return Snapshot(run);
    }

    /// <inheritdoc />
    public async Task<AnnouncementStatusDto> RetryFailedAsync(
        Guid electionGuid,
        string channel,
        CancellationToken cancellationToken = default)
    {
        if (!AnnouncementChannels.IsDirect(channel))
        {
            throw new InvalidOperationException(PeopleMessageKeys.AnnouncementChannelInvalid);
        }

        var failed = await LoadFailedPersonGuidsAsync(electionGuid, channel, cancellationToken);
        if (failed.Count == 0)
        {
            return new AnnouncementStatusDto { Channel = channel };
        }

        return await StartAsync(electionGuid, channel, failed, cancellationToken);
    }

    /// <inheritdoc />
    public AnnouncementStatusDto? Abort(Guid electionGuid, string channel, string? queueToken = null)
    {
        if (!_runs.TryGetValue(RunKey(electionGuid, channel), out var run))
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
    public AnnouncementStatusDto? GetStatus(Guid electionGuid, string channel, string? queueToken = null)
    {
        if (!_runs.TryGetValue(RunKey(electionGuid, channel), out var run))
        {
            return null;
        }

        return TokenMatches(run, queueToken) ? Snapshot(run) : null;
    }

    internal static async Task<List<Guid>> LoadFailedPersonGuidsAsync(
        IServiceScopeFactory scopeFactory,
        Guid electionGuid,
        string channel,
        CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        return await db.PersonLastAnnouncements
            .AsNoTracking()
            .Where(row => row.ElectionGuid == electionGuid
                && row.Channel == channel
                && row.Outcome == WhatsAppNotifyOutcome.Failed)
            .Select(row => row.PersonGuid)
            .Take(StartAnnouncementDto.MaxSelectedPeople + 1)
            .ToListAsync(cancellationToken);
    }

    private Task<List<Guid>> LoadFailedPersonGuidsAsync(
        Guid electionGuid,
        string channel,
        CancellationToken cancellationToken) =>
        LoadFailedPersonGuidsAsync(_scopeFactory, electionGuid, channel, cancellationToken);

    private async Task ClassifyEmailAsync(
        MainDbContext db,
        Election election,
        Person person,
        string template,
        string hostSite,
        string subject,
        List<WorkItem> work,
        List<AnnouncementPersonResultDto> results,
        List<string> failedMasked,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(person.Email))
        {
            results.Add(Result(person.PersonGuid, AnnouncementOutcomes.SkippedNoEmail, null));
            await BulkSendSupport.UpsertLastAsync(
                db, election.ElectionGuid, person.PersonGuid, AnnouncementChannels.Email,
                AnnouncementOutcomes.SkippedNoEmail, null, cancellationToken);
            return;
        }

        if (!MailboxAddress.TryParse(person.Email, out _))
        {
            await RecordPreSkipAsync(
                db, election.ElectionGuid, person.PersonGuid, AnnouncementChannels.Email,
                person.Email, "invalid-email", AnnouncementOutcomes.SkippedInvalidEmail,
                results, failedMasked, cancellationToken);
            return;
        }

        work.Add(new WorkItem(
            person.PersonGuid,
            person.Email,
            WhatsAppNotifyMessage.Fill(template, person.FullNameFl, person.FirstName, person.Email, hostSite),
            subject));
        results.Add(Result(person.PersonGuid, WhatsAppNotifyOutcome.Queued, null));
    }

    private async Task ClassifySmsAsync(
        MainDbContext db,
        Guid electionGuid,
        Person person,
        string template,
        string hostSite,
        List<WorkItem> work,
        List<AnnouncementPersonResultDto> results,
        List<string> failedMasked,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(person.Phone))
        {
            results.Add(Result(person.PersonGuid, WhatsAppNotifyOutcome.SkippedNoPhone, null));
            await BulkSendSupport.UpsertLastAsync(
                db, electionGuid, person.PersonGuid, AnnouncementChannels.Sms,
                WhatsAppNotifyOutcome.SkippedNoPhone, null, cancellationToken);
            return;
        }

        if (!PaidDestinationPhone.TryExplain(person.Phone, out var phoneReason))
        {
            await RecordPreSkipAsync(
                db, electionGuid, person.PersonGuid, AnnouncementChannels.Sms,
                person.Phone, phoneReason ?? PaidDestinationPhone.ReasonMalformedE164,
                AnnouncementOutcomes.SkippedInvalidPhone,
                results, failedMasked, cancellationToken);
            return;
        }

        var row = await OnlineVoterPhoneHelper.FindPhoneOnlineVoterAsync(db, person.Phone, cancellationToken);
        if (row != null && !OnlineVoterSmsStatus.AllowsPaidSend(row.SmsStatus))
        {
            results.Add(Result(person.PersonGuid, AnnouncementOutcomes.SkippedSmsBlocked, row.SmsStatus));
            await BulkSendSupport.UpsertLastAsync(
                db, electionGuid, person.PersonGuid, AnnouncementChannels.Sms,
                AnnouncementOutcomes.SkippedSmsBlocked, row.SmsStatus, cancellationToken);
            return;
        }

        work.Add(new WorkItem(
            person.PersonGuid,
            person.Phone,
            WhatsAppNotifyMessage.Fill(template, person.FullNameFl, person.FirstName, person.Phone, hostSite),
            null));
        results.Add(Result(person.PersonGuid, WhatsAppNotifyOutcome.Queued, null));
    }

    private static async Task RecordPreSkipAsync(
        MainDbContext db,
        Guid electionGuid,
        Guid personGuid,
        string channel,
        string destination,
        string reason,
        string outcome,
        List<AnnouncementPersonResultDto> results,
        List<string> failedMasked,
        CancellationToken cancellationToken)
    {
        results.Add(Result(personGuid, outcome, reason));
        failedMasked.Add(BulkSendSupport.Mask(destination));
        await BulkSendSupport.LogUnreservedBlockAsync(
            db, electionGuid, channel, destination, reason, cancellationToken);
        await BulkSendSupport.UpsertLastAsync(
            db, electionGuid, personGuid, channel, outcome, reason, cancellationToken);
    }

    private async Task ProcessAsync(NotifyRun run, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var guard = scope.ServiceProvider.GetRequiredService<ICodeSendGuard>();
            var sms = scope.ServiceProvider.GetRequiredService<IAnnouncementSmsSender>();
            var email = scope.ServiceProvider.GetRequiredService<IAnnouncementEmailSender>();
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
                        await _delayBetweenSends(run.Channel, cancellationToken);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        MarkRemainingCancelled(run, item.PersonGuid);
                        break;
                    }
                }

                var reservation = await guard.ReserveAsync(
                    [run.ElectionGuid], run.Channel, item.Destination, cancellationToken);
                if (!reservation.Allowed)
                {
                    var error = reservation.BlockReason ?? CodeSendBlockReason.NotApproved;
                    RememberFailure(run, item, error);
                    await BulkSendSupport.UpsertLastAsync(
                        db, run.ElectionGuid, item.PersonGuid, run.Channel,
                        WhatsAppNotifyOutcome.Failed, error, CancellationToken.None);
                    continue;
                }

                AnnouncementProviderResult send;
                try
                {
                    send = run.Channel == AnnouncementChannels.Email
                        ? await email.SendAsync(item.Destination, run.FromAddress, run.Subject, item.Message, cancellationToken)
                        : await sms.SendAsync(item.Destination, item.Message, run.ElectionGuid, item.PersonGuid, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    await guard.LogOutcomeAsync(reservation, run.Channel, item.Destination, sent: false, CancellationToken.None);
                    RememberFailure(run, item, "cancelled");
                    await BulkSendSupport.UpsertLastAsync(
                        db, run.ElectionGuid, item.PersonGuid, run.Channel,
                        WhatsAppNotifyOutcome.Failed, "cancelled", CancellationToken.None);
                    MarkRemainingCancelled(run, NextQueued(run, item.PersonGuid));
                    break;
                }

                providerCalls++;
                if (!send.Sent)
                {
                    var error = send.Error ?? "send-failed";
                    await guard.LogOutcomeAsync(reservation, run.Channel, item.Destination, sent: false, CancellationToken.None);
                    RememberFailure(run, item, error);
                    await BulkSendSupport.UpsertLastAsync(
                        db, run.ElectionGuid, item.PersonGuid, run.Channel,
                        WhatsAppNotifyOutcome.Failed, error, CancellationToken.None);
                    continue;
                }

                await guard.LogOutcomeAsync(reservation, run.Channel, item.Destination, sent: true, CancellationToken.None);
                lock (run.Sync)
                {
                    SetOutcome(run, item.PersonGuid, WhatsAppNotifyOutcome.Sent, null);
                    run.Sent++;
                }

                await BulkSendSupport.UpsertLastAsync(
                    db, run.ElectionGuid, item.PersonGuid, run.Channel,
                    WhatsAppNotifyOutcome.Sent, null, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Announcement queue failed for election {ElectionGuid} channel {Channel}",
                run.ElectionGuid, run.Channel);
            MarkRemainingCancelled(run, remainingFromFirstUnsent: true);
        }
        finally
        {
            BulkSendSupport.LogFailures(_logger, run.ElectionGuid, run.Channel, run.FailedMasked);
            run.Running = false;
            run.Cts.Dispose();
        }
    }

    private static async Task<bool> SenderConfiguredAsync(IServiceProvider provider, string channel)
    {
        if (channel == AnnouncementChannels.Email)
        {
            return provider.GetRequiredService<IAnnouncementEmailSender>().IsConfigured();
        }

        return provider.GetRequiredService<IAnnouncementSmsSender>().IsConfigured();
    }

    private static void RememberFailure(NotifyRun run, WorkItem item, string error)
    {
        lock (run.Sync)
        {
            SetOutcome(run, item.PersonGuid, WhatsAppNotifyOutcome.Failed, error);
            run.Failed++;
            run.FailedMasked.Add(BulkSendSupport.Mask(item.Destination));
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

    private static void MarkRemainingCancelled(NotifyRun run, Guid fromPersonGuid)
    {
        if (fromPersonGuid == Guid.Empty)
        {
            return;
        }

        lock (run.Sync)
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

    private static AnnouncementPersonResultDto Result(Guid personGuid, string outcome, string? error) =>
        new()
        {
            PersonGuid = personGuid,
            Outcome = outcome,
            Error = AnnouncementErrorText.Sanitize(error)
        };

    private static bool IsAnnouncementSkip(string outcome) =>
        outcome is AnnouncementOutcomes.SkippedNoEmail
            or AnnouncementOutcomes.SkippedInvalidEmail
            or AnnouncementOutcomes.SkippedInvalidPhone
            or AnnouncementOutcomes.SkippedSmsBlocked;

    private static bool TokenMatches(NotifyRun run, string? queueToken) =>
        string.IsNullOrWhiteSpace(queueToken)
        || string.Equals(run.QueueToken, queueToken.Trim(), StringComparison.OrdinalIgnoreCase);

    private static string RunKey(Guid electionGuid, string channel) =>
        electionGuid.ToString("N") + ":" + channel;

    private static AnnouncementStatusDto Snapshot(NotifyRun run)
    {
        lock (run.Sync)
        {
            return new AnnouncementStatusDto
            {
                QueueToken = run.QueueToken,
                Channel = run.Channel,
                Running = run.Running,
                Cancelled = run.Cancelled,
                Queued = run.Queued,
                Sent = run.Sent,
                Skipped = run.Skipped,
                Failed = run.Failed,
                Results = run.Results.Select(r => new AnnouncementPersonResultDto
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
        public required string Channel { get; init; }
        public string? FromAddress { get; init; }
        public string Subject { get; init; } = "Announcement";
        public required List<AnnouncementPersonResultDto> Results { get; init; }
        public required List<WorkItem> Work { get; init; }
        public required List<string> FailedMasked { get; init; }
        public int Queued { get; init; }
        public int Skipped { get; set; }
        public int Sent { get; set; }
        public int Failed { get; set; }
        public bool Running { get; set; }
        public bool Cancelled { get; set; }
        public CancellationTokenSource Cts { get; } = new();
        public Task? ProcessTask { get; set; }
    }

    private sealed record WorkItem(Guid PersonGuid, string Destination, string Message, string? Subject);
}

/// <summary>
/// Skip reasons that are not on the WhatsApp notify list.
/// </summary>
public static class AnnouncementOutcomes
{
    public const string SkippedNoEmail = "skipped-no-email";
    public const string SkippedInvalidEmail = "skipped-invalid-email";
    public const string SkippedInvalidPhone = "skipped-invalid-phone";
    public const string SkippedSmsBlocked = "skipped-sms-blocked";
}
