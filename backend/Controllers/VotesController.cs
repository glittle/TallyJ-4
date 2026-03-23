using Backend.DTOs.Votes;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing vote operations including creation, retrieval, updates, and deletion.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VotesController : ControllerBase
{
    private readonly IVoteService _voteService;
    private readonly ILogger<VotesController> _logger;

    /// <summary>
    /// Initializes a new instance of the VotesController.
    /// </summary>
    /// <param name="voteService">The vote service for vote operations.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public VotesController(IVoteService voteService, ILogger<VotesController> logger)
    {
        _voteService = voteService;
        _logger = logger;
    }

    /// <summary>
    /// Gets all votes for a specific ballot.
    /// </summary>
    /// <param name="ballotGuid">The GUID of the ballot.</param>
    /// <returns>A list of votes for the specified ballot.</returns>
    [HttpGet("{ballotGuid}/getVotesByBallot")]
    public async Task<ActionResult<ApiResponse<List<VoteDto>>>> GetVotesByBallot(Guid ballotGuid)
    {
        var votes = await _voteService.GetVotesByBallotAsync(ballotGuid);
        return Ok(ApiResponse<List<VoteDto>>.SuccessResponse(votes));
    }

    /// <summary>
    /// Gets all votes for a specific election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>A list of votes for the specified election.</returns>
    [HttpGet("{electionGuid}/getVotesByElection")]
    public async Task<ActionResult<ApiResponse<List<VoteDto>>>> GetVotesByElection(Guid electionGuid)
    {
        var votes = await _voteService.GetVotesByElectionAsync(electionGuid);
        return Ok(ApiResponse<List<VoteDto>>.SuccessResponse(votes));
    }

    /// <summary>
    /// Gets a specific vote by its ID.
    /// </summary>
    /// <param name="id">The ID of the vote.</param>
    /// <returns>The vote information.</returns>
    [HttpGet("{id}/getVote")]
    public async Task<ActionResult<ApiResponse<VoteDto>>> GetVote(int id)
    {
        var vote = await _voteService.GetVoteByIdAsync(id);

        if (vote == null)
        {
            return NotFound(ApiResponse<VoteDto>.ErrorResponse($"Vote with ID '{id}' not found"));
        }

        return Ok(ApiResponse<VoteDto>.SuccessResponse(vote));
    }

    /// <summary>
    /// Creates a new vote. The vote status is determined server-side based on the person's eligibility.
    /// </summary>
    /// <param name="createDto">The vote creation data.</param>
    /// <returns>The created vote information and the ballot's current status.</returns>
    [HttpPost("createVote")]
    public async Task<ActionResult<ApiResponse<VoteWithBallotStatusDto>>> CreateVote(CreateVoteDto createDto)
    {
        try
        {
            var result = await _voteService.CreateVoteAsync(createDto);
            return CreatedAtAction(nameof(GetVote), new { id = result.Vote.RowId }, ApiResponse<VoteWithBallotStatusDto>.SuccessResponse(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VoteWithBallotStatusDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Updates an existing vote. The vote status is determined server-side based on the person's eligibility.
    /// </summary>
    /// <param name="id">The ID of the vote to update.</param>
    /// <param name="updateDto">The updated vote data.</param>
    /// <returns>The updated vote information and the ballot's current status.</returns>
    [HttpPut("{id}/updateVote")]
    public async Task<ActionResult<ApiResponse<VoteWithBallotStatusDto>>> UpdateVote(int id, CreateVoteDto updateDto)
    {
        try
        {
            var result = await _voteService.UpdateVoteAsync(id, updateDto);

            if (result == null)
            {
                return NotFound(ApiResponse<VoteWithBallotStatusDto>.ErrorResponse($"Vote with ID '{id}' not found"));
            }

            return Ok(ApiResponse<VoteWithBallotStatusDto>.SuccessResponse(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VoteWithBallotStatusDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Deletes a vote by its ID.
    /// </summary>
    /// <param name="id">The ID of the vote to delete.</param>
    /// <returns>A success response if the vote was deleted, or not found if the vote doesn't exist.</returns>
    [HttpDelete("{id}/deleteVote")]
    public async Task<ActionResult<ApiResponse<object?>>> DeleteVote(int id)
    {
        var success = await _voteService.DeleteVoteAsync(id);

        if (!success)
        {
            return NotFound(ApiResponse<object?>.ErrorResponse($"Vote with ID '{id}' not found"));
        }

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Vote deleted successfully"));
    }
}



