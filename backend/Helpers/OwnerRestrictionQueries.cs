using Backend.Context;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers;

/// <summary>
/// Shared lookups for elections whose owner or admin is not trusted.
/// </summary>
public static class OwnerRestrictionQueries
{
    /// <summary>
    /// Elections in <paramref name="electionIds"/> that have a rejected owner or admin.
    /// Those elections stay hidden from voters even if online voting was left on.
    /// </summary>
    public static async Task<List<Guid>> RejectedOwnerElectionIdsAsync(
        MainDbContext context,
        IReadOnlyCollection<Guid> electionIds,
        CancellationToken cancellationToken = default)
    {
        if (electionIds.Count == 0)
        {
            return new List<Guid>();
        }

        var links = await context.JoinElectionUsers
            .AsNoTracking()
            .Where(link =>
                electionIds.Contains(link.ElectionGuid)
                && (link.Role == "Owner" || link.Role == "Admin"))
            .Select(link => new { link.ElectionGuid, link.UserId })
            .ToListAsync(cancellationToken);
        if (links.Count == 0)
        {
            return new List<Guid>();
        }

        var userIds = links.Select(link => link.UserId).Distinct().ToList();
        var rejected = await context.OwnerSendControls
            .AsNoTracking()
            .Where(row => userIds.Contains(row.UserId) && row.RestrictionStatus == OwnerRestrictionStatus.Rejected)
            .Select(row => row.UserId)
            .ToListAsync(cancellationToken);
        if (rejected.Count == 0)
        {
            return new List<Guid>();
        }

        var rejectedSet = rejected.ToHashSet();
        return links
            .Where(link => rejectedSet.Contains(link.UserId))
            .Select(link => link.ElectionGuid)
            .Distinct()
            .ToList();
    }
}
