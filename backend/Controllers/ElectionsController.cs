using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallyJ4.EF.Context;
using TallyJ4.Domain.Entities;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ElectionsController : ControllerBase
{
    private readonly MainDbContext _context;
    private readonly ILogger<ElectionsController> _logger;

    public ElectionsController(MainDbContext context, ILogger<ElectionsController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Election>>> GetElections()
    {
        return await _context.Elections
            .OrderByDescending(e => e.DateOfElection)
            .ToListAsync();
    }

    [HttpGet("{guid}")]
    public async Task<ActionResult<Election>> GetElection(Guid guid)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == guid);

        if (election == null)
        {
            return NotFound();
        }

        return election;
    }

    [HttpPost]
    public async Task<ActionResult<Election>> CreateElection(Election election)
    {
        if (election.ElectionGuid == Guid.Empty)
        {
            election.ElectionGuid = Guid.NewGuid();
        }

        _context.Elections.Add(election);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetElection), new { guid = election.ElectionGuid }, election);
    }

    [HttpPut("{guid}")]
    public async Task<IActionResult> UpdateElection(Guid guid, Election election)
    {
        if (guid != election.ElectionGuid)
        {
            return BadRequest();
        }

        _context.Entry(election).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await ElectionExists(guid))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{guid}")]
    public async Task<IActionResult> DeleteElection(Guid guid)
    {
        var election = await _context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == guid);
        if (election == null)
        {
            return NotFound();
        }

        _context.Elections.Remove(election);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> ElectionExists(Guid guid)
    {
        return await _context.Elections.AnyAsync(e => e.ElectionGuid == guid);
    }
}
