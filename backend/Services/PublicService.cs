using Microsoft.EntityFrameworkCore;
using TallyJ4.DTOs.Public;
using TallyJ4.Domain.Context;

namespace TallyJ4.Services;

/// <summary>
/// Service for managing public-facing operations including election discovery and status information.
/// Provides functionality for public users to access election information without authentication.
/// </summary>
public class PublicService : IPublicService
{
    private readonly MainDbContext _context;
    private readonly ILogger<PublicService> _logger;

    /// <summary>
    /// Initializes a new instance of the PublicService.
    /// </summary>
    /// <param name="context">The main database context for accessing election data.</param>
    /// <param name="logger">Logger for recording public service operations.</param>
    public PublicService(MainDbContext context, ILogger<PublicService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves general information for the public home page.
    /// </summary>
    /// <returns>A PublicHomeDto containing application information and available elections count.</returns>
    public async Task<PublicHomeDto> GetPublicHomeDataAsync()
    {
        var availableElectionsCount = await _context.Elections
            .Where(e => !string.IsNullOrEmpty(e.ElectionPasscode))
            .CountAsync();

        _logger.LogInformation("Public home data requested. Available elections: {Count}", availableElectionsCount);

        return new PublicHomeDto
        {
            ApplicationName = "TallyJ 4",
            Version = "4.0.0",
            Description = "Election management and online voting system",
            AvailableElectionsCount = availableElectionsCount,
            ServerTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Retrieves a list of elections that are available for public access.
    /// </summary>
    /// <returns>A list of AvailableElectionDto objects representing elections with passcodes.</returns>
    public async Task<List<AvailableElectionDto>> GetAvailableElectionsAsync()
    {
        var elections = await _context.Elections
            .Where(e => !string.IsNullOrEmpty(e.ElectionPasscode))
            .OrderByDescending(e => e.DateOfElection ?? DateTime.MinValue)
            .Select(e => new AvailableElectionDto
            {
                ElectionGuid = e.ElectionGuid,
                Name = e.Name,
                DateOfElection = e.DateOfElection,
                ElectionType = e.ElectionType ?? "Unknown"
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} available elections", elections.Count);

        return elections;
    }

    /// <summary>
    /// Retrieves the current status of a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>An ElectionStatusDto containing election status information, or null if the election is not found.</returns>
    public async Task<ElectionStatusDto?> GetElectionStatusAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .Include(e => e.People)
            .Include(e => e.Locations)
                .ThenInclude(l => l.Ballots)
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            _logger.LogWarning("Election {ElectionGuid} not found", electionGuid);
            return null;
        }

        var voterCount = election.People.Count(p => p.CanVote == true);
        var ballots = election.Locations.SelectMany(l => l.Ballots).ToList();
        var ballotCount = ballots.Count;

        var isActive = election.TallyStatus != "Complete" && 
                      election.TallyStatus != "Archived" &&
                      election.TallyStatus != "Deleted";

        _logger.LogInformation("Election status for {ElectionGuid}: {Status}", electionGuid, election.TallyStatus);

        return new ElectionStatusDto
        {
            ElectionGuid = election.ElectionGuid,
            Name = election.Name,
            DateOfElection = election.DateOfElection,
            ElectionType = election.ElectionType ?? "Unknown",
            TallyStatus = election.TallyStatus ?? "Unknown",
            IsActive = isActive,
            RegisteredVoters = voterCount,
            BallotsSubmitted = ballotCount
        };
    }
}
