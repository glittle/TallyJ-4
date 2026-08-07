using Backend.Context;
using Backend.Enumerations;
using Backend.DTOs.Public;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service for anonymous public operations: guest-teller election discovery and system info.
/// </summary>
public class PublicService : IPublicService
{
    private readonly MainDbContext _context;
    private readonly IComputerAssignmentService _assignmentService;
    private readonly ILogger<PublicService> _logger;

    /// <summary>
    /// Initializes a new instance of the PublicService.
    /// </summary>
    /// <param name="context">The main database context for accessing election data.</param>
    /// <param name="assignmentService">Tracks active main teller connections for guest login eligibility.</param>
    /// <param name="logger">Logger for recording public service operations.</param>
    public PublicService(
        MainDbContext context,
        IComputerAssignmentService assignmentService,
        ILogger<PublicService> logger)
    {
        _context = context;
        _assignmentService = assignmentService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves general information for the public home page.
    /// </summary>
    /// <returns>A PublicHomeDto containing application information and available elections count.</returns>
    public async Task<PublicHomeDto> GetPublicHomeDataAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var availableElectionsCount = await _context.Elections
            .Where(e => e.ListedForPublicAsOf != null && e.ListedForPublicAsOf <= now)
            .CountAsync();

        _logger.LogInformation("Public home data requested. Available elections: {Count}", availableElectionsCount);

        return new PublicHomeDto
        {
            ApplicationName = "TallyJ 4",
            Version = "4.0.0",
            Description = "Election management and online voting system",
            AvailableElectionsCount = availableElectionsCount,
            ServerTime = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Retrieves a list of elections that are available for public access.
    /// </summary>
    /// <returns>A list of AvailableElectionDto objects representing elections with passcodes.</returns>
    public async Task<List<AvailableElectionDto>> GetAvailableElectionsAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var listedElections = await _context.Elections
            .Where(e => e.ListedForPublicAsOf != null && e.ListedForPublicAsOf <= now)
            .OrderByDescending(e => e.DateOfElection ?? DateTimeOffset.MinValue)
            .Select(e => new
            {
                e.ElectionGuid,
                e.Name,
                e.DateOfElection,
                e.ElectionType
            })
            .ToListAsync();

        var elections = listedElections
            .Where(e => _assignmentService.HasActiveMainTeller(e.ElectionGuid))
            .Select(e => new AvailableElectionDto
            {
                ElectionGuid = e.ElectionGuid,
                Name = e.Name,
                DateOfElection = e.DateOfElection,
                ElectionType = ElectionTypeEnum.ParseCode(e.ElectionType)
            })
            .ToList();

        _logger.LogInformation(
            "Retrieved {Count} guest-joinable elections ({ListedCount} listed, filtered by active main teller)",
            elections.Count,
            listedElections.Count);

        return elections;
    }

    /// <summary>
    /// Retrieves the current status of a specific election (for authorized joined tellers).
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

        var isActive = election.ElectionStage != ElectionStage.ProcessingBallots;

        _logger.LogInformation("Election status for {ElectionGuid}: {Stage}", electionGuid, election.ElectionStage);

        return new ElectionStatusDto
        {
            ElectionGuid = election.ElectionGuid,
            Name = election.Name,
            DateOfElection = election.DateOfElection,
            ElectionType = ElectionTypeEnum.ParseCode(election.ElectionType),
            ElectionStage = election.ElectionStage,
            IsActive = isActive,
            RegisteredVoters = voterCount,
            BallotsSubmitted = ballotCount
        };
    }
}

