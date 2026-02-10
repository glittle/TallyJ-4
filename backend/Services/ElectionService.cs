using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TallyJ4.DTOs.Elections;
using TallyJ4.DTOs.SignalR;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Entities;
using TallyJ4.Models;
using System.Security.Claims;

namespace TallyJ4.Services;

/// <summary>
/// Service for managing election operations including creation, retrieval, updates, and deletion.
/// Provides functionality to handle elections and their associated data.
/// </summary>
public class ElectionService : IElectionService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ElectionService> _logger;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the ElectionService.
    /// </summary>
    /// <param name="context">The main database context for accessing election data.</param>
    /// <param name="mapper">AutoMapper instance for object mapping operations.</param>
    /// <param name="logger">Logger for recording election service operations.</param>
    /// <param name="signalRNotificationService">Service for sending real-time notifications about election updates.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for retrieving current user information.</param>
    public ElectionService(MainDbContext context, IMapper mapper, ILogger<ElectionService> logger, ISignalRNotificationService signalRNotificationService, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _signalRNotificationService = signalRNotificationService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Retrieves a paginated list of elections with optional status filtering. Glen reviewed.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (1-based). Default is 1.</param>
    /// <param name="pageSize">The number of elections per page. Default is 10.</param>
    /// <param name="status">Optional status filter to apply to elections.</param>
    /// <returns>A paginated response containing election summary DTOs.</returns>
    public async Task<PaginatedResponse<ElectionSummaryDto>> GetElectionsAsync(int pageNumber = 1, int pageSize = 10, string? status = null)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("GetElectionsAsync: Could not parse user ID from claims");
            return PaginatedResponse<ElectionSummaryDto>.Create(new List<ElectionSummaryDto>(), pageNumber, pageSize, 0);
        }

        var query = _context.Elections
            .Where(e => _context.JoinElectionUsers.Any(jeu => jeu.ElectionGuid == e.ElectionGuid && jeu.UserId == userId));

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

    /// <summary>
    /// Retrieves a specific election by its unique identifier.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>An ElectionDto containing the election information, or null if not found.</returns>
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

    /// <summary>
    /// Creates a new election.
    /// </summary>
    /// <param name="createDto">The data transfer object containing election creation information.</param>
    /// <returns>An ElectionDto representing the created election.</returns>
    public async Task<ElectionDto> CreateElectionAsync(CreateElectionDto createDto)
    {
        var election = _mapper.Map<Election>(createDto);
        election.ElectionGuid = Guid.NewGuid();
        election.TallyStatus = "Setup";
        election.RowVersion = new byte[8];

        _context.Elections.Add(election);
        await _context.SaveChangesAsync();

        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
        {
            var joinEntry = new JoinElectionUser
            {
                ElectionGuid = election.ElectionGuid,
                UserId = userId,
                Role = "Admin"
            };
            _context.JoinElectionUsers.Add(joinEntry);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Created election {ElectionGuid} - {Name}", election.ElectionGuid, election.Name);

        return await GetElectionByGuidAsync(election.ElectionGuid) ?? _mapper.Map<ElectionDto>(election);
    }

    /// <summary>
    /// Updates an existing election with new information.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to update.</param>
    /// <param name="updateDto">The data transfer object containing updated election information.</param>
    /// <returns>An ElectionDto representing the updated election, or null if the election was not found.</returns>
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

        await _signalRNotificationService.SendElectionUpdateAsync(new ElectionUpdateDto
        {
            ElectionGuid = election.ElectionGuid,
            Name = election.Name,
            TallyStatus = election.TallyStatus,
            ElectionStatus = null,
            UpdatedAt = DateTime.UtcNow
        });

        return await GetElectionByGuidAsync(electionGuid);
    }

    /// <summary>
    /// Deletes an election by its unique identifier.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to delete.</param>
    /// <returns>True if the election was successfully deleted, false if the election was not found.</returns>
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

    /// <summary>
    /// Retrieves a summary of a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>An ElectionDto containing the election summary information, or null if not found.</returns>
    public async Task<ElectionDto?> GetElectionSummaryAsync(Guid electionGuid)
    {
        return await GetElectionByGuidAsync(electionGuid);
    }

    /// <summary>
    /// Updates the public listing status of an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="isListed">Whether the election should be listed for public access.</param>
    /// <returns>True if the listing status was updated successfully, false if the election was not found.</returns>
    public async Task<bool> UpdateElectionListingAsync(Guid electionGuid, bool isListed)
    {
        var election = await _context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return false;
        }

        election.ListForPublic = isListed;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated election {ElectionGuid} listing to {IsListed}", electionGuid, isListed);

        return true;
    }
}
