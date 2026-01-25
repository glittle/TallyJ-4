using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TallyJ4.DTOs.Ballots;
using TallyJ4.DTOs.Votes;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Entities;
using TallyJ4.Models;

namespace TallyJ4.Services;

public class BallotService : IBallotService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<BallotService> _logger;

    public BallotService(MainDbContext context, IMapper mapper, ILogger<BallotService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResponse<BallotDto>> GetBallotsByElectionAsync(Guid electionGuid, int pageNumber = 1, int pageSize = 50)
    {
        var query = _context.Ballots
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .Include(b => b.Location)
            .Include(b => b.Votes)
                .ThenInclude(v => v.Person)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var ballots = await query
            .OrderBy(b => b.ComputerCode)
            .ThenBy(b => b.BallotNumAtComputer)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var ballotDtos = ballots.Select(b => MapToBallotDto(b)).ToList();

        return PaginatedResponse<BallotDto>.Create(ballotDtos, pageNumber, pageSize, totalCount);
    }

    public async Task<BallotDto?> GetBallotByGuidAsync(Guid ballotGuid)
    {
        var ballot = await _context.Ballots
            .Include(b => b.Location)
            .Include(b => b.Votes)
                .ThenInclude(v => v.Person)
            .FirstOrDefaultAsync(b => b.BallotGuid == ballotGuid);

        if (ballot == null)
        {
            return null;
        }

        return MapToBallotDto(ballot);
    }

    public async Task<BallotDto> CreateBallotAsync(CreateBallotDto createDto)
    {
        // Find or create a default location for the election
        var location = await _context.Locations.FirstOrDefaultAsync(l => l.ElectionGuid == createDto.ElectionGuid);

        if (location == null)
        {
            // For testing purposes, create a default location if none exists
            // In production, locations should be created through proper setup
            var election = await _context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == createDto.ElectionGuid);
            if (election != null)
            {
                location = new Location
                {
                    LocationGuid = Guid.NewGuid(),
                    ElectionGuid = election.ElectionGuid,
                    Name = "Default Location"
                };
                _context.Locations.Add(location);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Created default location {LocationGuid} for election {ElectionGuid}",
                    location.LocationGuid, election.ElectionGuid);
            }
            else
            {
                throw new InvalidOperationException($"Election with GUID '{createDto.ElectionGuid}' not found");
            }
        }

        var nextBallotNum = await _context.Ballots
            .Where(b => b.LocationGuid == location.LocationGuid && b.ComputerCode == createDto.ComputerCode)
            .MaxAsync(b => (int?)b.BallotNumAtComputer) ?? 0;

        nextBallotNum++;

        var ballot = new Ballot
        {
            BallotGuid = Guid.NewGuid(),
            LocationGuid = location.LocationGuid,
            ComputerCode = createDto.ComputerCode,
            BallotNumAtComputer = nextBallotNum,
            BallotCode = $"{createDto.ComputerCode}{nextBallotNum}",
            StatusCode = "New",
            RowVersion = new byte[8]
        };

        _context.Ballots.Add(ballot);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created ballot {BallotGuid} - {BallotCode} at location {LocationGuid}", 
            ballot.BallotGuid, ballot.BallotCode, ballot.LocationGuid);

        return await GetBallotByGuidAsync(ballot.BallotGuid) ?? _mapper.Map<BallotDto>(ballot);
    }

    public async Task<BallotDto?> UpdateBallotAsync(Guid ballotGuid, UpdateBallotDto updateDto)
    {
        var ballot = await _context.Ballots.FirstOrDefaultAsync(b => b.BallotGuid == ballotGuid);

        if (ballot == null)
        {
            return null;
        }

        ballot.StatusCode = updateDto.StatusCode;
        ballot.Teller1 = updateDto.Teller1;
        ballot.Teller2 = updateDto.Teller2;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated ballot {BallotGuid}", ballotGuid);

        return await GetBallotByGuidAsync(ballotGuid);
    }

    public async Task<bool> DeleteBallotAsync(Guid ballotGuid)
    {
        var ballot = await _context.Ballots.FirstOrDefaultAsync(b => b.BallotGuid == ballotGuid);

        if (ballot == null)
        {
            return false;
        }

        _context.Ballots.Remove(ballot);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted ballot {BallotGuid}", ballotGuid);

        return true;
    }

    private BallotDto MapToBallotDto(Ballot ballot)
    {
        var dto = _mapper.Map<BallotDto>(ballot);
        dto.LocationName = ballot.Location.Name;
        dto.BallotCode = ballot.BallotCode ?? $"{ballot.ComputerCode}{ballot.BallotNumAtComputer}";
        dto.VoteCount = ballot.Votes.Count;
        dto.Votes = ballot.Votes.Select(v => 
        {
            var voteDto = _mapper.Map<VoteDto>(v);
            voteDto.PersonFullName = v.Person?.FullName;
            return voteDto;
        }).ToList();

        return dto;
    }
}
