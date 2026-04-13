using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.Domain.Helpers;
using Backend.DTOs.People;
using Backend.DTOs.SignalR;
using Backend.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service for managing people operations including creation, retrieval, updates, and deletion.
/// Provides functionality to handle people within elections and their voting capabilities.
/// </summary>
public class PeopleService : IPeopleService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<PeopleService> _logger;
    private readonly ISignalRNotificationService _signalRNotificationService;

    /// <summary>
    /// Initializes a new instance of the PeopleService.
    /// </summary>
    /// <param name="context">The main database context for accessing people data.</param>
    /// <param name="mapper">Mapster instance for object mapping operations.</param>
    /// <param name="logger">Logger for recording people service operations.</param>
    /// <param name="signalRNotificationService">Service for sending real-time notifications about people updates.</param>
    public PeopleService(MainDbContext context, IMapper mapper, ILogger<PeopleService> logger, ISignalRNotificationService signalRNotificationService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _signalRNotificationService = signalRNotificationService;
    }

    /// <summary>
    /// Retrieves a paginated list of people for a specific election with optional filtering.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based). Default is 1.</param>
    /// <param name="pageSize">The number of people per page. Default is 50.</param>
    /// <param name="search">Optional search string to filter people by name, email, or Bahai ID.</param>
    /// <param name="canVote">Optional filter for people who can vote.</param>
    /// <param name="canReceiveVotes">Optional filter for people who can receive votes.</param>
    /// <returns>A paginated response containing person DTOs with their vote counts.</returns>
    public async Task<PaginatedResponse<PersonDto>> GetPeopleByElectionAsync(
        Guid electionGuid,
        int pageNumber = 1,
        int pageSize = 50,
        string? search = null,
        bool? canVote = null,
        bool? canReceiveVotes = null)
    {
        var query = _context.People
            .Where(p => p.ElectionGuid == electionGuid)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            query = query.Where(p =>
                (p.FirstName != null && p.FirstName.ToLower().Contains(searchLower)) ||
                p.LastName.ToLower().Contains(searchLower) ||
                (p.FullName != null && p.FullName.ToLower().Contains(searchLower)) ||
                (p.Email != null && p.Email.ToLower().Contains(searchLower)) ||
                (p.BahaiId != null && p.BahaiId.ToLower().Contains(searchLower)));
        }

        if (canVote.HasValue)
        {
            query = query.Where(p => p.CanVote == canVote.Value);
        }

        if (canReceiveVotes.HasValue)
        {
            query = query.Where(p => p.CanReceiveVotes == canReceiveVotes.Value);
        }

        var totalCount = await query.CountAsync();

        var people = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(p => p.Results)
            .ToListAsync();

        var peopleDtos = people.Select(p =>
        {
            var dto = _mapper.Map<PersonDto>(p);
            dto.VoteCount = p.Results.FirstOrDefault()?.VoteCount ?? 0;
            return dto;
        }).ToList();

        return PaginatedResponse<PersonDto>.Create(peopleDtos, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Retrieves a specific person by their unique identifier.
    /// </summary>
    /// <param name="personGuid">The unique identifier of the person.</param>
    /// <returns>A PersonDto containing the person information with vote count, or null if not found.</returns>
    public async Task<PersonDto?> GetPersonByGuidAsync(Guid personGuid)
    {
        var person = await _context.People
            .Include(p => p.Results)
            .FirstOrDefaultAsync(p => p.PersonGuid == personGuid);

        if (person == null)
        {
            return null;
        }

        var dto = _mapper.Map<PersonDto>(person);
        dto.VoteCount = person.Results.FirstOrDefault()?.VoteCount ?? 0;

        return dto;
    }

    /// <summary>
    /// Creates a new person for an election.
    /// </summary>
    /// <param name="createDto">The data transfer object containing person creation information.</param>
    /// <returns>A PersonDto representing the created person.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a person with the same email or phone already exists.</exception>
    public async Task<PersonDto> CreatePersonAsync(CreatePersonDto createDto)
    {
        var existingPerson = await _context.People
            .FirstOrDefaultAsync(p => p.ElectionGuid == createDto.ElectionGuid &&
                                     ((p.Email != null && createDto.Email != null && p.Email == createDto.Email) ||
                                      (p.Phone != null && createDto.Phone != null && p.Phone == createDto.Phone)));

        if (existingPerson != null)
        {
            if (existingPerson.Email == createDto.Email)
            {
                throw new InvalidOperationException($"A person with email '{createDto.Email}' already exists in this election");
            }
            if (existingPerson.Phone == createDto.Phone)
            {
                throw new InvalidOperationException($"A person with phone '{createDto.Phone}' already exists in this election");
            }
        }

        var person = _mapper.Map<Person>(createDto);
        person.PersonGuid = Guid.NewGuid();
        person.RowVersion = new byte[8];
        person.FullName = PersonNameHelper.ComputeFullName(person);
        person.FullNameFl = PersonNameHelper.ComputeFullNameFl(person);

        // Sync eligibility based on IneligibleReasonGuid
        SyncEligibility(person);

        _context.People.Add(person);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created person {PersonGuid} - {FullName}", person.PersonGuid, person.FullName);

        await _signalRNotificationService.SendPersonUpdateAsync(new PersonUpdateDto
        {
            ElectionGuid = person.ElectionGuid,
            PersonGuid = person.PersonGuid,
            Action = "added",
            FirstName = person.FirstName,
            LastName = person.LastName,
            UpdatedAt = DateTime.UtcNow
        });

        return await GetPersonByGuidAsync(person.PersonGuid) ?? _mapper.Map<PersonDto>(person);
    }

    /// <summary>
    /// Updates an existing person with new information.
    /// </summary>
    /// <param name="personGuid">The unique identifier of the person to update.</param>
    /// <param name="updateDto">The data transfer object containing updated person information.</param>
    /// <returns>A PersonDto representing the updated person, or null if the person was not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when a person with the same email or phone already exists.</exception>
    public async Task<PersonDto?> UpdatePersonAsync(Guid personGuid, UpdatePersonDto updateDto)
    {
        var person = await _context.People.FirstOrDefaultAsync(p => p.PersonGuid == personGuid);

        if (person == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(updateDto.Email) && updateDto.Email != person.Email)
        {
            var emailExists = await _context.People
                .AnyAsync(p => p.ElectionGuid == person.ElectionGuid &&
                              p.PersonGuid != personGuid &&
                              p.Email == updateDto.Email);

            if (emailExists)
            {
                throw new InvalidOperationException($"A person with email '{updateDto.Email}' already exists in this election");
            }
        }

        if (!string.IsNullOrWhiteSpace(updateDto.Phone) && updateDto.Phone != person.Phone)
        {
            var phoneExists = await _context.People
                .AnyAsync(p => p.ElectionGuid == person.ElectionGuid &&
                              p.PersonGuid != personGuid &&
                              p.Phone == updateDto.Phone);

            if (phoneExists)
            {
                throw new InvalidOperationException($"A person with phone '{updateDto.Phone}' already exists in this election");
            }
        }

        _mapper.Map(updateDto, person);

        person.FullName = PersonNameHelper.ComputeFullName(person);
        person.FullNameFl = PersonNameHelper.ComputeFullNameFl(person);

        // Sync eligibility based on IneligibleReasonGuid
        SyncEligibility(person);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated person {PersonGuid}", personGuid);

        await _signalRNotificationService.SendPersonUpdateAsync(new PersonUpdateDto
        {
            ElectionGuid = person.ElectionGuid,
            PersonGuid = person.PersonGuid,
            Action = "updated",
            FirstName = person.FirstName,
            LastName = person.LastName,
            UpdatedAt = DateTime.UtcNow
        });

        return await GetPersonByGuidAsync(personGuid);
    }

    /// <summary>
    /// Deletes a person by their unique identifier.
    /// </summary>
    /// <param name="personGuid">The unique identifier of the person to delete.</param>
    /// <returns>True if the person was successfully deleted, false if the person was not found.</returns>
    public async Task<bool> DeletePersonAsync(Guid personGuid)
    {
        var person = await _context.People.FirstOrDefaultAsync(p => p.PersonGuid == personGuid);

        if (person == null)
        {
            return false;
        }

        var electionGuid = person.ElectionGuid;
        var firstName = person.FirstName;
        var lastName = person.LastName;

        _context.People.Remove(person);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted person {PersonGuid}", personGuid);

        await _signalRNotificationService.SendPersonUpdateAsync(new PersonUpdateDto
        {
            ElectionGuid = electionGuid,
            PersonGuid = personGuid,
            Action = "deleted",
            FirstName = firstName,
            LastName = lastName,
            UpdatedAt = DateTime.UtcNow
        });

        return true;
    }

    /// <summary>
    /// Searches for people within an election by name, email, or Bahai ID.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to search within.</param>
    /// <param name="query">The search query string.</param>
    /// <returns>A list of up to 20 PersonDto objects matching the search criteria.</returns>
    public async Task<List<PersonDto>> SearchPeopleAsync(Guid electionGuid, string query)
    {
        var searchLower = query.ToLower();

        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid &&
                       ((p.FirstName != null && p.FirstName.ToLower().Contains(searchLower)) ||
                        p.LastName.ToLower().Contains(searchLower) ||
                        (p.FullName != null && p.FullName.ToLower().Contains(searchLower)) ||
                        (p.Email != null && p.Email.ToLower().Contains(searchLower)) ||
                        (p.BahaiId != null && p.BahaiId.ToLower().Contains(searchLower))))
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(20)
            .Include(p => p.Results)
            .ToListAsync();

        return people.Select(p =>
        {
            var dto = _mapper.Map<PersonDto>(p);
            dto.VoteCount = p.Results.FirstOrDefault()?.VoteCount ?? 0;
            return dto;
        }).ToList();
    }

    /// <summary>
    /// Retrieves all candidates (people who can receive votes) for a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A list of all candidates ordered by last name and first name, including phonetic sound codes.</returns>
    public async Task<List<PersonDto>> GetCandidatesAsync(Guid electionGuid)
    {
        var candidates = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanReceiveVotes == true)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Include(p => p.Results)
            .ToListAsync();

        return candidates.Select(p =>
        {
            var dto = _mapper.Map<PersonDto>(p);
            dto.VoteCount = p.Results.FirstOrDefault()?.VoteCount ?? 0;
            return dto;
        }).ToList();
    }

    /// <summary>
    /// Retrieves all people in an election for ballot entry, including ineligible persons.
    /// VoteCount is computed live from the Vote table rather than the Result (tally) table.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A list of all people ordered by last name and first name.</returns>
    public async Task<List<PersonDto>> GetAllForBallotEntryAsync(Guid electionGuid)
    {
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        var liveVoteCounts = await _context.Votes
            .Where(v => v.PersonGuid != null && v.Ballot.Location.ElectionGuid == electionGuid)
            .GroupBy(v => v.PersonGuid!.Value)
            .Select(g => new { PersonGuid = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.PersonGuid, x => x.Count);

        return people.Select(p =>
        {
            var dto = _mapper.Map<PersonDto>(p);
            dto.VoteCount = liveVoteCounts.TryGetValue(p.PersonGuid, out var count) ? count : 0;
            return dto;
        }).ToList();
    }

    /// <summary>
    /// Synchronizes the CanVote and CanReceiveVotes properties based on the IneligibleReasonGuid.
    /// </summary>
    /// <param name="person">The person entity to update.</param>
    private void SyncEligibility(Person person)
    {
        if (person.IneligibleReasonGuid.HasValue)
        {
            var reason = IneligibleReasonEnum.GetByGuid(person.IneligibleReasonGuid.Value);
            if (reason != null)
            {
                person.CanVote = reason.CanVote;
                person.CanReceiveVotes = reason.CanReceiveVotes;
            }
            else
            {
                // Unknown GUID - set to ineligible for both
                person.CanVote = false;
                person.CanReceiveVotes = false;
                _logger.LogWarning("Unknown IneligibleReasonGuid {Guid} for person {PersonGuid}, setting CanVote=false, CanReceiveVotes=false",
                    person.IneligibleReasonGuid, person.PersonGuid);
            }
        }
        else
        {
            // No ineligibility reason - fully eligible
            person.CanVote = true;
            person.CanReceiveVotes = true;
        }
    }

    /// <summary>
    /// Retrieves all people in an election as a lightweight list for display.
    /// Returns only the essential fields needed for the list view.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A list of lightweight person DTOs ordered by last name and first name.</returns>
    public async Task<List<PersonListDto>> GetAllPeopleForListAsync(Guid electionGuid)
    {
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return _mapper.Map<List<PersonListDto>>(people);
    }

    /// <summary>
    /// Retrieves detailed information about a specific person, including registration history.
    /// Includes all editable fields.
    /// </summary>
    /// <param name="personGuid">The unique identifier of the person.</param>
    /// <returns>Detailed person data with history, or null if not found.</returns>
    public async Task<PersonDetailDto?> GetPersonDetailsAsync(Guid personGuid)
    {
        var person = await _context.People
            .Include(p => p.Results)
            .FirstOrDefaultAsync(p => p.PersonGuid == personGuid);

        if (person == null)
        {
            return null;
        }

        var dto = _mapper.Map<PersonDetailDto>(person);

        // Get vote count (how many votes this person has received)
        dto.VoteCount = person.Results.FirstOrDefault()?.VoteCount ?? 0;

        return dto;
    }
}



