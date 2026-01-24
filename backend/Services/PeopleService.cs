using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TallyJ4.DTOs.People;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Entities;
using TallyJ4.Models;

namespace TallyJ4.Services;

public class PeopleService : IPeopleService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<PeopleService> _logger;

    public PeopleService(MainDbContext context, IMapper mapper, ILogger<PeopleService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

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
        
        person.FullName = string.IsNullOrWhiteSpace(person.FirstName) 
            ? person.LastName 
            : $"{person.LastName}, {person.FirstName}";
        
        person.FullNameFl = string.IsNullOrWhiteSpace(person.FirstName) 
            ? person.LastName 
            : $"{person.FirstName} {person.LastName}";

        _context.People.Add(person);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created person {PersonGuid} - {FullName}", person.PersonGuid, person.FullName);

        return await GetPersonByGuidAsync(person.PersonGuid) ?? _mapper.Map<PersonDto>(person);
    }

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
        
        person.FullName = string.IsNullOrWhiteSpace(person.FirstName) 
            ? person.LastName 
            : $"{person.LastName}, {person.FirstName}";
        
        person.FullNameFl = string.IsNullOrWhiteSpace(person.FirstName) 
            ? person.LastName 
            : $"{person.FirstName} {person.LastName}";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated person {PersonGuid}", personGuid);

        return await GetPersonByGuidAsync(personGuid);
    }

    public async Task<bool> DeletePersonAsync(Guid personGuid)
    {
        var person = await _context.People.FirstOrDefaultAsync(p => p.PersonGuid == personGuid);

        if (person == null)
        {
            return false;
        }

        _context.People.Remove(person);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted person {PersonGuid}", personGuid);

        return true;
    }

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
}
