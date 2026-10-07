using Backend.Context;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Approves paid sends for accounts that already run online voting.
/// The migration does this once for existing databases.
/// Seed calls <see cref="ApproveSeededOnlineOwnersIfMissingAsync"/> for the built-in
/// sample elections only, and only when that account has no control row yet.
/// An existing row is left as the super admin set it.
/// </summary>
public static class OwnerPaidSendGrandfather
{
    /// <summary>
    /// Inserts an approved row for each owner or admin of <paramref name="electionGuids"/>
    /// who does not already have a control row. Also matches <see cref="Election.OwnerLoginId"/>
    /// to a login email for those elections.
    /// </summary>
    public static async Task ApproveSeededOnlineOwnersIfMissingAsync(
        MainDbContext context,
        IReadOnlyCollection<Guid> electionGuids)
    {
        if (electionGuids.Count == 0)
        {
            return;
        }

        var ownerIds = await context.JoinElectionUsers
            .Where(link =>
                electionGuids.Contains(link.ElectionGuid)
                && (link.Role == "Owner" || link.Role == "Admin"))
            .Select(link => link.UserId)
            .Distinct()
            .ToListAsync();

        var loginIds = await context.Elections
            .Where(election =>
                electionGuids.Contains(election.ElectionGuid)
                && election.OwnerLoginId != null)
            .Select(election => election.OwnerLoginId!)
            .Distinct()
            .ToListAsync();

        if (loginIds.Count > 0)
        {
            var users = await context.Users
                .Where(user => user.Email != null)
                .Select(user => new { user.Id, user.Email })
                .ToListAsync();
            foreach (var user in users)
            {
                if (loginIds.Any(login => string.Equals(login, user.Email, StringComparison.OrdinalIgnoreCase))
                    && Guid.TryParse(user.Id, out var userId))
                {
                    ownerIds.Add(userId);
                }
            }
        }

        var distinct = ownerIds.Distinct().ToList();
        if (distinct.Count == 0)
        {
            return;
        }

        var existing = await context.OwnerSendControls
            .Where(row => distinct.Contains(row.UserId))
            .Select(row => row.UserId)
            .ToListAsync();
        var existingSet = existing.ToHashSet();
        var now = DateTimeOffset.UtcNow;
        foreach (var userId in distinct)
        {
            if (existingSet.Contains(userId))
            {
                continue;
            }

            context.OwnerSendControls.Add(new OwnerSendControl
            {
                UserId = userId,
                PaidSendsApproved = true,
                ApprovedAt = now
            });
        }

        await context.SaveChangesAsync();
    }
}
