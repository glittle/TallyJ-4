using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TallyJ4.DTOs.Elections;
using TallyJ4.EF.Context;
using TallyJ4.EF.Models;
using TallyJ4.Models;

namespace TallyJ4.Services;

public class ElectionService : IElectionService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ElectionService> _logger;

    public ElectionService(MainDbContext context, IMapper mapper, ILogger<ElectionService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResponse<ElectionSummaryDto>> GetElectionsAsync(int pageNumber = 1, int pageSize = 10, string? status = null)
    {
        var query = _context.Elections.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(e => e.TallyStatus == status);
        }

        var totalCount = await query.CountAsync();

        var elections = await query
            .OrderByDescending(e => e.DateOfElection)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(e => e.People)
            .Include(e => e.Locations)
                .ThenInclude(l => l.Ballots)
            .ToListAsync();

        var electionDtos = elections.Select(e => new ElectionSummaryDto
        {
            ElectionGuid = e.ElectionGuid,
            Name = e.Name,
            DateOfElection = e.DateOfElection,
            TallyStatus = e.TallyStatus,
            VoterCount = e.People.Count(p => p.CanVote == true),
            BallotCount = e.Locations.SelectMany(l => l.Ballots).Count()
        }).ToList();

        return PaginatedResponse<ElectionSummaryDto>.Create(electionDtos, pageNumber, pageSize, totalCount);
    }

    public async Task<ElectionDto?> GetElectionByGuidAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .Include(e => e.People)
            .Include(e => e.Locations)
                .ThenInclude(l => l.Ballots)
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return null;
        }

        var dto = _mapper.Map<ElectionDto>(election);
        dto.VoterCount = election.People.Count(p => p.CanVote == true);
        dto.BallotCount = election.Locations.SelectMany(l => l.Ballots).Count();
        dto.LocationCount = election.Locations.Count;

        return dto;
    }

    public async Task<ElectionDto> CreateElectionAsync(CreateElectionDto createDto)
    {
        var election = _mapper.Map<Election>(createDto);
        election.ElectionGuid = Guid.NewGuid();
        election.TallyStatus = "Setup";
        election.RowVersion = new byte[8];

        _context.Elections.Add(election);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created election {ElectionGuid} - {Name}", election.ElectionGuid, election.Name);

        return await GetElectionByGuidAsync(election.ElectionGuid) ?? _mapper.Map<ElectionDto>(election);
    }

    public async Task<ElectionDto?> UpdateElectionAsync(Guid electionGuid, UpdateElectionDto updateDto)
    {
        var election = await _context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return null;
        }

        _mapper.Map(updateDto, election);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated election {ElectionGuid}", electionGuid);

        return await GetElectionByGuidAsync(electionGuid);
    }

    public async Task<bool> DeleteElectionAsync(Guid electionGuid)
    {
        var election = await _context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return false;
        }

        _context.Elections.Remove(election);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted election {ElectionGuid}", electionGuid);

        return true;
    }

    public async Task<ElectionDto?> GetElectionSummaryAsync(Guid electionGuid)
    {
        return await GetElectionByGuidAsync(electionGuid);
    }
}
