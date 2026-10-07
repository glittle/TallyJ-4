using Backend.DTOs.SuperAdmin;

namespace Backend.Services;

/// <summary>
/// Super-admin actions for paid-send approval, caps, and freezes.
/// </summary>
public interface IPaidSendAdminService
{
    /// <summary>
    /// Lists pending approvals, current cap hits, freezes, and flagged elections.
    /// </summary>
    Task<PaidSendAdminOverviewDto> GetOverviewAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Approves one owner for SMS, voice, and WhatsApp codes.
    /// </summary>
    Task<bool> ApproveOwnerAsync(Guid userId, string adminUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Freezes or lifts the freeze on every login code for one owner.
    /// </summary>
    Task<bool> SetOwnerFrozenAsync(Guid userId, bool frozen, string adminUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Freezes or lifts the freeze on every login code for one election.
    /// </summary>
    Task<bool> SetElectionFrozenAsync(Guid electionGuid, bool frozen, string adminUserId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Raises the election paid-send allowance. Returns false when the election does not exist
    /// or the new allowance is not higher than the one in effect.
    /// </summary>
    Task<bool> RaiseElectionAllowanceAsync(
        Guid electionGuid,
        int allowance,
        string adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Raises the owner's daily paid-send cap. Returns false when the new cap is not higher.
    /// </summary>
    Task<bool> RaiseOwnerDailyCapAsync(
        Guid userId,
        int dailyCap,
        string adminUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears the voter-list flag so SMS, voice, WhatsApp, and online voting can run again.
    /// Returns false when the election is not flagged.
    /// </summary>
    Task<bool> ClearElectionFlagAsync(
        Guid electionGuid,
        string adminUserId,
        CancellationToken cancellationToken = default);
}
