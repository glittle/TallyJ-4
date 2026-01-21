using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Votes;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VotesController : ControllerBase
{
    private readonly IVoteService _voteService;
    private readonly ILogger<VotesController> _logger;

    public VotesController(IVoteService voteService, ILogger<VotesController> logger)
    {
        _voteService = voteService;
        _logger = logger;
    }

    [HttpGet("ballot/{ballotGuid}")]
    public async Task<ActionResult<ApiResponse<List<VoteDto>>>> GetVotesByBallot(Guid ballotGuid)
    {
        var votes = await _voteService.GetVotesByBallotAsync(ballotGuid);
        return Ok(ApiResponse<List<VoteDto>>.SuccessResponse(votes));
    }

    [HttpGet("election/{electionGuid}")]
    public async Task<ActionResult<ApiResponse<List<VoteDto>>>> GetVotesByElection(Guid electionGuid)
    {
        var votes = await _voteService.GetVotesByElectionAsync(electionGuid);
        return Ok(ApiResponse<List<VoteDto>>.SuccessResponse(votes));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<VoteDto>>> GetVote(int id)
    {
        var vote = await _voteService.GetVoteByIdAsync(id);

        if (vote == null)
        {
            return NotFound(ApiResponse<VoteDto>.ErrorResponse($"Vote with ID '{id}' not found"));
        }

        return Ok(ApiResponse<VoteDto>.SuccessResponse(vote));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<VoteDto>>> CreateVote(CreateVoteDto createDto)
    {
        try
        {
            var vote = await _voteService.CreateVoteAsync(createDto);
            return CreatedAtAction(nameof(GetVote), new { id = vote.BallotGuid }, ApiResponse<VoteDto>.SuccessResponse(vote));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VoteDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<ApiResponse<VoteDto>>> UpdateVote(int id, CreateVoteDto updateDto)
    {
        try
        {
            var vote = await _voteService.UpdateVoteAsync(id, updateDto);

            if (vote == null)
            {
                return NotFound(ApiResponse<VoteDto>.ErrorResponse($"Vote with ID '{id}' not found"));
            }

            return Ok(ApiResponse<VoteDto>.SuccessResponse(vote));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VoteDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{id}")]
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
