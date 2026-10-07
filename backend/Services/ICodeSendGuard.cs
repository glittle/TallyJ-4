namespace Backend.Services;

/// <summary>
/// Decides whether a login code may be sent for the elections that list the voter.
/// SMS, voice, and WhatsApp require an approved owner and stay inside the caps.
/// Email skips the approval and the caps.
/// A freeze or a flagged election stops every channel.
/// </summary>
public interface ICodeSendGuard
{
    /// <summary>
    /// Reserves a paid-send slot when the channel is paid and the caps allow it.
    /// Returns <see cref="CodeSendReservation.Allowed"/> false when the send must not happen.
    /// The voter-facing reply stays the same as a successful send.
    /// </summary>
    Task<CodeSendReservation> ReserveAsync(
        IReadOnlyList<Guid> electionGuids,
        string channel,
        string destination,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes a code-send log row for a send that was reserved or blocked.
    /// </summary>
    Task LogOutcomeAsync(
        CodeSendReservation reservation,
        string channel,
        string destination,
        bool sent,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a send check. <see cref="BlockReason"/> is null when the send is allowed.
/// </summary>
public sealed record CodeSendReservation(
    bool Allowed,
    string? BlockReason,
    Guid? ElectionGuid,
    Guid? OwnerUserId,
    bool FirstPaidSend);
