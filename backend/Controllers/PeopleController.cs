using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.People;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PeopleController : ControllerBase
{
    private readonly IPeopleService _peopleService;
    private readonly ILogger<PeopleController> _logger;

    public PeopleController(IPeopleService peopleService, ILogger<PeopleController> logger)
    {
        _peopleService = peopleService;
        _logger = logger;
    }

    [HttpGet("election/{electionGuid}")]
    public async Task<ActionResult<PaginatedResponse<PersonDto>>> GetPeopleByElection(
        Guid electionGuid,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? search = null,
        [FromQuery] bool? canVote = null,
        [FromQuery] bool? canReceiveVotes = null)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 200)
        {
            return BadRequest(new { message = "Invalid pagination parameters. PageNumber must be >= 1, PageSize must be between 1 and 200." });
        }

        var result = await _peopleService.GetPeopleByElectionAsync(electionGuid, pageNumber, pageSize, search, canVote, canReceiveVotes);
        return Ok(result);
    }

    [HttpGet("election/{electionGuid}/search")]
    public async Task<ActionResult<ApiResponse<List<PersonDto>>>> SearchPeople(
        Guid electionGuid,
        [FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
        {
            return BadRequest(ApiResponse<List<PersonDto>>.ErrorResponse("Search query is required"));
        }

        var results = await _peopleService.SearchPeopleAsync(electionGuid, q);
        return Ok(ApiResponse<List<PersonDto>>.SuccessResponse(results));
    }

    [HttpGet("{guid}")]
    public async Task<ActionResult<ApiResponse<PersonDto>>> GetPerson(Guid guid)
    {
        var person = await _peopleService.GetPersonByGuidAsync(guid);

        if (person == null)
        {
            return NotFound(ApiResponse<PersonDto>.ErrorResponse("Person not found"));
        }

        return Ok(ApiResponse<PersonDto>.SuccessResponse(person));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PersonDto>>> CreatePerson(CreatePersonDto createDto)
    {
        try
        {
            var person = await _peopleService.CreatePersonAsync(createDto);

            return CreatedAtAction(
                nameof(GetPerson),
                new { guid = person.PersonGuid },
                ApiResponse<PersonDto>.SuccessResponse(person, "Person created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PersonDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{guid}")]
    public async Task<ActionResult<ApiResponse<PersonDto>>> UpdatePerson(Guid guid, UpdatePersonDto updateDto)
    {
        try
        {
            var person = await _peopleService.UpdatePersonAsync(guid, updateDto);

            if (person == null)
            {
                return NotFound(ApiResponse<PersonDto>.ErrorResponse("Person not found"));
            }

            return Ok(ApiResponse<PersonDto>.SuccessResponse(person, "Person updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<PersonDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{guid}")]
    public async Task<IActionResult> DeletePerson(Guid guid)
    {
        var success = await _peopleService.DeletePersonAsync(guid);

        if (!success)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Person not found"));
        }

        return NoContent();
    }
}
