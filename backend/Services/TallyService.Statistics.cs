using Backend.DTOs.Results;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class TallyService
{
    /// <summary>
    /// Retrieves detailed statistical information about an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A DetailedStatisticsDto containing comprehensive statistical data.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<DetailedStatisticsDto> GetDetailedStatisticsAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        // Get all necessary data
        var results = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid)
            .ToListAsync();

        var summary = await _context.ResultSummaries
            .FirstOrDefaultAsync(rs => rs.ElectionGuid == electionGuid);

        var locations = await _context.Locations
            .Where(l => l.ElectionGuid == electionGuid)
            .Include(l => l.Ballots)
            .ThenInclude(b => b.Votes)
            .ToListAsync();

        var allBallots = await _context.Ballots
            .Include(b => b.Votes)
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .ToListAsync();

        var totalRegisteredVoters = await _context.People
            .CountAsync(p => p.ElectionGuid == electionGuid && p.CanVote == true);
        var totalBallotsCast = allBallots.Count;
        var validBallots = allBallots.Count(b => b.StatusCode == BallotStatus.Ok);
        var spoiledBallots = summary?.SpoiledBallots ?? 0;
        var totalVotes = summary?.TotalVotes ?? 0;

        // Calculate election overview
        var overview = CalculateElectionOverview(election, totalRegisteredVoters, totalBallotsCast, validBallots, spoiledBallots, totalVotes);

        // Calculate vote distribution
        var voteDistribution = CalculateVoteDistribution(election, allBallots);

        // Calculate person performance
        var personPerformance = CalculatePersonPerformance(results, totalVotes);

        // Calculate turnout analysis
        var turnoutAnalysis = await CalculateTurnoutAnalysisAsync(electionGuid, locations, totalRegisteredVoters, totalBallotsCast, election);

        // Calculate location statistics
        var locationStatistics = await CalculateLocationStatisticsAsync(electionGuid, locations);

        return new DetailedStatisticsDto
        {
            Overview = overview,
            VoteDistribution = voteDistribution,
            PersonPerformance = personPerformance,
            TurnoutAnalysis = turnoutAnalysis,
            LocationStatistics = locationStatistics
        };
    }
}
