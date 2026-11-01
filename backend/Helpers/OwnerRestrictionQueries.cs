using Backend.Context;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers;

/// <summary>
/// Shared lookups for whether an election's owners leave it restricted.
/// An election stays unrestricted when any owner or co-admin is approved.
/// It is restricted only when none of them is approved and at least one is
/// explicitly restricted, waiting for review, or rejected.
/// </summary>
public static class OwnerRestrictionQueries
{
    /// <summary>
    /// One owner or admin on an election, including a join with no control row.
    /// </summary>
    public readonly record struct OwnerTrust(
        Guid UserId,
        string? Status,
        bool PaidSendsApproved,
        bool SendsFrozen)
    {
        /// <summary>
        /// This account is trusted. A restricted co-admin does not cancel it.
        /// </summary>
        public bool IsApproved => OwnerRestrictionStatus.IsApproved(Status, PaidSendsApproved);

        /// <summary>
        /// This account is in the review system and is not trusted.
        /// </summary>
        public bool ExplicitlyRestricts => OwnerRestrictionStatus.BlocksSends(Status);
    }

    /// <summary>
    /// True when voter codes, voter messages, and online voting stay closed.
    /// No owner links, or links with no restriction row, do not restrict the election.
    /// </summary>
    public static bool ElectionIsOwnerRestricted(IReadOnlyList<OwnerTrust> owners)
    {
        if (owners.Count == 0 || owners.Any(owner => owner.IsApproved))
        {
            return false;
        }

        return owners.Any(owner => owner.ExplicitlyRestricts);
    }

    /// <summary>
    /// Loads every owner and admin. A join with no control row is not approved and is not an explicit restriction.
    /// </summary>
    public static async Task<List<OwnerTrust>> LoadOwnersAsync(
        MainDbContext context,
        Guid electionGuid,
        CancellationToken cancellationToken = default)
    {
        var ownerIds = await context.JoinElectionUsers
            .AsNoTracking()
            .Where(link =>
                link.ElectionGuid == electionGuid
                && (link.Role == "Owner" || link.Role == "Admin"))
            .Select(link => link.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
        if (ownerIds.Count == 0)
        {
            return new List<OwnerTrust>();
        }

        var rows = await context.OwnerSendControls
            .AsNoTracking()
            .Where(row => ownerIds.Contains(row.UserId))
            .Select(row => new OwnerTrust(
                row.UserId,
                row.RestrictionStatus,
                row.PaidSendsApproved,
                row.SendsFrozen))
            .ToListAsync(cancellationToken);
        var byId = rows.ToDictionary(row => row.UserId);
        return ownerIds
            .Select(id => byId.TryGetValue(id, out var row)
                ? row
                : new OwnerTrust(id, null, false, false))
            .ToList();
    }

    /// <summary>
    /// Why a message to voters must not go out, or null when it may.
    /// Freeze wins over restriction. A rejected owner does not set this by themselves
    /// on an election that still has another approved owner.
    /// </summary>
    public static async Task<string?> VoterMessageBlockKeyAsync(
        MainDbContext context,
        Guid electionGuid,
        string frozenKey,
        string restrictedKey,
        CancellationToken cancellationToken = default)
    {
        var control = await context.ElectionSendControls
            .AsNoTracking()
            .FirstOrDefaultAsync(row => row.ElectionGuid == electionGuid, cancellationToken);
        var owners = await LoadOwnersAsync(context, electionGuid, cancellationToken);
        if (control?.SendsFrozen == true || owners.Any(owner => owner.SendsFrozen))
        {
            return frozenKey;
        }

        if (ElectionIsOwnerRestricted(owners))
        {
            return restrictedKey;
        }

        return null;
    }

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
