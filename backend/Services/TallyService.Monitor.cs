using Backend.DTOs.Results;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class TallyService
{
    /// <summary>
    /// Retrieves monitoring information for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A MonitorInfoDto containing monitoring information for the election.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<MonitorInfoDto> GetMonitorInfoAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        var ballotRows = await _context.Ballots
            .AsNoTracking()
            .Where(b => b.Location.ElectionGuid == electionGuid && b.ComputerCode != null)
            .Select(b => new
            {
                b.ComputerCode,
                b.DateUpdated,
                b.DateCreated,
                LocationName = b.Location.Name,
            })
            .ToListAsync();

        var ballotStatsByCode = ballotRows
            .GroupBy(b => b.ComputerCode!)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var latest = g
                        .OrderByDescending(b => b.DateUpdated ?? b.DateCreated ?? DateTimeOffset.MinValue)
                        .First();
                    return (Count: g.Count(), LastKnownLocationName: latest.LocationName ?? UnknownLocationName);
                });

        var assignedLocationsByCode = await _context.Computers
            .AsNoTracking()
            .Where(c => c.ElectionGuid == electionGuid)
            .Select(c => new { c.ComputerCode, LocationName = c.Location.Name })
            .ToDictionaryAsync(c => c.ComputerCode, c => c.LocationName ?? UnknownLocationName);

        var computers = _computerAssignmentService.GetActiveComputers(electionGuid)
            .Select(active =>
            {
                var hasBallotStats = ballotStatsByCode.TryGetValue(active.ComputerCode, out var stats);
                var locationName = hasBallotStats
                    ? stats.LastKnownLocationName
                    : assignedLocationsByCode.GetValueOrDefault(active.ComputerCode, string.Empty);

                return new ComputerInfoDto
                {
                    ComputerCode = active.ComputerCode,
                    LocationName = locationName,
                    BallotCount = hasBallotStats ? stats.Count : 0,
                    LastContact = active.ConnectedAt,
                    Status = DetermineComputerStatus(active.ConnectedAt),
                };
            })
            .ToList();

        // Get location information
        var locations = await _context.Locations
            .Where(l => l.ElectionGuid == electionGuid)
            .Select(l => new LocationInfoDto
            {
                LocationGuid = l.LocationGuid,
                LocationName = l.Name ?? UnknownLocationName,
                BallotCount = l.Ballots.Count(b => b.StatusCode == BallotStatus.Ok),
                VoteCount = l.Ballots.Sum(b => b.Votes.Count),
                VoterCount = _context.People.Count(p => p.ElectionGuid == electionGuid && p.VotingLocationGuid == l.LocationGuid && p.CanVote == true),
                Status = l.Ballots.Any() ? "Active" : "No Ballots"
            })
            .ToListAsync();

        // Get online voting information
        var onlineVotingInfo = new OnlineVotingInfoDto
        {
            OnlineVotingEnabled = election.OnlineWhenOpen.HasValue,
            OnlineVotingStart = election.OnlineWhenOpen,
            OnlineVotingEnd = election.OnlineWhenClose,
            TotalOnlineBallots = await _context.OnlineVotingInfos
                .Where(o => o.ElectionGuid == electionGuid)
                .CountAsync(),
            ProcessedOnlineBallots = await _context.OnlineVotingInfos
                .Where(o => o.ElectionGuid == electionGuid && o.Status == Backend.Helpers.OnlineBallotStatus.Processed)
                .CountAsync(),
            PendingOnlineBallots = await _context.OnlineVotingInfos
                .Where(o => o.ElectionGuid == electionGuid
                            && (o.Status == Backend.Helpers.OnlineBallotStatus.Submitted
                                || o.Status == Backend.Helpers.OnlineBallotStatus.Processing))
                .CountAsync()
        };

        var totalBallots = await _context.Ballots
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .CountAsync();

        var totalVotes = await _context.Votes
            .Where(v => v.Ballot.Location.ElectionGuid == electionGuid)
            .CountAsync();

        return new MonitorInfoDto
        {
            ElectionGuid = electionGuid,
            Computers = computers,
            Locations = locations,
            OnlineVotingInfo = onlineVotingInfo,
            TotalBallots = totalBallots,
            TotalVotes = totalVotes,
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    /// <summary>
    /// Refreshes the contact information for a computer in an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="computerCode">The code identifying the computer.</param>
    public async Task RefreshComputerContactAsync(Guid electionGuid, string computerCode)
    {
        // In the current model, computers don't have a separate entity with last contact tracking
        // This method is kept for API compatibility but doesn't update any database state
        // Computer activity is inferred from ballot creation/update timestamps
        _logger.LogInformation("Computer {ComputerCode} checked in for election {ElectionGuid}", computerCode, electionGuid);

        // Could potentially update a cache or in-memory store here if needed
        // For now, just log the contact
    }

    private string DetermineComputerStatus(DateTimeOffset? lastContact)
    {
        if (!lastContact.HasValue)
            return "Offline";

        var timeSinceContact = DateTimeOffset.UtcNow - lastContact.Value;
        return timeSinceContact.TotalMinutes < 5 ? "Active" : "Inactive";
    }
}
