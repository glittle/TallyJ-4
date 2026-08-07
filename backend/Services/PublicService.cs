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
    /// Retrieves elections currently open for guest-teller join
    /// (listed for public discovery and with an active main teller).
    /// </summary>
    /// <returns>A list of guest-joinable election summaries.</returns>
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
}

