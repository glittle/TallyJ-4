using Backend.DTOs.OnlineVoting;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class OnlineVotingService
{
    /// <inheritdoc/>
    public async Task<OnlineElectionInfoDto?> GetElectionInfoAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .Where(e => e.ElectionGuid == electionGuid)
            .FirstOrDefaultAsync();

        if (election == null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var isOpen = (election.OnlineWhenOpen == null || election.OnlineWhenOpen <= now) &&
                     (election.OnlineWhenClose == null || election.OnlineWhenClose > now);

        return new OnlineElectionInfoDto
        {
            ElectionGuid = election.ElectionGuid,
            Name = election.Name,
            Convenor = election.Convenor,
            DateOfElection = election.DateOfElection,
            NumberToElect = election.NumberToElect,
            OnlineWhenOpen = election.OnlineWhenOpen,
            OnlineWhenClose = election.OnlineWhenClose,
            IsOpen = isOpen,
            Instructions = $"voting.election.instructions:{election.NumberToElect ?? 9}",
            OnlineSelectionProcess = election.OnlineSelectionProcess
        };
    }

    /// <inheritdoc/>
    public async Task<List<OnlinePersonDto>> GetPeopleAsync(Guid electionGuid)
    {
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanReceiveVotes == true)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return people.Select(p => new OnlinePersonDto
        {
            PersonGuid = p.PersonGuid,
            FullName = p.FullName ?? "",
            Area = p.Area,
            OtherInfo = p.OtherInfo
        }).ToList();
    }

    /// <inheritdoc/>
    public async Task<List<AvailableElectionDto>> GetAvailableElectionsAsync(string voterId)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;

            // Find all elections where this voter is registered (by email, phone, or kiosk code)
            var personElections = await _context.People
                .Where(p => p.Email == voterId || p.Phone == voterId || p.KioskCode == voterId)
                .Join(_context.Elections,
                    person => person.ElectionGuid,
                    election => election.ElectionGuid,
                    (person, election) => new { Person = person, Election = election })
                .GroupBy(x => x.Election.ElectionGuid)
                .Select(g => g.First())
                .ToListAsync();

            // Look up OnlineVotingInfo for each person
            var personGuids = personElections.Select(x => x.Person.PersonGuid).ToList();
            var electionGuids = personElections.Select(x => x.Election.ElectionGuid).ToList();

            var votingInfos = await _context.OnlineVotingInfos
                .Where(ovi => personGuids.Contains(ovi.PersonGuid) && electionGuids.Contains(ovi.ElectionGuid))
                .ToListAsync();

            var result = personElections.Select(x =>
            {
                var hasOnlineVoting = x.Election.UseOnlineVoting;
                var isOpen = hasOnlineVoting &&
                             (x.Election.OnlineWhenOpen == null || x.Election.OnlineWhenOpen <= now) &&
                             (x.Election.OnlineWhenClose == null || x.Election.OnlineWhenClose > now);

                var votingInfo = votingInfos
                    .Where(ovi => ovi.ElectionGuid == x.Election.ElectionGuid && ovi.PersonGuid == x.Person.PersonGuid)
                    .OrderByDescending(ovi => ovi.WhenBallotCreated)
                    .FirstOrDefault();

                return new AvailableElectionDto
                {
                    ElectionGuid = x.Election.ElectionGuid,
                    Name = x.Election.Name,
                    Convenor = x.Election.Convenor,
                    OnlineWhenOpen = x.Election.OnlineWhenOpen,
                    OnlineWhenClose = x.Election.OnlineWhenClose,
                    OnlineCloseIsEstimate = x.Election.OnlineCloseIsEstimate,
                    DateOfElection = x.Election.DateOfElection,
                    IsOpen = isOpen,
                    HasOnlineVoting = hasOnlineVoting,
                    HasVoted = x.Person.HasOnlineBallot == true,
                    VoterName = x.Person.FullName,
                    BallotStatus = votingInfo?.Status,
                    WhenBallotStatus = votingInfo?.WhenStatus,
                    CanChangeVote = votingInfo == null || !CannotChangeOnlineVote(votingInfo)
                };
            })
            .OrderBy(e => !e.IsOpen)
            .ThenBy(e => e.Name)
            .ToList();

            result = result.Where(e => e.IsOpen).ToList();

            _logger.LogInformation("Found {Count} available elections", result.Count);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available elections");
            return new List<AvailableElectionDto>();
        }
    }
}
