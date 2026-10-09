using Backend.Configuration;
using Backend.Context;
using Backend.Entities;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Services;

/// <summary>
/// Applies the owner approval, election allowance, owner daily cap, freeze, and flag
/// before a login code is sent.
/// </summary>
public class CodeSendGuard : ICodeSendGuard
{
    private readonly MainDbContext _context;
    private readonly PaidSendCounters _counters;
    private readonly IAbuseAlertService _alerts;
    private readonly ICodeSendClock _clock;
    private readonly AntiAbuseOptions _options;
    private readonly ILogger<CodeSendGuard> _logger;
    private readonly IPhonePrefixSendLimiter? _prefixLimits;

    /// <summary>
    /// Initializes the guard.
    /// </summary>
    public CodeSendGuard(
        MainDbContext context,
        PaidSendCounters counters,
        IAbuseAlertService alerts,
        ICodeSendClock clock,
        IOptions<AntiAbuseOptions> options,
        ILogger<CodeSendGuard> logger,
        IPhonePrefixSendLimiter? prefixLimits = null)
    {
        _context = context;
        _counters = counters;
        _alerts = alerts;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
        _prefixLimits = prefixLimits;
    }

    /// <inheritdoc />
    public async Task<CodeSendReservation> ReserveAsync(
        IReadOnlyList<Guid> electionGuids,
        string channel,
        string destination,
        CancellationToken cancellationToken = default)
    {
        if (electionGuids.Count == 0)
        {
            return new CodeSendReservation(false, CodeSendBlockReason.NotApproved, null, null, false);
        }

        var paid = PaidDestinationPhone.IsPaidChannel(channel);
        var names = await _context.Elections
            .AsNoTracking()
            .Where(election => electionGuids.Contains(election.ElectionGuid))
            .Select(election => new { election.ElectionGuid, election.Name })
            .ToDictionaryAsync(election => election.ElectionGuid, election => election.Name, cancellationToken);

        var electionControls = await _context.ElectionSendControls
            .AsNoTracking()
            .Where(row => electionGuids.Contains(row.ElectionGuid))
            .ToDictionaryAsync(row => row.ElectionGuid, cancellationToken);

        var ownerLinks = await _context.JoinElectionUsers
            .AsNoTracking()
            .Where(link =>
                electionGuids.Contains(link.ElectionGuid)
                && (link.Role == "Owner" || link.Role == "Admin"))
            .Select(link => new { link.ElectionGuid, link.UserId })
            .ToListAsync(cancellationToken);

        var ownerIds = ownerLinks.Select(link => link.UserId).Distinct().ToList();
        var ownerControls = ownerIds.Count == 0
            ? new Dictionary<Guid, OwnerSendControl>()
            : await _context.OwnerSendControls
                .AsNoTracking()
                .Where(row => ownerIds.Contains(row.UserId))
                .ToDictionaryAsync(row => row.UserId, cancellationToken);

        var today = _clock.UtcDate;
        var dailyCounts = ownerIds.Count == 0
            ? new Dictionary<Guid, int>()
            : await _context.OwnerDailyPaidSends
                .AsNoTracking()
                .Where(row => ownerIds.Contains(row.UserId) && row.UtcDate == today)
                .ToDictionaryAsync(row => row.UserId, row => row.SendCount, cancellationToken);

        string? lastReason = null;
        Guid? lastElection = electionGuids[0];
        Guid? lastOwner = null;
        var prefixBlocked = false;
        string? blockedPrefix = null;

        foreach (var electionGuid in electionGuids.Distinct())
        {
            lastElection = electionGuid;
            electionControls.TryGetValue(electionGuid, out var electionControl);
            if (electionControl?.SendsFrozen == true)
            {
                lastReason = CodeSendBlockReason.Frozen;
                continue;
            }

            if (electionControl?.Flagged == true)
            {
                lastReason = CodeSendBlockReason.Flagged;
                continue;
            }

            var owners = ownerLinks
                .Where(link => link.ElectionGuid == electionGuid)
                .Select(link => link.UserId)
                .Distinct()
                .Select(userId => ToOwner(userId, ownerControls, dailyCounts))
                .ToList();

            // One frozen owner or admin stops the election. A second account must not keep sending.
            var frozenOwner = owners.FirstOrDefault(owner => owner.SendsFrozen);
            if (frozenOwner != null)
            {
                lastReason = CodeSendBlockReason.Frozen;
                lastOwner = frozenOwner.UserId;
                continue;
            }

            if (!paid)
            {
                return new CodeSendReservation(
                    true,
                    null,
                    electionGuid,
                    owners.FirstOrDefault()?.UserId,
                    false);
            }

            // Every owner and admin must be approved. One approved account must not carry the others.
            var unapproved = owners.FirstOrDefault(owner => !owner.PaidSendsApproved);
            if (owners.Count == 0 || unapproved != null)
            {
                lastReason = CodeSendBlockReason.NotApproved;
                lastOwner = unapproved?.UserId;
                continue;
            }

            var allowance = electionControl?.AllowanceOverride ?? _options.ResolvedElectionPaidSendAllowance;
            if ((electionControl?.PaidSendsUsed ?? 0) >= allowance)
            {
                lastReason = CodeSendBlockReason.ElectionCap;
                await NotifyElectionCapAsync(electionGuid, names, electionControl?.PaidSendsUsed ?? 0, allowance, cancellationToken);
                continue;
            }

            var ownerBlocked = false;
            foreach (var owner in owners)
            {
                lastOwner = owner.UserId;
                var cap = owner.DailyCapOverride ?? _options.ResolvedOwnerDailyPaidSendCap;
                if (owner.TodayCount >= cap)
                {
                    lastReason = CodeSendBlockReason.OwnerDailyCap;
                    ownerBlocked = true;
                    await NotifyOwnerCapAsync(owner.UserId, electionGuid, names, owner.TodayCount, cap, cancellationToken);
                    continue;
                }

                if (!await _counters.TryConsumeElectionAsync(electionGuid, allowance, cancellationToken))
                {
                    lastReason = CodeSendBlockReason.ElectionCap;
                    await NotifyElectionCapAsync(electionGuid, names, allowance, allowance, cancellationToken);
                    ownerBlocked = false;
                    break;
                }

                if (!await _counters.TryConsumeOwnerDayAsync(owner.UserId, today, cap, cancellationToken))
                {
                    await _counters.RefundElectionAsync(electionGuid, cancellationToken);
                    lastReason = CodeSendBlockReason.OwnerDailyCap;
                    ownerBlocked = true;
                    await NotifyOwnerCapAsync(owner.UserId, electionGuid, names, cap, cap, cancellationToken);
                    continue;
                }

                if (_prefixLimits != null)
                {
                    var prefix = await _prefixLimits.TryConsumeAsync(channel, destination, cancellationToken);
                    if (!prefix.Allowed)
                    {
                        await _counters.RefundElectionAsync(electionGuid, cancellationToken);
                        await _counters.RefundOwnerDayAsync(owner.UserId, today, cancellationToken);
                        lastReason = prefix.BlockReason ?? CodeSendBlockReason.PrefixLimit;
                        blockedPrefix = prefix.Prefix;
                        prefixBlocked = true;
                        break;
                    }
                }

                var first = await _counters.TryStampFirstPaidSendAsync(owner.UserId, _clock.UtcNow, cancellationToken);
                if (first)
                {
                    names.TryGetValue(electionGuid, out var electionName);
                    await _alerts.NotifyFirstPaidSendAsync(
                        new AbuseFirstPaidSendAlert(
                            owner.UserId,
                            electionGuid,
                            electionName,
                            channel,
                            DestinationMask.Mask(destination)),
                        cancellationToken);
                }

                _logger.LogInformation(
                    "Paid send reserved for election {ElectionGuid} owner {OwnerUserId} channel {Channel}",
                    electionGuid,
                    owner.UserId,
                    channel);

                return new CodeSendReservation(true, null, electionGuid, owner.UserId, first);
            }

            if (prefixBlocked)
            {
                break;
            }

            if (!ownerBlocked && lastReason == null)
            {
                lastReason = CodeSendBlockReason.NotApproved;
            }
        }

        lastReason ??= CodeSendBlockReason.NotApproved;
        _logger.LogInformation(
            "Login code not sent ({BlockReason}) channel {Channel} election {ElectionGuid}",
            lastReason,
            channel,
            lastElection);

        if (lastReason == CodeSendBlockReason.PrefixLimit && blockedPrefix != null)
        {
            await _alerts.NotifyPrefixLimitAsync(
                new AbusePrefixLimitAlert(
                    blockedPrefix,
                    channel,
                    DestinationMask.Mask(destination),
                    _options.ResolvedPhonePrefixSendLimit,
                    _options.ResolvedPhonePrefixWindowMinutes),
                cancellationToken);
        }

        var blocked = new CodeSendReservation(false, lastReason, lastElection, lastOwner, false);
        await LogOutcomeAsync(blocked, channel, destination, sent: false, cancellationToken);
        return blocked;
    }

    /// <inheritdoc />
    public async Task LogOutcomeAsync(
        CodeSendReservation reservation,
        string channel,
        string destination,
        bool sent,
        CancellationToken cancellationToken = default)
    {
        var outcome = reservation.Allowed
            ? (sent ? CodeSendBlockReason.Sent : CodeSendBlockReason.SendFailed)
            : CodeSendBlockReason.Blocked(reservation.BlockReason ?? CodeSendBlockReason.NotApproved);

        _context.CodeSendLogs.Add(new CodeSendLog
        {
            ElectionGuid = reservation.ElectionGuid,
            OwnerUserId = reservation.OwnerUserId,
            Channel = channel,
            MaskedDestination = DestinationMask.Mask(destination),
            Outcome = outcome,
            SentAt = _clock.UtcNow
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifyElectionCapAsync(
        Guid electionGuid,
        Dictionary<Guid, string> names,
        int used,
        int cap,
        CancellationToken cancellationToken)
    {
        names.TryGetValue(electionGuid, out var name);
        await _alerts.NotifyCapHitAsync(
            new AbuseCapHitAlert(
                "cap:election:" + electionGuid.ToString("N"),
                "election",
                electionGuid,
                name,
                null,
                used,
                cap),
            cancellationToken);
    }

    private async Task NotifyOwnerCapAsync(
        Guid ownerUserId,
        Guid electionGuid,
        Dictionary<Guid, string> names,
        int used,
        int cap,
        CancellationToken cancellationToken)
    {
        names.TryGetValue(electionGuid, out var name);
        await _alerts.NotifyCapHitAsync(
            new AbuseCapHitAlert(
                "cap:owner:" + ownerUserId.ToString("N") + ":" + _clock.UtcDate.ToString("yyyyMMdd"),
                "owner-daily",
                electionGuid,
                name,
                ownerUserId,
                used,
                cap),
            cancellationToken);
    }

    private static OwnerSnapshot ToOwner(
        Guid userId,
        Dictionary<Guid, OwnerSendControl> controls,
        Dictionary<Guid, int> dailyCounts)
    {
        controls.TryGetValue(userId, out var control);
        dailyCounts.TryGetValue(userId, out var today);
        return new OwnerSnapshot(
            userId,
            control?.PaidSendsApproved == true,
            control?.SendsFrozen == true,
            control?.DailyCapOverride,
            today);
    }

    private sealed record OwnerSnapshot(
        Guid UserId,
        bool PaidSendsApproved,
        bool SendsFrozen,
        int? DailyCapOverride,
        int TodayCount);
}
