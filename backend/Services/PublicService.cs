using Microsoft.EntityFrameworkCore;
using Backend.DTOs.Public;
using Backend.Domain.Context;
using Backend.Domain.Enumerations;

namespace Backend.Services;

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
            .Where(e => e.ListedForPublicAsOf != null)
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
        var elections = (await _context.Elections
            .Where(e => e.ListedForPublicAsOf != null)
            .OrderByDescending(e => e.DateOfElection ?? DateTime.MinValue)
            .Select(e => new
            {
                e.ElectionGuid,
                e.Name,
                e.DateOfElection,
                e.ElectionType
            })
            .ToListAsync())
            .Select(e => new AvailableElectionDto
            {
                ElectionGuid = e.ElectionGuid,
                Name = e.Name,
                DateOfElection = e.DateOfElection,
                ElectionType = ElectionTypeEnum.ParseCode(e.ElectionType)
            })
            .ToList();

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
            ElectionType = ElectionTypeEnum.ParseCode(election.ElectionType),
            TallyStatus = election.TallyStatus ?? "Unknown",
            IsActive = isActive,
            RegisteredVoters = voterCount,
            BallotsSubmitted = ballotCount
        };
    }

    /// <summary>
    /// Retrieves election results formatted for public display.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The public display data with results, or null if the election is not found or not public.</returns>
    public async Task<PublicDisplayDto?> GetPublicDisplayDataAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            _logger.LogWarning("Election {ElectionGuid} not found for public display", electionGuid);
            return null;
        }

        if (election.ListForPublic != true)
        {
            _logger.LogWarning("Election {ElectionGuid} is not listed for public display", electionGuid);
            return null;
        }

        var results = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid)
            .OrderBy(r => r.Rank)
            .ToListAsync();

        var resultSummary = await _context.ResultSummaries
            .FirstOrDefaultAsync(rs => rs.ElectionGuid == electionGuid);

        var numberToElect = election.NumberToElect ?? 0;
        var numberExtra = election.NumberExtra ?? 0;

        var electedCandidates = results
            .Where(r => r.Section == "E" && r.Rank > 0 && r.Rank <= numberToElect)
            .Select(r => new PublicCandidateDto
            {
                Rank = r.Rank,
                FullName = r.Person != null
                    ? $"{r.Person.LastName ?? string.Empty}, {r.Person.FirstName ?? string.Empty}".Trim().Trim(',')
                    : "Unknown",
                VoteCount = r.VoteCount ?? 0,
                IsTied = r.IsTied ?? false,
                TieBreakRequired = r.TieBreakRequired ?? false
            })
            .OrderBy(c => c.Rank)
            .ToList();

        var additionalCandidates = results
            .Where(r => (r.Section == "X" || (r.Section == "E" && r.Rank > numberToElect))
                        && r.Rank > 0 && r.Rank <= numberToElect + numberExtra)
            .Select(r => new PublicCandidateDto
            {
                Rank = r.Rank,
                FullName = r.Person != null
                    ? $"{r.Person.LastName ?? string.Empty}, {r.Person.FirstName ?? string.Empty}".Trim().Trim(',')
                    : "Unknown",
                VoteCount = r.VoteCount ?? 0,
                IsTied = r.IsTied ?? false,
                TieBreakRequired = r.TieBreakRequired ?? false
            })
            .OrderBy(c => c.Rank)
            .ToList();

        var totalBallots = resultSummary?.BallotsReceived ?? 0;
        var spoiledBallots = resultSummary?.SpoiledBallots ?? 0;
        var validBallots = totalBallots - spoiledBallots;
        var totalVotes = resultSummary?.TotalVotes ?? 0;
        var registeredVoters = resultSummary?.NumEligibleToVote ?? 0;
        var turnoutPercentage = registeredVoters > 0 ? (decimal)totalBallots / registeredVoters * 100 : 0;

        var isFinalized = election.TallyStatus == "Finalized" ||
                         election.TallyStatus == "Complete" ||
                         election.TallyStatus == "Archived";

        _logger.LogInformation("Retrieved public display data for election {ElectionGuid}", electionGuid);

        return new PublicDisplayDto
        {
            ElectionGuid = election.ElectionGuid,
            ElectionName = election.Name ?? "Unknown Election",
            DateOfElection = election.DateOfElection,
            Convenor = election.Convenor ?? string.Empty,
            ElectionType = ElectionTypeEnum.ParseCode(election.ElectionType),
            TallyStatus = election.TallyStatus ?? "Unknown",
            NumberToElect = numberToElect,
            NumberExtra = numberExtra,
            ElectedCandidates = electedCandidates,
            AdditionalCandidates = additionalCandidates,
            Statistics = new PublicDisplayStatsDto
            {
                TotalBallots = totalBallots,
                ValidBallots = validBallots,
                SpoiledBallots = spoiledBallots,
                TotalVotes = totalVotes,
                RegisteredVoters = registeredVoters,
                TurnoutPercentage = Math.Round(turnoutPercentage, 2)
            },
            LastUpdated = DateTime.UtcNow,
            IsFinalized = isFinalized
        };
    }
}



