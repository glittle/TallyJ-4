namespace Backend.Entities;

/// <summary>
/// Per-owner review state. Approval is the account, not one election.
/// An approved account may send voter codes (including paid channels, still under the caps)
/// and may open online voting. Restricted, review-requested, and rejected accounts may not.
/// </summary>
public static class OwnerRestrictionStatus
{
    /// <summary>
    /// New self-serve account. The default until a super admin approves it.
    /// </summary>
    public const string Restricted = "Restricted";

    /// <summary>
    /// The owner asked for a review. Sends and online voting stay blocked.
    /// </summary>
    public const string ReviewRequested = "ReviewRequested";

    /// <summary>
    /// Trusted. Paid sends are approved as part of the same decision.
    /// </summary>
    public const string Approved = "Approved";

    /// <summary>
    /// Declined. Elections are frozen, hidden from voters, and deleted after the retention window.
    /// </summary>
    public const string Rejected = "Rejected";

    /// <summary>
    /// Minimum time between review-request emails for one owner.
    /// </summary>
    public const int ReviewRequestThrottleHours = 24;

    /// <summary>
    /// How long a rejected owner's elections are kept before the cleanup job deletes them.
    /// </summary>
    public const int RejectionRetentionDays = 30;

    /// <summary>
    /// Stable subject prefix so a mail watcher can find review requests.
    /// Outcome mail to the owner does not use this prefix.
    /// </summary>
    public const string ReviewRequestSubjectPrefix = "[TallyJ review request]";

    /// <summary>
    /// Names and flagged contacts included in the review email and the review page.
    /// </summary>
    public const int SampleSize = 8;

    /// <summary>
    /// True when this account must not send voter codes or open online voting.
    /// An empty status is a row written before this column existed, or a unit-test row
    /// that only set <c>PaidSendsApproved</c>. Those rows are not blocked here.
    /// </summary>
    public static bool BlocksSends(string? status)
        => status is Restricted or ReviewRequested or Rejected;

    /// <summary>
    /// A restricted account may email one test code to its own login address.
    /// A rejected account may not.
    /// </summary>
    public static bool AllowsOwnEmailTest(string? status)
        => status is Restricted or ReviewRequested;

    /// <summary>
    /// True when this account is trusted for an election it owns.
    /// An empty status with paid-send approval is a row from before owner review.
    /// A missing row is not approved; it also does not by itself restrict an election.
    /// </summary>
    public static bool IsApproved(string? status, bool paidSendsApproved)
    {
        if (string.Equals(status, Approved, StringComparison.Ordinal))
        {
            return true;
        }

        return string.IsNullOrEmpty(status) && paidSendsApproved;
    }
}
