using System.Security.Claims;
using Backend.Authorization;
using Backend.Context;
using Backend.DTOs.Elections;
using Backend.Entities;
using Backend.Helpers;
using Backend.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Election co-admins are <see cref="JoinElectionUser"/> rows with role Admin.
/// Who may change that list is <see cref="ElectionAccessPolicies.FullTellerAccess"/>:
/// an Owner or Admin join, an Identity Admin, or a super admin.
/// Restriction is not stored on the election. The next
/// <see cref="OwnerRestrictionQueries.LoadOwnersAsync"/> sees the new joins.
/// </summary>
public class ElectionAdminService : IElectionAdminService
{
    public const string OwnerRole = "Owner";
    public const string AdminRole = "Admin";

    private readonly MainDbContext _context;
    private readonly IElectionAccessEvaluator _access;
    private readonly ILogger<ElectionAdminService> _logger;

    public ElectionAdminService(
        MainDbContext context,
        IElectionAccessEvaluator access,
        ILogger<ElectionAdminService> logger)
    {
        _context = context;
        _access = access;
        _logger = logger;
    }

    public async Task<ElectionAdminsListResult> ListAsync(
        Guid electionGuid,
        ClaimsPrincipal caller,
        CancellationToken cancellationToken = default)
    {
        var gate = await GateAsync(electionGuid, caller, cancellationToken);
        if (gate == Gate.NotFound)
        {
            return ElectionAdminsListResult.NotFound();
        }

        if (gate == Gate.Forbidden)
        {
            return ElectionAdminsListResult.Forbidden();
        }

        return ElectionAdminsListResult.Ok(await LoadAdminsAsync(electionGuid, cancellationToken));
    }

    public async Task<AddElectionAdminResult> AddAsync(
        Guid electionGuid,
        string email,
        ClaimsPrincipal caller,
        CancellationToken cancellationToken = default)
    {
        var gate = await GateAsync(electionGuid, caller, cancellationToken);
        if (gate == Gate.NotFound)
        {
            return AddElectionAdminResult.NotFound();
        }

        if (gate == Gate.Forbidden)
        {
            return AddElectionAdminResult.Forbidden();
        }

        var user = await FindOfficerAsync(email, cancellationToken);
        if (user == null || !Guid.TryParse(user.Id, out var userId))
        {
            _logger.LogInformation(
                "Co-admin add for election {ElectionGuid} did not match an officer account",
                electionGuid);
            return AddElectionAdminResult.NoAccount();
        }

        var existing = await _context.JoinElectionUsers
            .AsNoTracking()
            .AnyAsync(
                link => link.ElectionGuid == electionGuid
                    && link.UserId == userId
                    && (link.Role == OwnerRole || link.Role == AdminRole),
                cancellationToken);
        if (!existing)
        {
            _context.JoinElectionUsers.Add(new JoinElectionUser
            {
                ElectionGuid = electionGuid,
                UserId = userId,
                Role = AdminRole
            });
            await _context.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Added co-admin {UserId} to election {ElectionGuid}",
                userId,
                electionGuid);
        }

        var admins = await LoadAdminsAsync(electionGuid, cancellationToken);
        var admin = admins.First(item => item.UserId == userId);
        return AddElectionAdminResult.Ok(admin, existing);
    }

    public async Task<RemoveElectionAdminResult> RemoveAsync(
        Guid electionGuid,
        Guid userId,
        ClaimsPrincipal caller,
        CancellationToken cancellationToken = default)
    {
        var gate = await GateAsync(electionGuid, caller, cancellationToken);
        if (gate == Gate.NotFound)
        {
            return RemoveElectionAdminResult.NotFound();
        }

        if (gate == Gate.Forbidden)
        {
            return RemoveElectionAdminResult.Forbidden();
        }

        var links = await _context.JoinElectionUsers
            .Where(link =>
                link.ElectionGuid == electionGuid
                && (link.Role == OwnerRole || link.Role == AdminRole))
            .ToListAsync(cancellationToken);
        var target = links.Where(link => link.UserId == userId).ToList();
        if (target.Count == 0)
        {
            return RemoveElectionAdminResult.NotAnAdmin();
        }

        if (target.Any(link => link.Role == OwnerRole))
        {
            return RemoveElectionAdminResult.Owner();
        }

        var distinctOwners = links.Select(link => link.UserId).Distinct().Count();
        if (distinctOwners <= 1)
        {
            return RemoveElectionAdminResult.LastOwner();
        }

        _context.JoinElectionUsers.RemoveRange(target);
        await _context.SaveChangesAsync(cancellationToken);
        _logger.LogInformation(
            "Removed co-admin {UserId} from election {ElectionGuid}",
            userId,
            electionGuid);
        return RemoveElectionAdminResult.Ok();
    }

    private async Task<Gate> GateAsync(
        Guid electionGuid,
        ClaimsPrincipal caller,
        CancellationToken cancellationToken)
    {
        var outcome = await _access.EvaluateAsync(
            caller,
            electionGuid,
            ElectionAccessPolicies.FullTellerAccess);
        if (!outcome.ElectionExists)
        {
            return Gate.NotFound;
        }

        return outcome.Allowed ? Gate.Ok : Gate.Forbidden;
    }

    /// <summary>
    /// Same uppercase key ASP.NET Identity stores in <see cref="AppUser.NormalizedEmail"/>.
    /// </summary>
    private async Task<AppUser?> FindOfficerAsync(string? email, CancellationToken cancellationToken)
    {
        var normalized = email?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(normalized))
        {
            return null;
        }

        return await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.NormalizedEmail == normalized, cancellationToken);
    }

    private async Task<List<ElectionAdminDto>> LoadAdminsAsync(
        Guid electionGuid,
        CancellationToken cancellationToken)
    {
        var links = await _context.JoinElectionUsers
            .AsNoTracking()
            .Where(link =>
                link.ElectionGuid == electionGuid
                && (link.Role == OwnerRole || link.Role == AdminRole))
            .Select(link => new { link.UserId, link.Role })
            .ToListAsync(cancellationToken);

        var grouped = links
            .GroupBy(link => link.UserId)
            .Select(group => new
            {
                UserId = group.Key,
                Role = group.Any(link => link.Role == OwnerRole) ? OwnerRole : AdminRole
            })
            .ToList();
        if (grouped.Count == 0)
        {
            return [];
        }

        var ids = grouped.Select(item => item.UserId.ToString()).ToList();
        var users = await _context.Users
            .AsNoTracking()
            .Where(user => ids.Contains(user.Id))
            .Select(user => new { user.Id, user.Email, user.DisplayName })
            .ToListAsync(cancellationToken);
        var byId = users.ToDictionary(user => user.Id, StringComparer.Ordinal);

        var canRemove = grouped.Count > 1;
        return grouped
            .Select(item =>
            {
                byId.TryGetValue(item.UserId.ToString(), out var user);
                return new ElectionAdminDto
                {
                    UserId = item.UserId,
                    Email = user?.Email,
                    DisplayName = user?.DisplayName,
                    Role = item.Role,
                    CanRemove = canRemove && item.Role != OwnerRole
                };
            })
            .OrderBy(item => item.Role == OwnerRole ? 0 : 1)
            .ThenBy(item => item.Email)
            .ToList();
    }

    private enum Gate
    {
        Ok,
        NotFound,
        Forbidden
    }
}
