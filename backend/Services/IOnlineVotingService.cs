using Backend.DTOs.OnlineVoting;

namespace Backend.Services;

/// <summary>
/// Service interface for online voting operations.
/// </summary>
public interface IOnlineVotingService
{
    /// <summary>
    /// Requests a verification code for a voter.
    /// </summary>
    /// <param name="dto">The request containing voter identification details.</param>
    /// <returns>A task containing the response message and optional dev verification code.</returns>
    Task<RequestCodeResponseDto> RequestVerificationCodeAsync(RequestCodeDto dto);

    /// <summary>
    /// Verifies a voter's verification code.
    /// </summary>
    /// <param name="dto">The verification request containing the code.</param>
    /// <returns>A task containing success flag, optional error message, and authentication response.</returns>
    Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> VerifyCodeAsync(VerifyCodeDto dto);

    /// <summary>
    /// Retrieves information about an election for online voting.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A task containing the election information, or null if not found.</returns>
    Task<OnlineElectionInfoDto?> GetElectionInfoAsync(Guid electionGuid);

    /// <summary>
    /// Retrieves the list of people for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A task containing the list of people.</returns>
    Task<List<OnlinePersonDto>> GetPeopleAsync(Guid electionGuid);

    /// <summary>
    /// Submits an online ballot for an election.
    /// <paramref name="dto"/>'s voter id must be the authenticated session id.
    /// The HTTP action overwrites any client-supplied id before calling this.
    /// </summary>
    /// <param name="dto">The ballot submission data.</param>
    /// <returns>A task containing a success flag and optional error message.</returns>
    Task<(bool Success, string? Error)> SubmitBallotAsync(SubmitOnlineBallotDto dto);

    /// <summary>
    /// Retrieves the voting status for the authenticated voter in an election.
    /// Callers must pass the session voter id, not an unverified client value.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="voterId">The authenticated voter's identifier.</param>
    /// <returns>A task containing the voter's status information.</returns>
    Task<OnlineVoteStatusDto> GetVoteStatusAsync(Guid electionGuid, string voterId);

    /// <summary>
    /// Authenticates a voter using Google. The email must belong to a person
    /// in an election open for external sign-in (both ends set, close instant
    /// still open). A null open or close is not that window.
    /// </summary>
    /// <param name="dto">The Google authentication request containing the credential.</param>
    /// <returns>A task containing success flag, optional error message, and authentication response.</returns>
    Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> AuthenticateVoterWithGoogleAsync(GoogleAuthForVoterDto dto);

    /// <summary>
    /// Gets the list of elections available to an authenticated voter.
    /// </summary>
    /// <param name="voterId">The voter's identifier (email/phone/code).</param>
    /// <returns>A task containing the list of available elections.</returns>
    Task<List<AvailableElectionDto>> GetAvailableElectionsAsync(string voterId);

    /// <summary>
    /// Authenticates a voter using Facebook. The email must belong to a person
    /// in an election <see cref="OnlineVotingService"/> treats as open for
    /// external sign-in (both ends set, close instant still open).
    /// </summary>
    /// <param name="dto">The Facebook authentication request containing the access token.</param>
    /// <returns>A task containing success flag, optional error message, and authentication response.</returns>
    Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> AuthenticateVoterWithFacebookAsync(FacebookAuthForVoterDto dto);

    /// <summary>
    /// Authenticates a voter using Kakao. Email is tried first, then phone,
    /// against a person in an election open for external sign-in.
    /// </summary>
    /// <param name="dto">The Kakao authentication request containing the access token.</param>
    /// <returns>A task containing success flag, optional error message, and authentication response.</returns>
    Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> AuthenticateVoterWithKakaoAsync(KakaoAuthForVoterDto dto);

    /// <summary>
    /// Authenticates a voter using the Telegram Login Widget. Some election
    /// must be open for external sign-in, and an <c>OnlineVoter</c> row of
    /// type <c>T</c> must exist for this Telegram id. That id is not matched
    /// to a person in the open election.
    /// </summary>
    /// <param name="dto">The Telegram authentication request containing the widget callback data.</param>
    /// <returns>A task containing success flag, optional error message, and authentication response.</returns>
    Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> AuthenticateVoterWithTelegramAsync(TelegramAuthForVoterDto dto);

    /// <summary>
    /// Pending is Submitted plus Processing, excluding people who already
    /// voted another way. Processed is every Processed row.
    /// </summary>
    Task<AcceptAllOnlineBallotsSummaryDto?> GetAcceptAllSummaryAsync(Guid electionGuid);

    /// <summary>
    /// Accepts pending online ballots into regular ballots. The window may stay open.
    /// A run takes Submitted rows that have votes or a legacy ballot id, and every
    /// already-Processing row. People who voted another way are not accepted.
    /// An empty Submitted row with no ballot id stays Submitted. A second run on
    /// this host is rejected. Processed rows are not taken again.
    /// A successful run writes one operational audit row (who / when / counts). Failed or
    /// overlapping (409) runs do not.
    /// </summary>
    /// <param name="acceptedByUserId">Logged-in teller user id (JWT sub). Not a voter id.</param>
    Task<AcceptAllOnlineBallotsResultDto> AcceptAllPendingAsync(
        Guid electionGuid,
        string? acceptedByUserId = null);
}



