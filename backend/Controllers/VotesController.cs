using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallyJ4.EF.Context;
using TallyJ4.EF.Models;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class VotesController : ControllerBase
{
    private readonly MainDbContext _context;
    private readonly ILogger<VotesController> _logger;

    public VotesController(MainDbContext context, ILogger<VotesController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("ballot/{ballotGuid}")]
    public async Task<ActionResult<IEnumerable<Vote>>> GetVotesByBallot(Guid ballotGuid)
    {
        return await _context.Votes
            .Where(v => v.BallotGuid == ballotGuid)
            .Include(v => v.Person)
            .OrderBy(v => v.PositionOnBallot)
            .ToListAsync();
    }

    [HttpGet("election/{electionGuid}")]
    public async Task<ActionResult<IEnumerable<Vote>>> GetVotesByElection(Guid electionGuid)
    {
        return await _context.Votes
            .Where(v => v.Ballot.Location.ElectionGuid == electionGuid)
            .Include(v => v.Person)
            .Include(v => v.Ballot)
            .OrderBy(v => v.Ballot.BallotNumAtComputer)
            .ThenBy(v => v.PositionOnBallot)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Vote>> GetVote(int id)
    {
        var vote = await _context.Votes
            .Include(v => v.Person)
            .Include(v => v.Ballot)
            .FirstOrDefaultAsync(v => v.RowId == id);

        if (vote == null)
        {
            return NotFound();
        }

        return vote;
    }

    [HttpPost]
    public async Task<ActionResult<Vote>> CreateVote(Vote vote)
    {
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetVote), new { id = vote.RowId }, vote);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateVote(int id, Vote vote)
    {
        if (id != vote.RowId)
        {
            return BadRequest();
        }

        _context.Entry(vote).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await VoteExists(id))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteVote(int id)
    {
        var vote = await _context.Votes.FirstOrDefaultAsync(v => v.RowId == id);
        if (vote == null)
        {
            return NotFound();
        }

        _context.Votes.Remove(vote);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> VoteExists(int id)
    {
        return await _context.Votes.AnyAsync(v => v.RowId == id);
    }
}
