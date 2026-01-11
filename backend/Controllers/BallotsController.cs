using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallyJ4.EF.Context;
using TallyJ4.Domain.Entities;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BallotsController : ControllerBase
{
    private readonly MainDbContext _context;
    private readonly ILogger<BallotsController> _logger;

    public BallotsController(MainDbContext context, ILogger<BallotsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("election/{electionGuid}")]
    public async Task<ActionResult<IEnumerable<Ballot>>> GetBallotsByElection(Guid electionGuid)
    {
        return await _context.Ballots
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .Include(b => b.Location)
            .Include(b => b.Votes)
            .OrderBy(b => b.BallotNumAtComputer)
            .ToListAsync();
    }

    [HttpGet("{guid}")]
    public async Task<ActionResult<Ballot>> GetBallot(Guid guid)
    {
        var ballot = await _context.Ballots
            .Include(b => b.Location)
            .Include(b => b.Votes)
            .FirstOrDefaultAsync(b => b.BallotGuid == guid);

        if (ballot == null)
        {
            return NotFound();
        }

        return ballot;
    }

    [HttpPost]
    public async Task<ActionResult<Ballot>> CreateBallot(Ballot ballot)
    {
        if (ballot.BallotGuid == Guid.Empty)
        {
            ballot.BallotGuid = Guid.NewGuid();
        }

        _context.Ballots.Add(ballot);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBallot), new { guid = ballot.BallotGuid }, ballot);
    }

    [HttpPut("{guid}")]
    public async Task<IActionResult> UpdateBallot(Guid guid, Ballot ballot)
    {
        if (guid != ballot.BallotGuid)
        {
            return BadRequest();
        }

        _context.Entry(ballot).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await BallotExists(guid))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{guid}")]
    public async Task<IActionResult> DeleteBallot(Guid guid)
    {
        var ballot = await _context.Ballots.FirstOrDefaultAsync(b => b.BallotGuid == guid);
        if (ballot == null)
        {
            return NotFound();
        }

        _context.Ballots.Remove(ballot);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> BallotExists(Guid guid)
    {
        return await _context.Ballots.AnyAsync(b => b.BallotGuid == guid);
    }
}
