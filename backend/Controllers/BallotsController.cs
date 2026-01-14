using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Ballots;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BallotsController : ControllerBase
{
    private readonly IBallotService _ballotService;
    private readonly ILogger<BallotsController> _logger;

    public BallotsController(IBallotService ballotService, ILogger<BallotsController> logger)
    {
        _ballotService = ballotService;
        _logger = logger;
    }

    [HttpGet("election/{electionGuid}")]
    public async Task<ActionResult<PaginatedResponse<BallotDto>>> GetBallotsByElection(
        Guid electionGuid,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 200)
        {
            return BadRequest(new { message = "Invalid pagination parameters. PageNumber must be >= 1, PageSize must be between 1 and 200." });
        }

        var result = await _ballotService.GetBallotsByElectionAsync(electionGuid, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{guid}")]
    public async Task<ActionResult<ApiResponse<BallotDto>>> GetBallot(Guid guid)
    {
        var ballot = await _ballotService.GetBallotByGuidAsync(guid);

        if (ballot == null)
        {
            return NotFound(ApiResponse<BallotDto>.ErrorResponse("Ballot not found"));
        }

        return Ok(ApiResponse<BallotDto>.SuccessResponse(ballot));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<BallotDto>>> CreateBallot(CreateBallotDto createDto)
    {
        try
        {
            var ballot = await _ballotService.CreateBallotAsync(createDto);

            return CreatedAtAction(
                nameof(GetBallot),
                new { guid = ballot.BallotGuid },
                ApiResponse<BallotDto>.SuccessResponse(ballot, "Ballot created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BallotDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{guid}")]
    public async Task<ActionResult<ApiResponse<BallotDto>>> UpdateBallot(Guid guid, UpdateBallotDto updateDto)
    {
        try
        {
            var ballot = await _ballotService.UpdateBallotAsync(guid, updateDto);

            if (ballot == null)
            {
                return NotFound(ApiResponse<BallotDto>.ErrorResponse("Ballot not found"));
            }

            return Ok(ApiResponse<BallotDto>.SuccessResponse(ballot, "Ballot updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<BallotDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{guid}")]
    public async Task<IActionResult> DeleteBallot(Guid guid)
    {
        var success = await _ballotService.DeleteBallotAsync(guid);

        if (!success)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Ballot not found"));
        }

        return NoContent();
    }
}
