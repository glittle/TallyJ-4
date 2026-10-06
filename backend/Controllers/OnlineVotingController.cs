using Backend.DTOs.OnlineVoting;
using Backend.Helpers;
using Backend.Middleware;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing online voting operations including voter verification and ballot submission.
/// </summary>
[ApiController]
[Route("api/online-voting")]
public class OnlineVotingController : ControllerBase
{
    private readonly IOnlineVotingService _onlineVotingService;
    private readonly ILogger<OnlineVotingController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnlineVotingController"/> class.
    /// </summary>
    /// <param name="onlineVotingService">The online voting service.</param>
    /// <param name="logger">The logger.</param>
    public OnlineVotingController(
        IOnlineVotingService onlineVotingService,
        ILogger<OnlineVotingController> logger)
    {
        _onlineVotingService = onlineVotingService;
        _logger = logger;
    }

    /// <summary>
    /// Requests a verification code for online voting.
    /// </summary>
    /// <param name="dto">The request code data.</param>
    /// <returns>A success message regardless of whether the code was sent.</returns>
    [HttpPost("requestCode")]
    [AllowAnonymous]
    public async Task<ActionResult<RequestCodeResponseDto>> RequestCode([FromBody] RequestCodeDto dto)
    {
        var result = await _onlineVotingService.RequestVerificationCodeAsync(dto);
        return Ok(result);
    }

    /// <summary>
    /// Verifies a voter's verification code for online voting access.
    /// </summary>
    /// <param name="dto">The verification code data.</param>
    /// <returns>The voter session information if successful.</returns>
    [HttpPost("verifyCode")]
    [AllowAnonymous]
    public async Task<ActionResult<OnlineVoterAuthResponse>> VerifyCode([FromBody] VerifyCodeDto dto)
    {
        var (success, error, response) = await _onlineVotingService.VerifyCodeAsync(dto);
        return CompleteVoterAuth(success, error, response);
    }

    /// <summary>
    /// Authenticates a voter using Google OAuth for online voting access.
    /// </summary>
    /// <param name="dto">The Google authentication request.</param>
    /// <returns>The voter session information if successful.</returns>
    [HttpPost("googleAuth")]
    [AllowAnonymous]
    public async Task<ActionResult<OnlineVoterAuthResponse>> GoogleAuth([FromBody] GoogleAuthForVoterDto dto)
    {
        var (success, error, response) = await _onlineVotingService.AuthenticateVoterWithGoogleAsync(dto);
        return CompleteVoterAuth(success, error, response);
    }

    /// <summary>
    /// Authenticates a voter using Facebook OAuth for online voting access.
    /// </summary>
    /// <param name="dto">The Facebook authentication request.</param>
    /// <returns>The voter session information if successful.</returns>
    [HttpPost("facebookAuth")]
    [AllowAnonymous]
    public async Task<ActionResult<OnlineVoterAuthResponse>> FacebookAuth([FromBody] FacebookAuthForVoterDto dto)
    {
        var (success, error, response) = await _onlineVotingService.FacebookAuthAsync(dto);
        return CompleteVoterAuth(success, error, response);
    }

    /// <summary>
    /// Authenticates a voter using Kakao OAuth for online voting access.
    /// </summary>
    /// <param name="dto">The Kakao authentication request.</param>
    /// <returns>The voter session information if successful.</returns>
    [HttpPost("kakaoAuth")]
    [AllowAnonymous]
    public async Task<ActionResult<OnlineVoterAuthResponse>> KakaoAuth([FromBody] KakaoAuthForVoterDto dto)
    {
        var (success, error, response) = await _onlineVotingService.KakaoAuthAsync(dto);
        return CompleteVoterAuth(success, error, response);
    }

    /// <summary>
    /// Authenticates a voter using the Telegram Login Widget.
    /// </summary>
    /// <param name="dto">The Telegram authentication request.</param>
    /// <returns>The voter session information if successful.</returns>
    [HttpPost("telegramAuth")]
    [AllowAnonymous]
    public async Task<ActionResult<OnlineVoterAuthResponse>> TelegramAuth([FromBody] TelegramAuthForVoterDto dto)
    {
        var (success, error, response) = await _onlineVotingService.TelegramAuthAsync(dto);
        return CompleteVoterAuth(success, error, response);
    }

    /// <summary>
    /// Gets the list of elections available to an authenticated voter.
    /// Voter identity is derived from the JWT issued during voter authentication.
    /// </summary>
    /// <returns>The list of elections the voter can participate in.</returns>
    [HttpGet("availableElections")]
    [Authorize(Policy = "OnlineVoter")]
    public async Task<ActionResult<List<AvailableElectionDto>>> GetAvailableElections()
    {
        var voterId = User.FindFirst("voterId")?.Value;
        if (string.IsNullOrWhiteSpace(voterId))
        {
            return Unauthorized(new { error = "Invalid voter token." });
        }

        var elections = await _onlineVotingService.GetAvailableElectionsAsync(voterId);
        return Ok(elections);
    }

    /// <summary>
    /// Returns the authenticated online voter's identity from the session cookie JWT.
    /// Used by the SPA to restore voterId after refresh without reading the httpOnly token.
    /// </summary>
    [HttpGet("me")]
    [Authorize(Policy = "OnlineVoter")]
    public ActionResult<OnlineVoterSessionDto> GetSession()
    {
        var voterId = User.FindFirst("voterId")?.Value;
        if (string.IsNullOrWhiteSpace(voterId))
        {
            return Unauthorized(new { error = "Invalid voter token." });
        }

        return Ok(new OnlineVoterSessionDto
        {
            VoterId = voterId,
            VoterIdType = User.FindFirst("voterIdType")?.Value ?? string.Empty
        });
    }

    /// <summary>
    /// Clears online-voter cookies. Anonymous so an expired JWT can still log out.
    /// Does not clear teller <c>auth_token</c> cookies.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public IActionResult Logout()
    {
        SecureCookieMiddleware.ClearVoterAuthCookies(HttpContext);
        return Ok(new { message = "Logged out successfully" });
    }

    /// <summary>
    /// Gets public information about an election for online voting.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <returns>The election information.</returns>
    [HttpGet("{electionGuid}/electionInfo")]
    [AllowAnonymous]
    public async Task<ActionResult<OnlineElectionInfoDto>> GetElectionInfo(Guid electionGuid)
    {
        var electionInfo = await _onlineVotingService.GetElectionInfoAsync(electionGuid);

        if (electionInfo == null)
        {
            return NotFound(new { error = "Election not found." });
        }

        return Ok(electionInfo);
    }

    /// <summary>
    /// Gets the list of people for an election.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <returns>The list of people.</returns>
    [HttpGet("{electionGuid}/people")]
    [AllowAnonymous]
    public async Task<ActionResult<List<OnlinePersonDto>>> GetPeople(Guid electionGuid)
    {
        var people = await _onlineVotingService.GetPeopleAsync(electionGuid);
        return Ok(people);
    }

    /// <summary>
    /// Submits an online ballot for the authenticated voter.
    /// The voter id is the <c>voterId</c> claim. A body id that differs is rejected.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="dto">The ballot submission data.</param>
    /// <returns>A success message if the ballot was submitted.</returns>
    [HttpPost("{electionGuid}/submitBallot")]
    [Authorize(Policy = "OnlineVoter")]
    public async Task<ActionResult<SubmitBallotResponseDto>> SubmitBallot(Guid electionGuid, [FromBody] SubmitOnlineBallotDto dto)
    {
        if (!TryGetAuthenticatedVoterId(out var voterId))
        {
            return Unauthorized();
        }

        if (!SuppliedVoterIdMatches(dto.VoterId, voterId))
        {
            return VoterAccessForbidden();
        }

        if (dto.ElectionGuid != electionGuid)
        {
            return BadRequest(new { error = "Election GUID mismatch." });
        }

        dto.VoterId = voterId;
        var (success, error) = await _onlineVotingService.SubmitBallotAsync(dto);

        if (!success)
        {
            if (IsVoterElectionMismatch(error))
            {
                return VoterAccessForbidden();
            }

            return BadRequest(new { error });
        }

        return Ok(new SubmitBallotResponseDto { Message = "Ballot submitted successfully." });
    }

    /// <summary>
    /// Gets the voting status for the authenticated voter.
    /// The route id must match the <c>voterId</c> claim; it is not used to choose a person.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="voterId">Ignored unless it differs from the session, which is forbidden.</param>
    /// <returns>The vote status information.</returns>
    [HttpGet("{electionGuid}/{voterId}/voteStatus")]
    [Authorize(Policy = "OnlineVoter")]
    public async Task<ActionResult<OnlineVoteStatusDto>> GetVoteStatus(Guid electionGuid, string voterId)
    {
        if (!TryGetAuthenticatedVoterId(out var authenticatedVoterId))
        {
            return Unauthorized();
        }

        if (!SuppliedVoterIdMatches(voterId, authenticatedVoterId))
        {
            return VoterAccessForbidden();
        }

        var status = await _onlineVotingService.GetVoteStatusAsync(electionGuid, authenticatedVoterId);
        if (status.Message == "voting.status.voterNotFound")
        {
            return VoterAccessForbidden();
        }

        return Ok(status);
    }

    /// <summary>
    /// Reads the online-voter id from the validated JWT. Missing claim is not a voter session.
    /// </summary>
    private bool TryGetAuthenticatedVoterId(out string voterId)
    {
        voterId = User.FindFirst("voterId")?.Value ?? string.Empty;
        return !string.IsNullOrWhiteSpace(voterId);
    }

    /// <summary>
    /// An omitted client id is ignored. A present id must be the session id (ordinal).
    /// </summary>
    private static bool SuppliedVoterIdMatches(string? suppliedVoterId, string authenticatedVoterId)
    {
        return string.IsNullOrWhiteSpace(suppliedVoterId)
            || string.Equals(suppliedVoterId, authenticatedVoterId, StringComparison.Ordinal);
    }

    /// <summary>
    /// Same body for id mismatch, unknown election, and not on the list.
    /// No phrase key and no voter id — those distinguish the cases.
    /// </summary>
    private ObjectResult VoterAccessForbidden()
    {
        return StatusCode(StatusCodes.Status403Forbidden, new { error = "forbidden" });
    }

    private static bool IsVoterElectionMismatch(string? error) =>
        error is "voting.submit.voterNotFound" or "voting.submit.electionNotFound";

    /// <summary>
    /// Issues httpOnly voter cookies and omits the JWT from the JSON body.
    /// </summary>
    private ActionResult<OnlineVoterAuthResponse> CompleteVoterAuth(
        bool success,
        string? error,
        OnlineVoterAuthResponse? response)
    {
        if (!success || response == null)
        {
            return BadRequest(VoterVerifyError.ToBadRequestBody(error));
        }

        if (!string.IsNullOrEmpty(response.Token))
        {
            SecureCookieMiddleware.SetVoterAuthCookies(HttpContext, response.Token);
        }

        response.Token = null;
        return Ok(response);
    }
}