using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs.People;
using Backend.Models;
using Backend.Services;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing people operations including creation, retrieval, updates, and deletion.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PeopleController : ControllerBase
{
    private readonly IPeopleService _peopleService;
    private readonly ILogger<PeopleController> _logger;

    /// <summary>
    /// Initializes a new instance of the PeopleController.
    /// </summary>
    /// <param name="peopleService">The people service for person operations.</param>
    /// <param name="logger">The logger for recording operations.</param>
    public PeopleController(IPeopleService peopleService, ILogger<PeopleController> logger)
    {
        _peopleService = peopleService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of people for the specified election with optional filtering.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <param name="pageNumber">The page number (starting from 1).</param>
    /// <param name="pageSize">The number of items per page (1-200).</param>
    /// <param name="search">Optional search term to filter people by name.</param>
    /// <param name="canVote">Optional filter for people who can vote.</param>
    /// <param name="canReceiveVotes">Optional filter for people who can receive votes.</param>
    /// <returns>A paginated response containing the people.</returns>
    [HttpGet("{electionGuid}/getPeople")]
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

    /// <summary>
    /// Searches for people within an election by name.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election to search in.</param>
    /// <param name="q">The search query string.</param>
    /// <returns>A list of people matching the search criteria.</returns>
    [HttpGet("{electionGuid}/searchPeople")]
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

    /// <summary>
    /// Gets all candidates (people who can receive votes) for the specified election.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>A list of all candidates with phonetic sound codes.</returns>
    [HttpGet("{electionGuid}/getCandidates")]
    public async Task<ActionResult<ApiResponse<List<PersonDto>>>> GetCandidates(Guid electionGuid)
    {
        var candidates = await _peopleService.GetCandidatesAsync(electionGuid);
        return Ok(ApiResponse<List<PersonDto>>.SuccessResponse(candidates));
    }

    /// <summary>
    /// Gets all people in the specified election for ballot entry, including ineligible persons.
    /// VoteCount reflects the live ballot vote count, not the tally result.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>A list of all people with live vote counts.</returns>
    [HttpGet("{electionGuid}/getAllForBallotEntry")]
    public async Task<ActionResult<ApiResponse<List<PersonDto>>>> GetAllForBallotEntry(Guid electionGuid)
    {
        var people = await _peopleService.GetAllForBallotEntryAsync(electionGuid);
        return Ok(ApiResponse<List<PersonDto>>.SuccessResponse(people));
    }

    /// <summary>
    /// Gets all people in the specified election as a lightweight list for display.
    /// Returns only essential fields (full name, area, eligibility) for efficient list rendering.
    /// </summary>
    /// <param name="electionGuid">The GUID of the election.</param>
    /// <returns>A list of lightweight person DTOs.</returns>
    [HttpGet("{electionGuid}/getAllPeople")]
    public async Task<ActionResult<ApiResponse<List<PersonListDto>>>> GetAllPeople(Guid electionGuid)
    {
        var people = await _peopleService.GetAllPeopleForListAsync(electionGuid);
        return Ok(ApiResponse<List<PersonListDto>>.SuccessResponse(people));
    }

    /// <summary>
    /// Gets detailed information about a specific person, including all editable fields,
    /// registration history, and vote history.
    /// </summary>
    /// <param name="guid">The GUID of the person.</param>
    /// <returns>Detailed person information with history.</returns>
    [HttpGet("{guid}/getPersonDetails")]
    public async Task<ActionResult<ApiResponse<PersonDetailDto>>> GetPersonDetails(Guid guid)
    {
        var person = await _peopleService.GetPersonDetailsAsync(guid);

        if (person == null)
        {
            return NotFound(ApiResponse<PersonDetailDto>.ErrorResponse("Person not found"));
        }

        return Ok(ApiResponse<PersonDetailDto>.SuccessResponse(person));
    }

    /// <summary>
    /// Gets a specific person by their GUID.
    /// </summary>
    /// <param name="guid">The GUID of the person.</param>
    /// <returns>The person information.</returns>
    [HttpGet("{guid}/getPerson")]
    public async Task<ActionResult<ApiResponse<PersonDto>>> GetPerson(Guid guid)
    {
        var person = await _peopleService.GetPersonByGuidAsync(guid);

        if (person == null)
        {
            return NotFound(ApiResponse<PersonDto>.ErrorResponse("Person not found"));
        }

        return Ok(ApiResponse<PersonDto>.SuccessResponse(person));
    }

    /// <summary>
    /// Creates a new person.
    /// </summary>
    /// <param name="createDto">The person creation data.</param>
    /// <returns>The created person information.</returns>
    [HttpPost("createPerson")]
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

    /// <summary>
    /// Updates an existing person.
    /// </summary>
    /// <param name="guid">The GUID of the person to update.</param>
    /// <param name="updateDto">The updated person data.</param>
    /// <returns>The updated person information.</returns>
    [HttpPut("{guid}/updatePerson")]
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

    /// <summary>
    /// Deletes a person by their GUID.
    /// </summary>
    /// <param name="guid">The GUID of the person to delete.</param>
    /// <returns>No content if successful, or not found if the person doesn't exist.</returns>
    [HttpDelete("{guid}/deletePerson")]
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



