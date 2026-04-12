using System.Security.Claims;
using Backend.Domain.Context;
using Backend.DTOs.Dashboard;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service for managing dashboard operations including election summaries, statistics, and teller management.
/// Provides functionality to retrieve election data and manage election-related settings.
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly MainDbContext _context;
    private readonly ILogger<DashboardService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the DashboardService.
    /// </summary>
    /// <param name="context">The main database context for accessing election and dashboard data.</param>
    /// <param name="logger">Logger for recording dashboard service operations.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for retrieving current user information.</param>
    public DashboardService(MainDbContext context, ILogger<DashboardService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
        {
            return userId;
        }
        return null;
    }

    /// <summary>
    /// Retrieves a summary of election statistics for the dashboard.
    /// </summary>
    /// <returns>A DashboardSummaryDto containing counts of active and completed elections, plus recent elections.</returns>
    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var userId = GetCurrentUserId();
        var baseQuery = _context.Elections.AsQueryable();

        if (userId.HasValue)
        {
            baseQuery = baseQuery.Where(e =>
                _context.JoinElectionUsers.Any(jeu => jeu.ElectionGuid == e.ElectionGuid && jeu.UserId == userId.Value));
        }

        var activeCount = await baseQuery
            .Where(e => e.TallyStatus != "Finalized" && e.TallyStatus != "Archived")
            .CountAsync();

        var completedCount = await baseQuery
            .Where(e => e.TallyStatus == "Finalized")
            .CountAsync();

        var recentElections = await GetRecentElectionsAsync(5);

        _logger.LogInformation("Dashboard summary: {ActiveCount} active, {CompletedCount} completed elections",
            activeCount, completedCount);

        return new DashboardSummaryDto
        {
            ActiveElectionCount = activeCount,
            CompletedElectionCount = completedCount,
            RecentElections = recentElections
        };
    }

    /// <summary>
    /// Retrieves a list of recent elections with their statistics.
    /// </summary>
    /// <param name="limit">The maximum number of elections to retrieve. Default is 10.</param>
    /// <returns>A list of ElectionCardDto objects containing election information and statistics.</returns>
    public async Task<List<ElectionCardDto>> GetRecentElectionsAsync(int limit = 10)
    {
        var userId = GetCurrentUserId();
        var baseQuery = _context.Elections.AsQueryable();

        if (userId.HasValue)
        {
            baseQuery = baseQuery.Where(e =>
                _context.JoinElectionUsers.Any(jeu => jeu.ElectionGuid == e.ElectionGuid && jeu.UserId == userId.Value));
        }

        var elections = await baseQuery
            .Include(e => e.People)
            .Include(e => e.Locations)
                .ThenInclude(l => l.Ballots)
                    .ThenInclude(b => b.Votes)
            .OrderByDescending(e => e.DateOfElection ?? DateTimeOffset.MinValue)
            .Take(limit)
            .ToListAsync();

        var electionCards = elections.Select(e =>
        {
            var voterCount = e.People.Count(p => p.CanVote == true);
            var ballots = e.Locations.SelectMany(l => l.Ballots).ToList();
            var ballotCount = ballots.Count;
            var voteCount = ballots.SelectMany(b => b.Votes).Count();

            double percentComplete = 0;
            if (voterCount > 0)
            {
                percentComplete = Math.Round((double)ballotCount / voterCount * 100, 2);
                if (percentComplete > 100) percentComplete = 100;
            }

            return new ElectionCardDto
            {
                ElectionGuid = e.ElectionGuid,
                Name = e.Name,
                DateOfElection = e.DateOfElection,
                TallyStatus = e.TallyStatus ?? "Unknown",
                VoterCount = voterCount,
                BallotCount = ballotCount,
                VoteCount = voteCount,
                PercentComplete = percentComplete
            };
        }).ToList();

        _logger.LogInformation("Retrieved {Count} recent elections", electionCards.Count);

        return electionCards;
    }

    /// <summary>
    /// Retrieves all elections that the current user has access to.
    /// Filters elections based on user permissions via JoinElectionUser.
    /// </summary>
    /// <returns>A list of ElectionCardDto objects for all accessible elections.</returns>
    public async Task<List<ElectionCardDto>> GetAllAccessibleElectionsAsync()
    {
        return await GetRecentElectionsAsync(100);
    }

    /// <summary>
    /// Retrieves static information about a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>An anonymous object containing static election information including configuration and counts.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<object> GetElectionStaticInfoAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .Include(e => e.People)
            .Include(e => e.Locations)
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        return new
        {
            electionGuid = election.ElectionGuid,
            name = election.Name,
            electionType = election.ElectionType,
            electionMode = election.ElectionMode,
            numberToElect = election.NumberToElect,
            dateOfElection = election.DateOfElection,
            tallyStatus = election.TallyStatus,
            ownerLoginId = election.OwnerLoginId,
            listForPublic = election.ListForPublic,
            showAsTest = election.ShowAsTest,
            voterCount = election.People.Count(p => p.CanVote == true),
            locationCount = election.Locations.Count
        };
    }

    /// <summary>
    /// Retrieves live statistics for a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>An anonymous object containing live election statistics including ballot and vote counts.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<object> GetElectionLiveStatsAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .Include(e => e.Locations)
                .ThenInclude(l => l.Ballots)
                    .ThenInclude(b => b.Votes)
            .Include(e => e.People)
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        var ballots = election.Locations.SelectMany(l => l.Ballots).ToList();
        var votes = ballots.SelectMany(b => b.Votes).ToList();

        var lastLogEntry = await _context.Logs
            .Where(l => l.ElectionGuid == electionGuid)
            .OrderByDescending(l => l.AsOf)
            .Select(l => l.AsOf)
            .FirstOrDefaultAsync();

        return new
        {
            electionGuid = election.ElectionGuid,
            ballotCount = ballots.Count,
            voteCount = votes.Count,
            voterCount = election.People.Count(p => p.CanVote == true),
            locationCount = election.Locations.Count,
            lastActivity = lastLogEntry != default ? (DateTimeOffset?)lastLogEntry : null,
            onlineVotingActive = election.OnlineWhenOpen.HasValue,
            passcode = election.ElectionPasscode
        };
    }

    /// <summary>
    /// Assigns a computer to a specific location for election management.
    /// </summary>
    /// <param name="computerCode">The code identifying the computer.</param>
    /// <param name="locationGuid">The unique identifier of the location to assign the computer to.</param>
    /// <returns>True if the assignment was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when the location is not found.</exception>
    public async Task<bool> SetComputerLocationAsync(string computerCode, Guid locationGuid)
    {
        var location = await _context.Locations
            .FirstOrDefaultAsync(l => l.LocationGuid == locationGuid);

        if (location == null)
        {
            throw new ArgumentException($"Location {locationGuid} not found");
        }

        var computer = await _context.Computers
            .FirstOrDefaultAsync(c => c.ComputerCode == computerCode && c.ElectionGuid == location.ElectionGuid);

        if (computer != null)
        {
            computer.LocationGuid = locationGuid;
            computer.LastActivity = DateTimeOffset.UtcNow;
        }
        else
        {
            _context.Computers.Add(new Backend.Domain.Entities.Computer
            {
                ElectionGuid = location.ElectionGuid,
                LocationGuid = locationGuid,
                ComputerGuid = Guid.NewGuid(),
                ComputerCode = computerCode
            });
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Computer {ComputerCode} assigned to location {LocationGuid}",
            computerCode, locationGuid);

        return true;
    }

    /// <summary>
    /// Assigns a guest teller to an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="tellerName">The name of the guest teller to assign.</param>
    /// <returns>True if the assignment was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<bool> AssignGuestTellerAsync(Guid electionGuid, string tellerName)
    {
        var electionExists = await _context.Elections
            .AnyAsync(e => e.ElectionGuid == electionGuid);

        if (!electionExists)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        var existingTeller = await _context.Tellers
            .FirstOrDefaultAsync(t => t.ElectionGuid == electionGuid && t.Name == tellerName);

        if (existingTeller == null)
        {
            _context.Tellers.Add(new Backend.Domain.Entities.Teller
            {
                ElectionGuid = electionGuid,
                Name = tellerName,
                RowVersion = new byte[8]
            });
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Guest teller {TellerName} assigned to election {ElectionGuid}",
            tellerName, electionGuid);

        return true;
    }

    /// <summary>
    /// Removes a guest teller from an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="tellerName">The name of the guest teller to remove.</param>
    /// <returns>True if the removal was successful.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<bool> RemoveGuestTellerAsync(Guid electionGuid, string tellerName)
    {
        var teller = await _context.Tellers
            .FirstOrDefaultAsync(t => t.ElectionGuid == electionGuid && t.Name == tellerName);

        if (teller == null)
        {
            return false;
        }

        _context.Tellers.Remove(teller);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Guest teller {TellerName} removed from election {ElectionGuid}",
            tellerName, electionGuid);

        return true;
    }
}



