using Backend.Configuration;
using Backend.Context;
using Backend.DTOs.Security;
using Backend.DTOs.SuperAdmin;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Services;

/// <summary>
/// Super-admin paid-send approval, cap, and freeze actions.
/// </summary>
public class PaidSendAdminService : IPaidSendAdminService
{
    private readonly MainDbContext _context;
    private readonly ISecurityAuditService _audit;
    private readonly ICodeSendClock _clock;
    private readonly AntiAbuseOptions _options;
    private readonly ILogger<PaidSendAdminService> _logger;

    /// <summary>
    /// Initializes the admin actions.
    /// </summary>
    public PaidSendAdminService(
        MainDbContext context,
        ISecurityAuditService audit,
        ICodeSendClock clock,
        IOptions<AntiAbuseOptions> options,
        ILogger<PaidSendAdminService> logger)
    {
        _context = context;
        _audit = audit;
        _clock = clock;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PaidSendAdminOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default)
    {
        var links = await _context.JoinElectionUsers
            .AsNoTracking()
            .Where(link => link.Role == "Owner" || link.Role == "Admin")
            .Select(link => new { link.UserId, link.ElectionGuid })
            .ToListAsync(cancellationToken);

        var ownerIds = links.Select(link => link.UserId).Distinct().ToList();
        var controls = await _context.OwnerSendControls
            .AsNoTracking()
            .Where(row => ownerIds.Contains(row.UserId))
            .ToDictionaryAsync(row => row.UserId, cancellationToken);

        var ownerIdStrings = ownerIds.Select(id => id.ToString()).ToList();
        var users = await _context.Users
            .AsNoTracking()
            .Where(user => ownerIdStrings.Contains(user.Id))
            .Select(user => new { user.Id, user.Email, user.DisplayName, user.UserName })
            .ToListAsync(cancellationToken);
        var usersById = users.ToDictionary(user => user.Id, StringComparer.OrdinalIgnoreCase);

        var pending = ownerIds
            .Where(id => !controls.TryGetValue(id, out var control) || !control.PaidSendsApproved)
            .Select(id =>
            {
                usersById.TryGetValue(id.ToString(), out var user);
                return new PendingPaidSendOwnerDto
                {
                    UserId = id,
                    Email = user?.Email,
                    DisplayName = user?.DisplayName ?? user?.UserName,
                    ElectionCount = links.Count(link => link.UserId == id)
                };
            })
            .OrderBy(item => item.Email)
            .ToList();

        var electionControls = await _context.ElectionSendControls
            .AsNoTracking()
            .ToListAsync(cancellationToken);
        var electionIds = electionControls.Select(row => row.ElectionGuid).ToList();
        var electionNames = await _context.Elections
            .AsNoTracking()
            .Where(election => electionIds.Contains(election.ElectionGuid))
            .Select(election => new { election.ElectionGuid, election.Name })
            .ToDictionaryAsync(election => election.ElectionGuid, election => election.Name, cancellationToken);

        var capHits = new List<PaidSendCapHitDto>();
        foreach (var election in electionControls)
        {
            var allowance = election.AllowanceOverride ?? _options.ResolvedElectionPaidSendAllowance;
            if (election.PaidSendsUsed < allowance)
            {
                continue;
            }

            electionNames.TryGetValue(election.ElectionGuid, out var name);
            capHits.Add(new PaidSendCapHitDto
            {
                Scope = "election",
                ElectionGuid = election.ElectionGuid,
                ElectionName = name,
                Used = election.PaidSendsUsed,
                Cap = allowance
            });
        }

        var today = _clock.UtcDate;
        var daily = await _context.OwnerDailyPaidSends
            .AsNoTracking()
            .Where(row => row.UtcDate == today)
            .ToListAsync(cancellationToken);
        foreach (var row in daily)
        {
            controls.TryGetValue(row.UserId, out var owner);
            var cap = owner?.DailyCapOverride ?? _options.ResolvedOwnerDailyPaidSendCap;
            if (row.SendCount < cap)
            {
                continue;
            }

            usersById.TryGetValue(row.UserId.ToString(), out var user);
            capHits.Add(new PaidSendCapHitDto
            {
                Scope = "owner-daily",
                OwnerUserId = row.UserId,
                OwnerEmail = user?.Email,
                Used = row.SendCount,
                Cap = cap
            });
        }

        var frozenElections = electionControls
            .Where(row => row.SendsFrozen)
            .Select(row =>
            {
                electionNames.TryGetValue(row.ElectionGuid, out var name);
                return new FrozenSendElectionDto
                {
                    ElectionGuid = row.ElectionGuid,
                    Name = name ?? row.ElectionGuid.ToString()
                };
            })
            .ToList();

        var frozenOwners = controls.Values
            .Where(row => row.SendsFrozen)
            .Select(row =>
            {
                usersById.TryGetValue(row.UserId.ToString(), out var user);
                return new FrozenSendOwnerDto
                {
                    UserId = row.UserId,
                    Email = user?.Email,
                    DisplayName = user?.DisplayName ?? user?.UserName
                };
            })
            .ToList();

        var flaggedIds = electionControls.Where(row => row.Flagged).Select(row => row.ElectionGuid).ToList();
        var flagRows = flaggedIds.Count == 0
            ? new List<VoterContactFlag>()
            : await _context.VoterContactFlags
                .AsNoTracking()
                .Where(row => flaggedIds.Contains(row.ElectionGuid) && row.Active)
                .ToListAsync(cancellationToken);
        var flagged = electionControls
            .Where(row => row.Flagged)
            .Select(row =>
            {
                electionNames.TryGetValue(row.ElectionGuid, out var name);
                return new FlaggedElectionDto
                {
                    ElectionGuid = row.ElectionGuid,
                    Name = name ?? row.ElectionGuid.ToString(),
                    FlaggedAt = row.FlaggedAt,
                    Rows = flagRows
                        .Where(flag => flag.ElectionGuid == row.ElectionGuid)
                        .Select(flag => new FlaggedVoterContactDto
                        {
                            RowNumber = flag.SourceRowNumber,
                            MaskedValue = flag.MaskedValue,
                            Reason = flag.Reason
                        })
                        .ToList()
                };
            })
            .ToList();

        return new PaidSendAdminOverviewDto
        {
            PendingOwners = pending,
            CapHits = capHits,
            FrozenElections = frozenElections,
            FrozenOwners = frozenOwners,
            FlaggedElections = flagged
        };
    }

    /// <inheritdoc />
    public async Task<bool> ApproveOwnerAsync(Guid userId, string adminUserId, CancellationToken cancellationToken = default)
    {
        var known = await _context.JoinElectionUsers
            .AnyAsync(link =>
                link.UserId == userId && (link.Role == "Owner" || link.Role == "Admin"),
                cancellationToken);
        if (!known)
        {
            return false;
        }

        var row = await GetOrAddOwnerAsync(userId, cancellationToken);
        row.PaidSendsApproved = true;
        row.ApprovedAt = _clock.UtcNow;
        row.ApprovedByUserId = adminUserId;
        await _context.SaveChangesAsync(cancellationToken);
        await AuditAsync(SecurityEventType.PaidSendOwnerApproved, adminUserId, null, "Paid sends approved for owner " + userId);
        _logger.LogInformation("Paid sends approved for owner {OwnerUserId}", userId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SetOwnerFrozenAsync(
        Guid userId,
        bool frozen,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var known = await _context.Users.AnyAsync(user => user.Id == userId.ToString(), cancellationToken)
            || await _context.JoinElectionUsers.AnyAsync(link => link.UserId == userId, cancellationToken);
        if (!known)
        {
            return false;
        }

        var row = await GetOrAddOwnerAsync(userId, cancellationToken);
        row.SendsFrozen = frozen;
        await _context.SaveChangesAsync(cancellationToken);
        await AuditAsync(
            frozen ? SecurityEventType.PaidSendFrozen : SecurityEventType.PaidSendUnfrozen,
            adminUserId,
            null,
            (frozen ? "Froze" : "Unfroze") + " login codes for owner " + userId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> SetElectionFrozenAsync(
        Guid electionGuid,
        bool frozen,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.Elections.AnyAsync(election => election.ElectionGuid == electionGuid, cancellationToken);
        if (!exists)
        {
            return false;
        }

        var row = await GetOrAddElectionAsync(electionGuid, cancellationToken);
        row.SendsFrozen = frozen;
        await _context.SaveChangesAsync(cancellationToken);
        await AuditAsync(
            frozen ? SecurityEventType.PaidSendFrozen : SecurityEventType.PaidSendUnfrozen,
            adminUserId,
            electionGuid,
            (frozen ? "Froze" : "Unfroze") + " login codes for election " + electionGuid);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RaiseElectionAllowanceAsync(
        Guid electionGuid,
        int allowance,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var exists = await _context.Elections.AnyAsync(election => election.ElectionGuid == electionGuid, cancellationToken);
        if (!exists)
        {
            return false;
        }

        var row = await _context.ElectionSendControls
            .FirstOrDefaultAsync(item => item.ElectionGuid == electionGuid, cancellationToken);
        var current = row?.AllowanceOverride ?? _options.ResolvedElectionPaidSendAllowance;
        if (allowance <= current)
        {
            return false;
        }

        if (row == null)
        {
            row = new ElectionSendControl { ElectionGuid = electionGuid };
            _context.ElectionSendControls.Add(row);
        }

        row.AllowanceOverride = allowance;
        await _context.SaveChangesAsync(cancellationToken);
        await AuditAsync(
            SecurityEventType.PaidSendCapRaised,
            adminUserId,
            electionGuid,
            "Raised election paid-send allowance from " + current + " to " + allowance);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> RaiseOwnerDailyCapAsync(
        Guid userId,
        int dailyCap,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var known = await _context.JoinElectionUsers.AnyAsync(link => link.UserId == userId, cancellationToken);
        if (!known)
        {
            return false;
        }

        var row = await _context.OwnerSendControls
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        var current = row?.DailyCapOverride ?? _options.ResolvedOwnerDailyPaidSendCap;
        if (dailyCap <= current)
        {
            return false;
        }

        if (row == null)
        {
            row = new OwnerSendControl { UserId = userId };
            _context.OwnerSendControls.Add(row);
        }

        row.DailyCapOverride = dailyCap;
        await _context.SaveChangesAsync(cancellationToken);
        await AuditAsync(
            SecurityEventType.PaidSendCapRaised,
            adminUserId,
            null,
            "Raised owner daily paid-send cap from " + current + " to " + dailyCap + " for " + userId);
        return true;
    }

    /// <inheritdoc />
    public async Task<bool> ClearElectionFlagAsync(
        Guid electionGuid,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var row = await _context.ElectionSendControls
            .FirstOrDefaultAsync(item => item.ElectionGuid == electionGuid, cancellationToken);
        if (row == null || !row.Flagged)
        {
            return false;
        }

        row.Flagged = false;
        row.FlaggedEntryCount = 0;
        row.ClearedAt = _clock.UtcNow;
        row.ClearedByUserId = adminUserId;
        var activeFlags = await _context.VoterContactFlags
            .Where(flag => flag.ElectionGuid == electionGuid && flag.Active)
            .ToListAsync(cancellationToken);
        foreach (var flag in activeFlags)
        {
            flag.Active = false;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await AuditAsync(
            SecurityEventType.ElectionFlagCleared,
            adminUserId,
            electionGuid,
            "Cleared the voter-list flag for election " + electionGuid);
        return true;
    }

    private async Task<OwnerSendControl> GetOrAddOwnerAsync(Guid userId, CancellationToken cancellationToken)
    {
        var row = await _context.OwnerSendControls
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (row != null)
        {
            return row;
        }

        row = new OwnerSendControl { UserId = userId };
        _context.OwnerSendControls.Add(row);
        return row;
    }

    private async Task<ElectionSendControl> GetOrAddElectionAsync(Guid electionGuid, CancellationToken cancellationToken)
    {
        var row = await _context.ElectionSendControls
            .FirstOrDefaultAsync(item => item.ElectionGuid == electionGuid, cancellationToken);
        if (row != null)
        {
            return row;
        }

        row = new ElectionSendControl { ElectionGuid = electionGuid };
        _context.ElectionSendControls.Add(row);
        return row;
    }

    private Task AuditAsync(SecurityEventType eventType, string adminUserId, Guid? electionGuid, string details)
    {
        return _audit.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = eventType,
            UserId = adminUserId,
            ElectionGuid = electionGuid,
            Details = details,
            Severity = SecurityEventSeverity.Warning
        });
    }
}
