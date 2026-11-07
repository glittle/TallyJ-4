using Backend.Configuration;
using Backend.Context;
using Backend.DTOs.Elections;
using Backend.Entities;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Services;

/// <summary>
/// Explains to the election owner why SMS, voice, and WhatsApp cannot send.
/// </summary>
public class PaidChannelStatusService : IPaidChannelStatusService
{
    private readonly MainDbContext _context;
    private readonly ICodeSendClock _clock;
    private readonly AntiAbuseOptions _options;

    /// <summary>
    /// Initializes the status reader.
    /// </summary>
    public PaidChannelStatusService(
        MainDbContext context,
        ICodeSendClock clock,
        IOptions<AntiAbuseOptions> options)
    {
        _context = context;
        _clock = clock;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task ApplyAsync(Guid electionGuid, ElectionDto dto, CancellationToken cancellationToken = default)
    {
        var control = await _context.ElectionSendControls
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.ElectionGuid == electionGuid, cancellationToken);

        var allowance = control?.AllowanceOverride ?? _options.ResolvedElectionPaidSendAllowance;
        dto.PaidSendsUsed = control?.PaidSendsUsed ?? 0;
        dto.PaidSendAllowance = allowance;
        dto.OnlineVotingSuspended = control?.Flagged == true;

        if (control?.Flagged == true)
        {
            dto.PaidChannelBlockReason = PaidChannelStatus.Flagged;
            return;
        }

        var ownerIds = await _context.JoinElectionUsers
            .AsNoTracking()
            .Where(link =>
                link.ElectionGuid == electionGuid
                && (link.Role == "Owner" || link.Role == "Admin"))
            .Select(link => link.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var owners = ownerIds.Count == 0
            ? new List<OwnerSendControlLite>()
            : await _context.OwnerSendControls
                .AsNoTracking()
                .Where(row => ownerIds.Contains(row.UserId))
                .Select(row => new OwnerSendControlLite(
                    row.UserId,
                    row.PaidSendsApproved,
                    row.SendsFrozen,
                    row.RestrictionStatus,
                    row.DailyCapOverride))
                .ToListAsync(cancellationToken);

        var trusts = ownerIds
            .Select(id =>
            {
                var owner = owners.FirstOrDefault(row => row.UserId == id);
                return owner == null
                    ? new OwnerRestrictionQueries.OwnerTrust(id, null, false, false)
                    : new OwnerRestrictionQueries.OwnerTrust(
                        owner.UserId,
                        owner.RestrictionStatus,
                        owner.PaidSendsApproved,
                        owner.SendsFrozen);
            })
            .ToList();
        if (OwnerRestrictionQueries.ElectionIsOwnerRestricted(trusts))
        {
            dto.PaidChannelBlockReason = PaidChannelStatus.OwnerRestricted;
            dto.OnlineVotingSuspended = true;
            return;
        }

        if (control?.SendsFrozen == true
            || ownerIds.Any(id => owners.Any(owner => owner.UserId == id && owner.SendsFrozen)))
        {
            dto.PaidChannelBlockReason = PaidChannelStatus.Frozen;
            return;
        }

        var approvedIds = trusts.Where(owner => owner.IsApproved && owner.PaidSendsApproved)
            .Select(owner => owner.UserId)
            .ToList();
        if (approvedIds.Count == 0)
        {
            dto.PaidChannelBlockReason = PaidChannelStatus.NotApproved;
            return;
        }

        if (dto.PaidSendsUsed >= allowance)
        {
            dto.PaidChannelBlockReason = PaidChannelStatus.ElectionCap;
            return;
        }

        var today = _clock.UtcDate;
        var counts = await _context.OwnerDailyPaidSends
            .AsNoTracking()
            .Where(row => approvedIds.Contains(row.UserId) && row.UtcDate == today)
            .ToDictionaryAsync(row => row.UserId, row => row.SendCount, cancellationToken);

        var anyUnderCap = approvedIds.Any(id =>
        {
            var cap = owners.First(owner => owner.UserId == id).DailyCapOverride
                ?? _options.ResolvedOwnerDailyPaidSendCap;
            counts.TryGetValue(id, out var used);
            return used < cap;
        });
        if (!anyUnderCap)
        {
            dto.PaidChannelBlockReason = PaidChannelStatus.OwnerDailyCap;
        }
    }

    private sealed record OwnerSendControlLite(
        Guid UserId,
        bool PaidSendsApproved,
        bool SendsFrozen,
        string? RestrictionStatus,
        int? DailyCapOverride);
}
