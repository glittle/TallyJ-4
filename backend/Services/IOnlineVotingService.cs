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
    /// <returns>A task containing the response message.</returns>
    Task<string> RequestVerificationCodeAsync(RequestCodeDto dto);

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
    /// Retrieves the list of candidates for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A task containing the list of candidates.</returns>
    Task<List<OnlineCandidateDto>> GetCandidatesAsync(Guid electionGuid);

    /// <summary>
    /// Submits an online ballot for an election.
    /// </summary>
    /// <param name="dto">The ballot submission data.</param>
    /// <returns>A task containing a success flag and optional error message.</returns>
    Task<(bool Success, string? Error)> SubmitBallotAsync(SubmitOnlineBallotDto dto);

    /// <summary>
    /// Retrieves the voting status for a specific voter in an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="voterId">The voter's identifier.</param>
    /// <returns>A task containing the voter's status information.</returns>
    Task<OnlineVoteStatusDto> GetVoteStatusAsync(Guid electionGuid, string voterId);

    /// <summary>
    /// Authenticates a voter using Google OAuth and returns authentication token if voter is in at least one open election.
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
    /// Authenticates a voter using Facebook OAuth and returns authentication token if voter is in at least one open election.
    /// </summary>
    /// <param name="dto">The Facebook authentication request containing the access token.</param>
    /// <returns>A task containing success flag, optional error message, and authentication response.</returns>
    Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> FacebookAuthAsync(FacebookAuthForVoterDto dto);

    /// <summary>
    /// Authenticates a voter using Kakao OAuth and returns authentication token if voter is in at least one open election.
    /// </summary>
    /// <param name="dto">The Kakao authentication request containing the access token.</param>
    /// <returns>A task containing success flag, optional error message, and authentication response.</returns>
    Task<(bool Success, string? Error, OnlineVoterAuthResponse? Response)> KakaoAuthAsync(KakaoAuthForVoterDto dto);
}



