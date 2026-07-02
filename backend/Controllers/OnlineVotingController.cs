using Backend.DTOs.OnlineVoting;
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

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(response);
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

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(response);
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

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(response);
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

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(response);
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

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(response);
    }

    /// <summary>
    /// Gets the list of elections available to an authenticated voter.
    /// </summary>
    /// <param name="voterId">The voter's identifier (from JWT token).</param>
    /// <returns>The list of elections the voter can participate in.</returns>
    [HttpGet("availableElections")]
    [AllowAnonymous]
    public async Task<ActionResult<List<AvailableElectionDto>>> GetAvailableElections([FromQuery] string voterId)
    {
        if (string.IsNullOrWhiteSpace(voterId))
        {
            return BadRequest(new { error = "Voter ID is required." });
        }

        var elections = await _onlineVotingService.GetAvailableElectionsAsync(voterId);
        return Ok(elections);
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
    /// Submits an online ballot for an election.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="dto">The ballot submission data.</param>
    /// <returns>A success message if the ballot was submitted.</returns>
    [HttpPost("{electionGuid}/submitBallot")]
    [AllowAnonymous]
    public async Task<ActionResult<SubmitBallotResponseDto>> SubmitBallot(Guid electionGuid, [FromBody] SubmitOnlineBallotDto dto)
    {
        if (dto.ElectionGuid != electionGuid)
        {
            return BadRequest(new { error = "Election GUID mismatch." });
        }

        var (success, error) = await _onlineVotingService.SubmitBallotAsync(dto);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(new SubmitBallotResponseDto { Message = "Ballot submitted successfully." });
    }

    /// <summary>
    /// Gets the voting status for a specific voter in an election.
    /// </summary>
    /// <param name="electionGuid">The election GUID.</param>
    /// <param name="voterId">The voter ID.</param>
    /// <returns>The vote status information.</returns>
    [HttpGet("{electionGuid}/{voterId}/voteStatus")]
    [AllowAnonymous]
    public async Task<ActionResult<OnlineVoteStatusDto>> GetVoteStatus(Guid electionGuid, string voterId)
    {
        if (string.IsNullOrWhiteSpace(voterId))
        {
            return BadRequest(new { error = "Voter ID is required." });
        }

        var status = await _onlineVotingService.GetVoteStatusAsync(electionGuid, voterId);
        return Ok(status);
    }
}