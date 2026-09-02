using Backend;
using Backend.DTOs.Results;
using Backend.Entities;
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

        var onlineLists = await LoadOnlineBallotListsAsync(electionGuid);

        // Get online voting information
        var onlineVotingInfo = new OnlineVotingInfoDto
        {
            OnlineVotingEnabled = election.OnlineWhenOpen.HasValue,
            OnlineVotingStart = election.OnlineWhenOpen,
            OnlineVotingEnd = election.OnlineWhenClose,
            TotalOnlineBallots = onlineLists.Total,
            ProcessedOnlineBallots = onlineLists.Accepted.Count,
            PendingOnlineBallots = onlineLists.Pending.Count,
            AcceptAllRuns = await LoadAcceptAllRunsAsync(electionGuid),
            PendingBallots = onlineLists.Pending,
            AcceptedBallots = onlineLists.Accepted
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

    /// <summary>
    /// Pending and accepted lists share the stored <c>OnlineVotingInfo.Status</c>
    /// with the monitor counts. Name columns only — no email, phone, kiosk, or
    /// vote payload, and no link to the regular ballot created on accept.
    /// </summary>
    private async Task<(
        int Total,
        List<OnlineBallotMonitorItemDto> Pending,
        List<OnlineBallotMonitorItemDto> Accepted)> LoadOnlineBallotListsAsync(Guid electionGuid)
    {
        var votingRows = await _context.OnlineVotingInfos
            .AsNoTracking()
            .Where(o => o.ElectionGuid == electionGuid)
            .Select(o => new { o.RowId, o.PersonGuid, o.Status, o.WhenStatus })
            .ToListAsync();

        var personGuids = votingRows.Select(v => v.PersonGuid).Distinct().ToList();
        var people = await _context.People
            .AsNoTracking()
            .Where(p => p.ElectionGuid == electionGuid && personGuids.Contains(p.PersonGuid))
            .Select(p => new
            {
                p.PersonGuid,
                p.LastName,
                p.FirstName,
                p.OtherLastNames,
                p.OtherNames,
                p.OtherInfo
            })
            .ToListAsync();
        var peopleByGuid = people.ToDictionary(p => p.PersonGuid);

        var items = votingRows
            .Select(r =>
            {
                peopleByGuid.TryGetValue(r.PersonGuid, out var person);
                var personName = person == null
                    ? string.Empty
                    : Backend.Helpers.PersonNameHelper.ComputeFullName(new Person
                    {
                        LastName = person.LastName,
                        FirstName = person.FirstName,
                        OtherLastNames = person.OtherLastNames,
                        OtherNames = person.OtherNames,
                        OtherInfo = person.OtherInfo
                    }) ?? string.Empty;
                return new OnlineBallotMonitorItemDto
                {
                    RowId = r.RowId,
                    PersonName = personName,
                    Status = r.Status,
                    WhenStatus = r.WhenStatus
                };
            })
            .OrderByDescending(i => i.WhenStatus)
            .ThenBy(i => i.PersonName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return (
            items.Count,
            items.Where(i =>
                    Backend.Helpers.OnlineBallotStatus.IsSubmitted(i.Status)
                    || Backend.Helpers.OnlineBallotStatus.IsProcessing(i.Status))
                .ToList(),
            items.Where(i => Backend.Helpers.OnlineBallotStatus.IsProcessed(i.Status)).ToList());
    }

    private async Task<List<AcceptAllOnlineBallotsRunDto>> LoadAcceptAllRunsAsync(Guid electionGuid)
    {
        var logs = await _context.SecurityAuditLogs
            .AsNoTracking()
            .Where(l => l.ElectionGuid == electionGuid
                        && l.EventType == SecurityEventType.OperationalActivity
                        && l.Details != null
                        && l.Details.StartsWith(Backend.Helpers.AcceptAllOnlineBallotsAudit.DetailsPrefix))
            .OrderByDescending(l => l.Timestamp)
            .ToListAsync();

        return logs.Select(Backend.Helpers.AcceptAllOnlineBallotsAudit.ToRunDto).ToList();
    }

    private string DetermineComputerStatus(DateTimeOffset? lastContact)
    {
        if (!lastContact.HasValue)
            return "Offline";

        var timeSinceContact = DateTimeOffset.UtcNow - lastContact.Value;
        return timeSinceContact.TotalMinutes < 5 ? "Active" : "Inactive";
    }
}
