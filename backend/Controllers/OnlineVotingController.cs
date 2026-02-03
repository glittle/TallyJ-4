using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.OnlineVoting;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/online-voting")]
public class OnlineVotingController : ControllerBase
{
    private readonly IOnlineVotingService _onlineVotingService;
    private readonly ILogger<OnlineVotingController> _logger;

    public OnlineVotingController(
        IOnlineVotingService onlineVotingService,
        ILogger<OnlineVotingController> logger)
    {
        _onlineVotingService = onlineVotingService;
        _logger = logger;
    }

    [HttpPost("request-code")]
    [AllowAnonymous]
    public async Task<IActionResult> RequestCode([FromBody] RequestCodeDto dto)
    {
        var (success, error) = await _onlineVotingService.RequestVerificationCodeAsync(dto);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(new { message = "Verification code sent successfully." });
    }

    [HttpPost("verify-code")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyCode([FromBody] VerifyCodeDto dto)
    {
        var (success, error, response) = await _onlineVotingService.VerifyCodeAsync(dto);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(response);
    }

    [HttpGet("elections/{electionGuid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetElectionInfo(Guid electionGuid)
    {
        var electionInfo = await _onlineVotingService.GetElectionInfoAsync(electionGuid);

        if (electionInfo == null)
        {
            return NotFound(new { error = "Election not found." });
        }

        return Ok(electionInfo);
    }

    [HttpGet("elections/{electionGuid}/candidates")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCandidates(Guid electionGuid)
    {
        var candidates = await _onlineVotingService.GetCandidatesAsync(electionGuid);
        return Ok(candidates);
    }

    [HttpPost("elections/{electionGuid}/submit-ballot")]
    [AllowAnonymous]
    public async Task<IActionResult> SubmitBallot(Guid electionGuid, [FromBody] SubmitOnlineBallotDto dto)
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

        return Ok(new { message = "Ballot submitted successfully." });
    }

    [HttpGet("elections/{electionGuid}/vote-status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetVoteStatus(Guid electionGuid, [FromQuery] string voterId)
    {
        if (string.IsNullOrWhiteSpace(voterId))
        {
            return BadRequest(new { error = "Voter ID is required." });
        }

        var status = await _onlineVotingService.GetVoteStatusAsync(electionGuid, voterId);
        return Ok(status);
    }
}
