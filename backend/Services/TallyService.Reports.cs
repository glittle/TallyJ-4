using Backend.DTOs.Results;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class TallyService
{
    /// <summary>
    /// Generates a comprehensive report for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>An ElectionReportDto containing comprehensive election report data.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<ElectionReportDto> GetElectionReportAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        var results = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid)
            .OrderBy(r => r.Rank)
            .ToListAsync();

        var summary = await _context.ResultSummaries
            .FirstOrDefaultAsync(rs => rs.ElectionGuid == electionGuid);

        var elected = results
            .Where(r => r.Section == "E")
            .Select(r => new PersonReportDto
            {
                Rank = r.Rank,
                FullName = r.Person?.FullNameFl ?? UnknownFallbackValue,
                VoteCount = r.VoteCount ?? 0,
                Section = SectionElected
            })
            .ToList();

        var extra = results
            .Where(r => r.Section == "X")
            .Select(r => new PersonReportDto
            {
                Rank = r.Rank,
                FullName = r.Person?.FullNameFl ?? UnknownFallbackValue,
                VoteCount = r.VoteCount ?? 0,
                Section = SectionExtra
            })
            .ToList();

        var other = results
            .Where(r => r.Section == "O")
            .Select(r => new PersonReportDto
            {
                Rank = r.Rank,
                FullName = r.Person?.FullNameFl ?? UnknownFallbackValue,
                VoteCount = r.VoteCount ?? 0,
                Section = SectionOther
            })
            .ToList();

        var ties = results
            .Where(r => r.IsTied == true && r.TieBreakGroup.HasValue)
            .GroupBy(r => r.TieBreakGroup!.Value)
            .Select(g => new TieReportDto
            {
                TieBreakGroup = g.Key,
                Section = g.First().Section ?? SectionOther,
                PersonNames = g.Select(r => r.Person?.FullNameFl ?? UnknownFallbackValue).ToList()
            })
            .ToList();

        return new ElectionReportDto
        {
            ElectionName = election.Name ?? UnknownElectionName,
            ElectionDate = election.DateOfElection,
            NumToElect = election.NumberToElect ?? 9,
            TotalBallots = summary?.BallotsReceived ?? 0,
            SpoiledBallots = summary?.SpoiledBallots ?? 0,
            TotalVotes = summary?.TotalVotes ?? 0,
            Elected = elected,
            Extra = extra,
            Other = other,
            Ties = ties
        };
    }

    /// <summary>
    /// Retrieves specific report data for an election based on a report code.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="reportCode">The code identifying the type of report to generate.</param>
    /// <returns>A ReportDataResponseDto containing the requested report data.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found or report code is invalid.</exception>
    public async Task<ReportDataResponseDto> GetReportDataAsync(Guid electionGuid, string reportCode)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        object data;

        switch (reportCode.ToLower())
        {
            case "ballots":
                data = await GetBallotReportDataAsync(electionGuid);
                break;
            case "voters":
                data = await GetVoterReportDataAsync(electionGuid);
                break;
            case "locations":
                data = await GetLocationReportDataAsync(electionGuid);
                break;
            case "summary":
                data = await GetSummaryReportDataAsync(electionGuid);
                break;
            case "ties":
                data = await GetTiesReportDataAsync(electionGuid);
                break;
            default:
                throw new ArgumentException($"Unknown report code: {reportCode}");
        }

        return new ReportDataResponseDto
        {
            ReportType = reportCode,
            Data = data
        };
    }

    private async Task<List<BallotReportDto>> GetBallotReportDataAsync(Guid electionGuid)
    {
        var ballots = await _context.Ballots
            .Include(b => b.Location)
            .Include(b => b.Votes)
            .ThenInclude(v => v.Person)
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .OrderBy(b => b.Location.Name)
            .ThenBy(b => b.BallotNumAtComputer)
            .ToListAsync();

        return ballots.Select(b => new BallotReportDto
        {
            BallotGuid = b.BallotGuid,
            LocationName = FormatLocationName(b.Location),
            Status = b.StatusCode,
            Votes = b.Votes
                .OrderBy(v => v.PositionOnBallot)
                .Select(v => new VoteReportDto
                {
                    FullName = v.Person != null ? v.Person.FullNameFl ?? UnknownFallbackValue : UnknownFallbackValue,
                    Position = v.PositionOnBallot
                })
                .ToList()
        }).ToList();
    }

    private async Task<List<VoterReportDto>> GetVoterReportDataAsync(Guid electionGuid)
    {
        var rows = await _context.People
            .Where(p => p.ElectionGuid == electionGuid)
            .GroupJoin(
                _context.Locations,
                p => p.VotingLocationGuid,
                l => l.LocationGuid,
                (p, locations) => new { Person = p, Locations = locations }
            )
            .SelectMany(
                x => x.Locations.DefaultIfEmpty(),
                (x, location) => new
                {
                    x.Person.PersonGuid,
                    FullName = x.Person.FullNameFl,
                    LocationName = location != null ? location.Name : null,
                    LocationTypeCode = location != null ? location.LocationTypeCode : null,
                    Voted = x.Person.HasOnlineBallot == true,
                }
            )
            .ToListAsync();

        return rows
            .Select(row => new VoterReportDto
            {
                PersonGuid = row.PersonGuid,
                FullName = row.FullName ?? UnknownFallbackValue,
                LocationName = row.LocationName == null && row.LocationTypeCode == null
                    ? UnknownFallbackValue
                    : FormatLocationName(row.LocationName, row.LocationTypeCode),
                Voted = row.Voted,
                VoteTime = null
            })
            .OrderBy(v => v.LocationName)
            .ThenBy(v => v.FullName)
            .ToList();
    }

    private async Task<List<LocationReportDto>> GetLocationReportDataAsync(Guid electionGuid)
    {
        var rows = await _context.Locations
            .Where(l => l.ElectionGuid == electionGuid)
            .Select(l => new
            {
                l.Name,
                l.LocationTypeCode,
                TotalVoters = _context.People.Count(p => p.ElectionGuid == electionGuid && p.VotingLocationGuid == l.LocationGuid && p.CanVote == true),
                Voted = _context.People.Count(p => p.ElectionGuid == electionGuid && p.VotingLocationGuid == l.LocationGuid && p.HasOnlineBallot == true),
                BallotsEntered = l.Ballots.Count(b => b.StatusCode == BallotStatus.Ok),
                TotalVotes = l.Ballots.Sum(b => b.Votes.Count)
            })
            .ToListAsync();

        return rows.Select(l => new LocationReportDto
        {
            LocationName = FormatLocationName(l.Name, l.LocationTypeCode),
            TotalVoters = l.TotalVoters,
            Voted = l.Voted,
            BallotsEntered = l.BallotsEntered,
            TotalVotes = l.TotalVotes
        }).ToList();
    }

    private async Task<ElectionReportDto> GetSummaryReportDataAsync(Guid electionGuid)
    {
        return await GetElectionReportAsync(electionGuid);
    }

    private async Task<List<TieReportDto>> GetTiesReportDataAsync(Guid electionGuid)
    {
        var results = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid && r.IsTied == true && r.TieBreakGroup.HasValue)
            .ToListAsync();

        return results
            .GroupBy(r => r.TieBreakGroup!.Value)
            .Select(g => new TieReportDto
            {
                TieBreakGroup = g.Key,
                Section = g.First().Section ?? SectionOther,
                PersonNames = g.Select(r => r.Person?.FullNameFl ?? UnknownFallbackValue).ToList()
            })
            .ToList();
    }
}
