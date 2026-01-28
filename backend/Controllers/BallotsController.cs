using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Ballots;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

/// <summary>
/// Controller for managing ballot operations including creation, retrieval, updates, and deletion.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BallotsController : ControllerBase
{
    private readonly IBallotService _ballotService;
    private readonly ILogger<BallotsController> _logger;

    /// <summary>
    /// Initializes a new instance of the BallotsController.
    /// </summary>
    /// <param name="ballotService">The ballot service for ballot operations.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public BallotsController(IBallotService ballotService, ILogger<BallotsController> logger)
    {
        _ballotService = ballotService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of ballots for the specified election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="pageNumber">The page number (starting from 1).</param>
    /// <param name="pageSize">The number of items per page (1-200).</param>
    /// <returns>A paginated response containing the ballots.</returns>
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

    /// <summary>
    /// Gets a specific ballot by its GUID.
    /// </summary>
    /// <param name="guid">The GUID of the ballot.</param>
    /// <returns>The ballot information.</returns>
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

    /// <summary>
    /// Creates a new ballot.
    /// </summary>
    /// <param name="createDto">The ballot creation data.</param>
    /// <returns>The created ballot information.</returns>
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

    /// <summary>
    /// Updates an existing ballot.
    /// </summary>
    /// <param name="guid">The GUID of the ballot to update.</param>
    /// <param name="updateDto">The updated ballot data.</param>
    /// <returns>The updated ballot information.</returns>
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

    /// <summary>
    /// Deletes a ballot by its GUID.
    /// </summary>
    /// <param name="guid">The GUID of the ballot to delete.</param>
    /// <returns>No content if successful, or not found if the ballot doesn't exist.</returns>
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
