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
}
