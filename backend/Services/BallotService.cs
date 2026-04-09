using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.DTOs.Ballots;
using Backend.DTOs.Votes;
using Backend.Models;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service for managing ballot operations including creation, retrieval, updates, and deletion.
/// Provides functionality to handle ballots within elections and locations.
/// </summary>
public class BallotService : IBallotService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<BallotService> _logger;

    /// <summary>
    /// Initializes a new instance of the BallotService.
    /// </summary>
    /// <param name="context">The main database context for accessing ballot data.</param>
    /// <param name="mapper">Mapster instance for object mapping operations.</param>
    /// <param name="logger">Logger for recording ballot service operations.</param>
    public BallotService(MainDbContext context, IMapper mapper, ILogger<BallotService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves a paginated list of ballots for a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based). Default is 1.</param>
    /// <param name="pageSize">The number of ballots per page. Default is 50.</param>
    /// <returns>A paginated response containing ballot DTOs with their associated votes and location information.</returns>
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

    /// <summary>
    /// Retrieves a specific ballot by its unique identifier.
    /// </summary>
    /// <param name="ballotGuid">The unique identifier of the ballot.</param>
    /// <returns>A BallotDto containing the ballot information with votes and location details, or null if not found.</returns>
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

    /// <summary>
    /// Creates a new ballot for an election.
    /// </summary>
    /// <param name="createDto">The data transfer object containing ballot creation information.</param>
    /// <returns>A BallotDto representing the created ballot.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the election is not found.</exception>
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
            StatusCode = BallotStatus.Raw,
            RowVersion = new byte[8],
        };

        _context.Ballots.Add(ballot);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created ballot {BallotGuid} - {BallotCode} at location {LocationGuid}",
            ballot.BallotGuid, ballot.BallotCode, ballot.LocationGuid);

        return await GetBallotByGuidAsync(ballot.BallotGuid) ?? _mapper.Map<BallotDto>(ballot);
    }

    /// <summary>
    /// Updates an existing ballot with new information.
    /// </summary>
    /// <param name="ballotGuid">The unique identifier of the ballot to update.</param>
    /// <param name="updateDto">The data transfer object containing updated ballot information.</param>
    /// <returns>A BallotDto representing the updated ballot, or null if the ballot was not found.</returns>
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

    /// <summary>
    /// Deletes a ballot by its unique identifier.
    /// </summary>
    /// <param name="ballotGuid">The unique identifier of the ballot to delete.</param>
    /// <returns>True if the ballot was successfully deleted, false if the ballot was not found.</returns>
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



