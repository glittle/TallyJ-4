using Backend.DTOs.Reports;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;

using LocationTypeEnum = Backend.Enumerations.LocationType;

namespace Backend.Services;

public partial class ReportService
{
    public async Task<BallotsReportDto> GetBallotsReportAsync(Guid electionGuid, string? filter = null)
    {
        var election = await GetElectionAsync(electionGuid);
        var isSingleName = IsSingleNameElection(election);
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();
        var hasMultipleLocations = locations.Count > 1;
        var tiedPersonGuids = new HashSet<Guid>();

        var ballotsQuery = _context.Ballots
            .Include(b => b.Location)
            .Include(b => b.Votes).ThenInclude(v => v.Person)
            .Where(b => b.Location.ElectionGuid == electionGuid);

        if (filter == "Online")
        {
            ballotsQuery = ballotsQuery.Where(b => b.Location.LocationTypeCode == nameof(LocationTypeEnum.Online));
        }
        else if (filter == "Imported")
        {
            ballotsQuery = ballotsQuery.Where(b => b.Location.LocationTypeCode == nameof(LocationTypeEnum.Imported));
        }
        else if (filter == "Tied")
        {
            var tiedResults = await _context.Results
                .Where(r => r.ElectionGuid == electionGuid && r.TieBreakRequired == true)
                .Select(r => r.PersonGuid)
                .ToListAsync();
            tiedPersonGuids = tiedResults.ToHashSet();
        }

        var ballots = await ballotsQuery
            .OrderBy(b => b.Location.Name)
            .ThenBy(b => b.ComputerCode)
            .ThenBy(b => b.BallotNumAtComputer)
            .ToListAsync();

        var ballotItems = ballots.Select(b =>
        {
            var locName = hasMultipleLocations ? b.Location.Name : "";
            var votes = b.Votes
                .OrderBy(v => isSingleName ? (v.Person?.FullNameFl ?? "") : v.PositionOnBallot.ToString("0000"))
                .Select(v => new BallotVoteDto
                {
                    PersonName = v.Person?.FullNameFl
                                 ?? Backend.Models.OnlineRawVote.Parse(v.OnlineVoteRaw).ToDisplayName(),
                    SingleNameElectionCount = v.SingleNameElectionCount,
                    OnlineVoteRaw = v.OnlineVoteRaw,
                    Spoiled = v.VoteStatus == VoteStatus.Spoiled,
                    TieBreakRequired = v.PersonGuid.HasValue && tiedPersonGuids.Contains(v.PersonGuid.Value),
                    InvalidReasonDesc = GetIneligibleDescription(v.IneligibleReasonCode)
                        ?? (v.Person != null && v.Person.CanReceiveVotes != true
                            ? GetIneligibleDescriptionByGuid(v.Person.IneligibleReasonGuid)
                            : null)
                }).ToList();

            return new BallotReportItemDto
            {
                BallotCode = b.BallotCode ?? "",
                Location = locName,
                IsOnline = b.Location.LocationTypeCode == nameof(LocationTypeEnum.Online),
                IsImported = b.Location.LocationTypeCode == nameof(LocationTypeEnum.Imported),
                BallotId = b.RowId,
                LocationId = b.Location.RowId,
                StatusCode = b.StatusCode.ToString(),
                Spoiled = b.StatusCode != BallotStatus.Ok,
                Votes = votes
            };
        }).ToList();

        if (filter == "Tied")
        {
            ballotItems = ballotItems
                .Where(b => b.Votes.Any(v => v.TieBreakRequired))
                .ToList();
        }

        return new BallotsReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            IsSingleNameElection = isSingleName,
            Ballots = ballotItems
        };
    }

    public async Task<SpoiledVotesReportDto> GetSpoiledVotesAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);

        var votes = await _context.Votes
            .Include(v => v.Person)
            .Where(v =>
                v.Ballot != null &&
                v.Ballot.Location != null &&
                v.Ballot.Location.ElectionGuid == electionGuid &&
                v.Ballot.StatusCode == BallotStatus.Ok)
            .Where(v => v.IneligibleReasonCode != null ||
                        (v.Person != null && v.Person.IneligibleReasonGuid != null))
            .ToListAsync();

        var grouped = votes
            .GroupBy(v => v.Person?.FullName ?? "Unknown")
            .Select(g =>
            {
                var firstVote = g.First();
                var desc = GetIneligibleDescription(firstVote.IneligibleReasonCode)
                           ?? GetIneligibleDescriptionByGuid(firstVote.Person?.IneligibleReasonGuid);
                return new SpoiledVoteItemDto
                {
                    PersonName = g.Key,
                    VoteCount = g.Count(),
                    InvalidReasonDesc = desc ?? "Unknown"
                };
            })
            .Where(x => !string.IsNullOrEmpty(x.InvalidReasonDesc) && x.InvalidReasonDesc != "Unknown")
            .OrderBy(x => x.PersonName)
            .ToList();

        return new SpoiledVotesReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = grouped
        };
    }

    public async Task<BallotAlignmentReportDto> GetBallotAlignmentAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var numToElect = election.NumberToElect ?? 0;

        var electedPersonGuids = await _context.Results
            .Where(r => r.ElectionGuid == electionGuid)
            .OrderBy(r => r.Rank)
            .Take(numToElect)
            .Select(r => r.PersonGuid)
            .ToListAsync();
        var electedSet = electedPersonGuids.ToHashSet();

        var ballots = await _context.Ballots
            .Include(b => b.Location)
            .Include(b => b.Votes)
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .ToListAsync();

        var alignmentCounts = new Dictionary<int, int>();
        for (var i = 0; i <= numToElect; i++) alignmentCounts[i] = 0;

        foreach (var ballot in ballots)
        {
            var matchCount = ballot.Votes.Count(v => v.PersonGuid.HasValue && electedSet.Contains(v.PersonGuid.Value));
            if (matchCount > numToElect) matchCount = numToElect;
            alignmentCounts[matchCount]++;
        }

        return new BallotAlignmentReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            NumToElect = numToElect,
            IsSingleNameElection = IsSingleNameElection(election),
            Rows = alignmentCounts
                .OrderByDescending(kv => kv.Key)
                .Select(kv => new AlignmentRowDto { MatchingNames = kv.Key, BallotCount = kv.Value })
                .ToList()
        };
    }

    public async Task<BallotsSameReportDto> GetBallotsSameAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var isSingleName = IsSingleNameElection(election);
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();
        var hasMultipleLocations = locations.Count > 1;

        var ballots = await _context.Ballots
            .Include(b => b.Location)
            .Include(b => b.Votes).ThenInclude(v => v.Person)
            .Where(b => b.Location.ElectionGuid == electionGuid && b.StatusCode != BallotStatus.Empty)
            .OrderBy(b => b.Location.Name)
            .ThenBy(b => b.ComputerCode)
            .ThenBy(b => b.BallotNumAtComputer)
            .ToListAsync();

        var grouped = ballots
            .Select(b =>
            {
                var orderedVotes = b.Votes
                    .OrderBy(v => isSingleName ? (v.Person?.FullNameFl ?? "") : v.PositionOnBallot.ToString("0000"))
                    .ToList();
                var hash = string.Join(",", orderedVotes.Select(v =>
                    (v.PersonGuid ?? Guid.Empty).ToString() + (v.IneligibleReasonCode ?? "")));
                return new { Hash = hash, Ballot = b };
            })
            .GroupBy(x => x.Hash)
            .Where(g => g.Count() > 1)
            .ToList();

        var groupNum = 1;
        var groups = grouped.Select(g => new DuplicateGroupDto
        {
            GroupNumber = groupNum++,
            Ballots = g.Select(x =>
            {
                var b = x.Ballot;
                var locName = hasMultipleLocations ? b.Location.Name : "";
                return new BallotReportItemDto
                {
                    BallotCode = b.BallotCode ?? "",
                    Location = locName,
                    BallotId = b.RowId,
                    LocationId = b.Location.RowId,
                    StatusCode = b.StatusCode.ToString(),
                    Spoiled = b.StatusCode != BallotStatus.Ok,
                    Votes = b.Votes
                        .OrderBy(v => isSingleName ? (v.Person?.FullNameFl ?? "") : v.PositionOnBallot.ToString("0000"))
                        .Select(v => new BallotVoteDto
                        {
                            PersonName = v.Person?.FullNameFl ?? "",
                            SingleNameElectionCount = v.SingleNameElectionCount,
                            Spoiled = v.VoteStatus != VoteStatus.Ok,
                            InvalidReasonDesc = GetIneligibleDescription(v.IneligibleReasonCode)
                        }).ToList()
                };
            }).ToList()
        }).ToList();

        return new BallotsSameReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            IsSingleNameElection = isSingleName,
            Groups = groups
        };
    }

    public async Task<BallotsSummaryReportDto> GetBallotsSummaryAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();
        var hasMultipleLocations = locations.Count > 1;

        var ballots = await _context.Ballots
            .Include(b => b.Location)
            .Include(b => b.Votes)
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .OrderBy(b => b.ComputerCode)
            .ThenBy(b => b.BallotNumAtComputer)
            .ToListAsync();

        return new BallotsSummaryReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            Ballots = ballots.Select(b => new BallotSummaryItemDto
            {
                BallotCode = b.BallotCode ?? "",
                Location = hasMultipleLocations ? b.Location.Name : "",
                LocationId = b.Location.RowId,
                BallotId = b.RowId,
                StatusCode = b.StatusCode.ToString(),
                Spoiled = b.StatusCode != BallotStatus.Ok,
                SpoiledVotes = b.StatusCode == BallotStatus.Ok
                    ? b.Votes.Count(v => v.VoteStatus != VoteStatus.Ok)
                    : 0,
                Teller1 = b.Teller1,
                Teller2 = b.Teller2
            }).ToList()
        };
    }
}
