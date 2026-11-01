using Backend.DTOs.Owners;

namespace Backend.Services;

/// <summary>
/// Owner restriction, review requests, and super-admin approve / hold / reject.
/// </summary>
public interface IOwnerReviewService
{
    /// <summary>
    /// Restriction banner for the signed-in account. A missing row does not block sends.
    /// </summary>
    Task<OwnerRestrictionDto> GetForUserAsync(string? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets ReviewRequested and emails every super admin. Throttled to once per 24 hours.
    /// </summary>
    Task<OwnerReviewActionResult> RequestReviewAsync(string? userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Accounts that are not approved.
    /// </summary>
    Task<IReadOnlyList<OwnerReviewListItemDto>> ListAsync(string? status, CancellationToken cancellationToken = default);

    /// <summary>
    /// Full review dossier. Null when the account does not exist.
    /// </summary>
    Task<OwnerReviewDetailDto?> GetDetailAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Trusts the account for voter codes, paid sends, and online voting, and emails the owner.
    /// </summary>
    Task<OwnerReviewActionResult> ApproveAsync(Guid userId, string adminUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rejects the account, freezes and hides its elections, and emails the owner.
    /// </summary>
    Task<OwnerReviewActionResult> RejectAsync(
        Guid userId,
        string adminUserId,
        string? note,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Leaves the account restricted, stores a note, and emails the owner.
    /// </summary>
    Task<OwnerReviewActionResult> HoldAsync(
        Guid userId,
        string adminUserId,
        string? note,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a rejected owner's elections before the 30-day cleanup.
    /// </summary>
    Task<OwnerReviewActionResult> DeleteElectionsNowAsync(
        Guid userId,
        string adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes elections whose rejection is at least 30 days old.
    /// </summary>
    Task<int> DeleteExpiredRejectedElectionsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a review action. <see cref="ErrorKey"/> is an i18n phrase key.
/// </summary>
public sealed class OwnerReviewActionResult
{
    public bool Success { get; init; }

    public string? ErrorKey { get; init; }

    public DateTimeOffset? NextRequestAt { get; init; }

    public OwnerRestrictionDto? Restriction { get; init; }

    public static OwnerReviewActionResult Ok(OwnerRestrictionDto? restriction = null)
        => new() { Success = true, Restriction = restriction };

    public static OwnerReviewActionResult Fail(string errorKey, DateTimeOffset? nextRequestAt = null)
        => new() { Success = false, ErrorKey = errorKey, NextRequestAt = nextRequestAt };
}
