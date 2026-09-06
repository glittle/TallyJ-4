using Backend.DTOs.Results;
using Backend.Entities;
using Backend.Enumerations;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class TallyService
{
    /// <summary>
    /// Retrieves presentation-ready data for displaying election results.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A PresentationDto containing formatted data for presentation purposes.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<PresentationDto> GetPresentationDataAsync(Guid electionGuid)
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

        var electedPeople = results
            .Where(r => r.Section == "E")
            .Select(r => new PresentationPersonDto
            {
                Rank = r.Rank,
                FullName = r.Person?.FullNameFl ?? UnknownFallbackValue,
                VoteCount = r.VoteCount ?? 0,
                IsTied = r.IsTied == true,
                IsWinner = true
            })
            .ToList();

        var extraPeople = results
            .Where(r => r.Section == "X")
            .Select(r => new PresentationPersonDto
            {
                Rank = r.Rank,
                FullName = r.Person?.FullNameFl ?? UnknownFallbackValue,
                VoteCount = r.VoteCount ?? 0,
                IsTied = r.IsTied == true,
                IsWinner = false
            })
            .ToList();

        var ties = results
            .Where(r => r.IsTied == true && r.TieBreakGroup.HasValue)
            .GroupBy(r => r.TieBreakGroup!.Value)
            .Select(g => new PresentationTieDto
            {
                TieBreakGroup = g.Key,
                Section = g.First().Section ?? SectionOther,
                PersonNames = g.Select(r => r.Person?.FullNameFl ?? UnknownFallbackValue).ToList(),
                TieBreakRequired = g.First().TieBreakRequired == true
            })
            .ToList();

        return new PresentationDto
        {
            ElectionName = election.Name ?? UnknownElectionName,
            ElectionDate = election.DateOfElection,
            NumToElect = election.NumberToElect ?? 9,
            TotalBallots = summary?.BallotsReceived ?? 0,
            TotalVotes = summary?.TotalVotes ?? 0,
            ElectedPeople = electedPeople,
            ExtraPeople = extraPeople,
            HasTies = ties.Any(),
            Ties = ties,
            Status = summary != null ? "Final" : "In Progress"
        };
    }

    private ElectionOverviewDto CalculateElectionOverview(Election election, int totalRegisteredVoters, int totalBallotsCast, int validBallots, int spoiledBallots, int totalVotes)
    {
        return new ElectionOverviewDto
        {
            ElectionName = election.Name ?? UnknownElectionName,
            ElectionDate = election.DateOfElection,
            TotalRegisteredVoters = totalRegisteredVoters,
            TotalBallotsCast = totalBallotsCast,
            ValidBallots = validBallots,
            SpoiledBallots = spoiledBallots,
            TotalVotes = totalVotes,
            PositionsToElect = election.NumberToElect ?? 9,
            OverallTurnoutPercentage = totalRegisteredVoters > 0 ? (decimal)totalBallotsCast / totalRegisteredVoters * 100 : 0
        };
    }

    private VoteDistributionDto CalculateVoteDistribution(Election election, List<Ballot> allBallots)
    {
        var votesPerPosition = new int[election.NumberToElect ?? 9];
        var ballotLengths = allBallots
            .Where(b => b.StatusCode == BallotStatus.Ok)
            .Select(b => b.Votes.Count)
            .ToList();

        foreach (var ballot in allBallots.Where(b => b.StatusCode == BallotStatus.Ok))
        {
            var voteCount = ballot.Votes.Count;
            if (voteCount > 0 && voteCount <= votesPerPosition.Length)
            {
                votesPerPosition[voteCount - 1]++;
            }
        }

        return new VoteDistributionDto
        {
            VotesPerPosition = votesPerPosition,
            AverageVotesPerBallot = ballotLengths.Any() ? ballotLengths.Average() : 0,
            MaxVotesOnSingleBallot = ballotLengths.Any() ? ballotLengths.Max() : 0,
            MinVotesOnSingleBallot = ballotLengths.Any() ? ballotLengths.Min() : 0,
            BallotLengthDistribution = ballotLengths
                .GroupBy(l => l)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    private PersonPerformanceDto[] CalculatePersonPerformance(List<Result> results, int totalVotes)
    {
        var personPerformance = results
            .GroupBy(r => r.PersonGuid)
            .Select(g =>
            {
                var person = g.First().Person;
                var totalVotesForPerson = g.Sum(r => r.VoteCount ?? 0);
                var rank = g.Min(r => r.Rank);
                var isElected = g.Any(r => r.Section == "E");
                var isEliminated = g.All(r => r.Section == "O");

                var votesByPosition = g
                    .Where(r => r.VoteCount.HasValue)
                    .ToDictionary(r => r.Rank, r => r.VoteCount!.Value);

                var firstChoiceVotes = g.FirstOrDefault(r => r.Rank == 1)?.VoteCount ?? 0;
                var lastChoiceVotes = g.OrderByDescending(r => r.Rank).FirstOrDefault()?.VoteCount ?? 0;

                return new PersonPerformanceDto
                {
                    PersonGuid = g.Key,
                    FullName = person?.FullNameFl ?? UnknownFallbackValue,
                    TotalVotes = totalVotesForPerson,
                    VotePercentage = totalVotes > 0 ? (decimal)totalVotesForPerson / totalVotes * 100 : 0,
                    Rank = rank,
                    IsElected = isElected,
                    IsEliminated = isEliminated,
                    VotesByPosition = votesByPosition,
                    FirstChoicePercentage = totalVotesForPerson > 0 ? (decimal)firstChoiceVotes / totalVotesForPerson * 100 : 0,
                    LastChoicePercentage = totalVotesForPerson > 0 ? (decimal)lastChoiceVotes / totalVotesForPerson * 100 : 0
                };
            })
            .OrderBy(c => c.Rank)
            .ToArray();

        return personPerformance;
    }

    private async Task<TurnoutAnalysisDto> CalculateTurnoutAnalysisAsync(Guid electionGuid, List<Location> locations, int totalRegisteredVoters, int totalBallotsCast, Election election)
    {
        var turnoutByLocation = await CalculateTurnoutByLocationAsync(electionGuid, locations);

        var demographicBreakdown = new List<DemographicTurnoutDto>();
        await CalculateDemographicAreaBreakdownAsync(electionGuid, demographicBreakdown);

        var timeBasedTurnout = await CalculateTimeBasedTurnoutAsync(totalBallotsCast, totalRegisteredVoters, election);

        var participationRates = await CalculateParticipationRatesAsync(electionGuid, totalBallotsCast, totalRegisteredVoters);

        return new TurnoutAnalysisDto
        {
            OverallTurnout = totalRegisteredVoters > 0 ? (decimal)totalBallotsCast / totalRegisteredVoters * 100 : 0,
            TurnoutByLocation = turnoutByLocation,
            EarlyVotingCount = 0, // Would need timestamp tracking
            ElectionDayVotingCount = totalBallotsCast,
            EarlyVotingPercentage = 0, // Would need timestamp tracking
            DemographicBreakdown = demographicBreakdown,
            TimeBasedTurnout = timeBasedTurnout,
            ParticipationRates = participationRates
        };
    }

    private async Task<Dictionary<string, decimal>> CalculateTurnoutByLocationAsync(Guid electionGuid, List<Location> locations)
    {
        var turnoutByLocation = new Dictionary<string, decimal>();
        foreach (var location in locations)
        {
            var locationVoterCount = await _context.People
                .CountAsync(p => p.ElectionGuid == electionGuid &&
                               p.VotingLocationGuid == location.LocationGuid &&
                               p.CanVote == true);

            var locationBallotCount = location.Ballots.Count(b => b.StatusCode == BallotStatus.Ok);
            var turnout = locationVoterCount > 0
                ? (decimal)locationBallotCount / locationVoterCount * 100
                : 0;

            turnoutByLocation[FormatLocationName(location)] = turnout;
        }
        return turnoutByLocation;
    }

    private async Task CalculateDemographicAreaBreakdownAsync(Guid electionGuid, List<DemographicTurnoutDto> demographicBreakdown)
    {
        var areas = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true && p.Area != null)
            .GroupBy(p => p.Area)
            .Select(g => new
            {
                Area = g.Key,
                TotalVoters = g.Count(),
                Voted = g.Count(p => p.HasOnlineBallot == true)
            })
            .ToListAsync();

        foreach (var area in areas)
        {
            demographicBreakdown.Add(new DemographicTurnoutDto
            {
                DemographicCategory = "Area",
                DemographicValue = area.Area ?? UnknownFallbackValue,
                TotalVoters = area.TotalVoters,
                Voted = area.Voted,
                TurnoutPercentage = area.TotalVoters > 0 ? (decimal)area.Voted / area.TotalVoters * 100 : 0
            });
        }
    }

    private async Task<List<TimeBasedTurnoutDto>> CalculateTimeBasedTurnoutAsync(int totalBallotsCast, int totalRegisteredVoters, Election election)
    {
        var timeBasedTurnout = new List<TimeBasedTurnoutDto>();

        var logEntries = await _context.SecurityAuditLogs
            .Where(l => l.ElectionGuid == election.ElectionGuid && l.Details != null && l.Details.Contains("ballot"))
            .OrderBy(l => l.Timestamp)
            .Select(l => l.Timestamp)
            .ToListAsync();

        if (logEntries.Count > 0)
        {
            var grouped = logEntries
                .GroupBy(d => new DateTimeOffset(d.Year, d.Month, d.Day, d.Hour, 0, 0, d.Offset))
                .OrderBy(g => g.Key);

            var cumulativeBallots = 0;
            foreach (var group in grouped)
            {
                cumulativeBallots += group.Count();
                timeBasedTurnout.Add(new TimeBasedTurnoutDto
                {
                    TimePeriod = group.Key,
                    PeriodType = "Hour",
                    BallotsCast = group.Count(),
                    CumulativeTurnout = totalRegisteredVoters > 0 ? (decimal)cumulativeBallots / totalRegisteredVoters * 100 : 0
                });
            }
        }
        else
        {
            var electionDate = election.DateOfElection ?? DateTimeOffset.UtcNow;
            timeBasedTurnout.Add(new TimeBasedTurnoutDto
            {
                TimePeriod = electionDate,
                PeriodType = "Total",
                BallotsCast = totalBallotsCast,
                CumulativeTurnout = totalRegisteredVoters > 0 ? (decimal)totalBallotsCast / totalRegisteredVoters * 100 : 0
            });
        }

        return timeBasedTurnout;
    }

    private async Task<ParticipationRateDto> CalculateParticipationRatesAsync(Guid electionGuid, int totalBallotsCast, int totalRegisteredVoters)
    {
        var onlineVoters = await _context.People
            .CountAsync(p => p.ElectionGuid == electionGuid && p.HasOnlineBallot == true);

        var inPersonVoters = totalBallotsCast - onlineVoters;

        return new ParticipationRateDto
        {
            FirstTimeVoters = 0, // Would need historical data
            ReturningVoters = 0, // Would need historical data
            OnlineVoters = totalRegisteredVoters > 0 ? (decimal)onlineVoters / totalRegisteredVoters * 100 : 0,
            InPersonVoters = totalRegisteredVoters > 0 ? (decimal)inPersonVoters / totalRegisteredVoters * 100 : 0,
            ParticipationByMethod = new Dictionary<string, decimal>
            {
                ["Online"] = totalRegisteredVoters > 0 ? (decimal)onlineVoters / totalRegisteredVoters * 100 : 0,
                ["In-Person"] = totalRegisteredVoters > 0 ? (decimal)inPersonVoters / totalRegisteredVoters * 100 : 0
            }
        };
    }

    private async Task<List<LocationStatisticsDto>> CalculateLocationStatisticsAsync(Guid electionGuid, List<Location> locations)
    {
        var locationStatistics = new List<LocationStatisticsDto>();
        foreach (var location in locations)
        {
            var locationVoters = await _context.People
                .CountAsync(p => p.ElectionGuid == electionGuid &&
                               p.VotingLocationGuid == location.LocationGuid &&
                               p.CanVote == true);

            var locationBallots = location.Ballots.Count(b => b.StatusCode == BallotStatus.Ok);
            var locationVotes = location.Ballots.Sum(b => b.Votes.Count);

            // Get top People for this location
            var locationPersonVotes = location.Ballots
                .Where(b => b.StatusCode == BallotStatus.Ok)
                .SelectMany(b => b.Votes)
                .GroupBy(v => v.Person?.FullNameFl ?? UnknownFallbackValue)
                .OrderByDescending(g => g.Count())
                .Take(3)
                .ToDictionary(g => g.Key, g => g.Count());

            locationStatistics.Add(new LocationStatisticsDto
            {
                LocationName = FormatLocationName(location),
                RegisteredVoters = locationVoters,
                BallotsCast = locationBallots,
                ValidBallots = locationBallots,
                SpoiledBallots = location.Ballots.Count(b => b.StatusCode != BallotStatus.Ok),
                TurnoutPercentage = locationVoters > 0 ? (decimal)locationBallots / locationVoters * 100 : 0,
                TotalVotes = locationVotes,
                TopPeople = locationPersonVotes
            });
        }

        return locationStatistics;
    }
}
