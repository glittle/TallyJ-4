using Microsoft.EntityFrameworkCore;
using TallyJ4.DTOs.Dashboard;
using TallyJ4.Domain.Context;

namespace TallyJ4.Services;

public class DashboardService : IDashboardService
{
    private readonly MainDbContext _context;
    private readonly ILogger<DashboardService> _logger;

    public DashboardService(MainDbContext context, ILogger<DashboardService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync()
    {
        var activeCount = await _context.Elections
            .Where(e => e.TallyStatus != "Complete" && e.TallyStatus != "Archived")
            .CountAsync();

        var completedCount = await _context.Elections
            .Where(e => e.TallyStatus == "Complete")
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

    public async Task<List<ElectionCardDto>> GetRecentElectionsAsync(int limit = 10)
    {
        var elections = await _context.Elections
            .Include(e => e.People)
            .Include(e => e.Locations)
                .ThenInclude(l => l.Ballots)
                    .ThenInclude(b => b.Votes)
            .OrderByDescending(e => e.DateOfElection ?? DateTime.MinValue)
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

    public async Task<List<ElectionCardDto>> GetAllAccessibleElectionsAsync()
    {
        // For now, return all elections. In a real implementation, this would filter
        // based on user permissions (admin vs guest teller vs voter)
        return await GetRecentElectionsAsync(100); // Get more elections
    }

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

        return new
        {
            electionGuid = election.ElectionGuid,
            ballotCount = ballots.Count,
            voteCount = votes.Count,
            voterCount = election.People.Count(p => p.CanVote == true),
            locationCount = election.Locations.Count,
            lastActivity = (DateTime?)null, // TODO: Add timestamp tracking if needed
            onlineVotingActive = election.OnlineWhenOpen.HasValue,
            passcode = election.ElectionPasscode
        };
    }

    public async Task<bool> SetComputerLocationAsync(string computerCode, Guid locationGuid)
    {
        // This would typically store computer-to-location mapping
        // For now, just validate that the location exists
        var locationExists = await _context.Locations
            .AnyAsync(l => l.LocationGuid == locationGuid);

        if (!locationExists)
        {
            throw new ArgumentException($"Location {locationGuid} not found");
        }

        _logger.LogInformation("Computer {ComputerCode} assigned to location {LocationGuid}",
            computerCode, locationGuid);

        // In a real implementation, this would persist the mapping
        return true;
    }

    public async Task<bool> AssignGuestTellerAsync(Guid electionGuid, string tellerName)
    {
        // Validate election exists
        var electionExists = await _context.Elections
            .AnyAsync(e => e.ElectionGuid == electionGuid);

        if (!electionExists)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        _logger.LogInformation("Guest teller {TellerName} assigned to election {ElectionGuid}",
            tellerName, electionGuid);

        // In a real implementation, this would create/update a teller record
        return true;
    }

    public async Task<bool> RemoveGuestTellerAsync(Guid electionGuid, string tellerName)
    {
        // Validate election exists
        var electionExists = await _context.Elections
            .AnyAsync(e => e.ElectionGuid == electionGuid);

        if (!electionExists)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        _logger.LogInformation("Guest teller {TellerName} removed from election {ElectionGuid}",
            tellerName, electionGuid);

        // In a real implementation, this would remove the teller record
        return true;
    }
}
