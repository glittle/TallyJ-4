using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TallyJ4.EF.Context;
using TallyJ4.EF.Models;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PeopleController : ControllerBase
{
    private readonly MainDbContext _context;
    private readonly ILogger<PeopleController> _logger;

    public PeopleController(MainDbContext context, ILogger<PeopleController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("election/{electionGuid}")]
    public async Task<ActionResult<IEnumerable<Person>>> GetPeopleByElection(Guid electionGuid)
    {
        return await _context.People
            .Where(p => p.ElectionGuid == electionGuid)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();
    }

    [HttpGet("{guid}")]
    public async Task<ActionResult<Person>> GetPerson(Guid guid)
    {
        var person = await _context.People
            .FirstOrDefaultAsync(p => p.PersonGuid == guid);

        if (person == null)
        {
            return NotFound();
        }

        return person;
    }

    [HttpPost]
    public async Task<ActionResult<Person>> CreatePerson(Person person)
    {
        if (person.PersonGuid == Guid.Empty)
        {
            person.PersonGuid = Guid.NewGuid();
        }

        _context.People.Add(person);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetPerson), new { guid = person.PersonGuid }, person);
    }

    [HttpPut("{guid}")]
    public async Task<IActionResult> UpdatePerson(Guid guid, Person person)
    {
        if (guid != person.PersonGuid)
        {
            return BadRequest();
        }

        _context.Entry(person).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await PersonExists(guid))
            {
                return NotFound();
            }
            throw;
        }

        return NoContent();
    }

    [HttpDelete("{guid}")]
    public async Task<IActionResult> DeletePerson(Guid guid)
    {
        var person = await _context.People.FirstOrDefaultAsync(p => p.PersonGuid == guid);
        if (person == null)
        {
            return NotFound();
        }

        _context.People.Remove(person);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private async Task<bool> PersonExists(Guid guid)
    {
        return await _context.People.AnyAsync(p => p.PersonGuid == guid);
    }
}
