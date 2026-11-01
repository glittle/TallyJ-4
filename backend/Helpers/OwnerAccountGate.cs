using Backend.Context;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers;

/// <summary>
/// Writes the owner-restriction row when an account is created.
/// Self-serve accounts start restricted. A SuperAdmin invite starts approved.
/// </summary>
public static class OwnerAccountGate
{
    /// <summary>
    /// When set to an email address, the next registration of that address fails
    /// after the user row is created and before the restriction row is saved.
    /// Tests use it to prove the two writes roll back together. Production leaves it null.
    /// </summary>
    public static string? FailRestrictionSaveForEmail { get; set; }

    /// <summary>
    /// Adds a restricted row for a new login. The caller saves the context.
    /// </summary>
    public static void AddRestricted(MainDbContext context, string? userId)
    {
        if (!TryParse(userId, out var id))
        {
            return;
        }

        if (context.OwnerSendControls.Local.Any(row => row.UserId == id))
        {
            return;
        }

        context.OwnerSendControls.Add(NewRow(id, OwnerRestrictionStatus.Restricted, paidApproved: false));
    }

    /// <summary>
    /// Marks an invited account approved. Invite issuance is the review.
    /// </summary>
    public static async Task ApproveInvitedAsync(
        MainDbContext context,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (!TryParse(userId, out var id))
        {
            return;
        }

        var row = await context.OwnerSendControls
            .FirstOrDefaultAsync(item => item.UserId == id, cancellationToken);
        if (row == null)
        {
            row = NewRow(id, OwnerRestrictionStatus.Approved, paidApproved: true);
            context.OwnerSendControls.Add(row);
        }
        else
        {
            ApplyApproved(row);
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private static OwnerSendControl NewRow(Guid userId, string status, bool paidApproved)
    {
        var now = DateTimeOffset.UtcNow;
        return new OwnerSendControl
        {
            UserId = userId,
            RestrictionStatus = status,
            PaidSendsApproved = paidApproved,
            ApprovedAt = paidApproved ? now : null,
            StatusChangedAt = now
        };
    }

    private static void ApplyApproved(OwnerSendControl row)
    {
        var now = DateTimeOffset.UtcNow;
        row.RestrictionStatus = OwnerRestrictionStatus.Approved;
        row.PaidSendsApproved = true;
        row.ApprovedAt ??= now;
        row.StatusChangedAt = now;
        row.RejectedAt = null;
    }

    /// <summary>
    /// True when this account must not open online voting. A missing row does not block.
    /// </summary>
    public static async Task<bool> BlocksOnlineVotingAsync(
        MainDbContext context,
        Guid? userId,
        CancellationToken cancellationToken = default)
    {
        if (userId is not Guid id)
        {
            return false;
        }

        var status = await context.OwnerSendControls
            .AsNoTracking()
            .Where(row => row.UserId == id)
            .Select(row => row.RestrictionStatus)
            .FirstOrDefaultAsync(cancellationToken);
        return OwnerRestrictionStatus.BlocksSends(status);
    }

    /// <summary>
    /// Turns online voting off. Used when a restricted owner imports a package that had it on.
    /// </summary>
    public static void ClearOnlineVoting(Election election)
    {
        election.UseOnlineVoting = false;
        election.OnlineWhenOpen = null;
        election.OnlineWhenClose = null;
    }

    private static bool TryParse(string? userId, out Guid id)
        => Guid.TryParse(userId, out id);
}
