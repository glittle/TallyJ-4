using Backend.DTOs.Results;
using Backend.DTOs.SignalR;
using Backend.Services.Analyzers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class TallyService
{
    /// <summary>
    /// Calculates the results for a normal election using the configured tally method.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to calculate.</param>
    /// <returns>A TallyResultDto containing the calculated election results.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<TallyResultDto> CalculateNormalElectionAsync(Guid electionGuid)
    {
        _logger.LogInformation("Starting normal election tally calculation for election {ElectionGuid}", electionGuid);

        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        await ElectionAnalysisPreparation.PrepareAsync(_context, electionGuid, _logger);
        await ElectionBallotBlocking.EnsureAnalysisCanProceedAsync(_context, electionGuid);

        var totalBallots = await _context.Ballots
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .CountAsync();

        await _signalRNotificationService.SendTallyProgressAsync(new TallyProgressDto
        {
            ElectionGuid = electionGuid,
            TotalBallots = totalBallots,
            ProcessedBallots = 0,
            TotalVotes = 0,
            Message = "tally.progress.starting",
            PercentComplete = 0,
            IsComplete = false
        });

        var analyzer = new ElectionAnalyzerNormal(_context, _logger, election);
        await analyzer.AnalyzeAsync();

        var totalVotes = await _context.Results
            .Where(r => r.ElectionGuid == electionGuid)
            .SumAsync(r => r.VoteCount ?? 0);

        await _signalRNotificationService.SendTallyProgressAsync(new TallyProgressDto
        {
            ElectionGuid = electionGuid,
            TotalBallots = totalBallots,
            ProcessedBallots = totalBallots,
            TotalVotes = totalVotes,
            Message = "tally.progress.complete",
            PercentComplete = 100,
            IsComplete = true
        });

        var result = await GetTallyResultsAsync(electionGuid);
        _logger.LogInformation("Completed tally calculation for election {ElectionGuid}", electionGuid);

        return result;
    }

    /// <summary>
    /// Calculates the results for a single-name election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to calculate.</param>
    /// <returns>A TallyResultDto containing the calculated election results.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<TallyResultDto> CalculateSingleNameElectionAsync(Guid electionGuid)
    {
        _logger.LogInformation("Starting single-name election tally calculation for election {ElectionGuid}", electionGuid);

        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        await ElectionAnalysisPreparation.PrepareAsync(_context, electionGuid, _logger);
        await ElectionBallotBlocking.EnsureAnalysisCanProceedAsync(_context, electionGuid);

        var totalBallots = await _context.Ballots
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .CountAsync();

        await _signalRNotificationService.SendTallyProgressAsync(new TallyProgressDto
        {
            ElectionGuid = electionGuid,
            TotalBallots = totalBallots,
            ProcessedBallots = 0,
            TotalVotes = 0,
            Message = "tally.progress.startingSingleName",
            PercentComplete = 0,
            IsComplete = false
        });

        var analyzer = new ElectionAnalyzerSingleName(_context, _logger, election);
        await analyzer.AnalyzeAsync();

        var totalVotes = await _context.Results
            .Where(r => r.ElectionGuid == electionGuid)
            .SumAsync(r => r.VoteCount ?? 0);

        await _signalRNotificationService.SendTallyProgressAsync(new TallyProgressDto
        {
            ElectionGuid = electionGuid,
            TotalBallots = totalBallots,
            ProcessedBallots = totalBallots,
            TotalVotes = totalVotes,
            Message = "tally.progress.completeSingleName",
            PercentComplete = 100,
            IsComplete = true
        });

        var result = await GetTallyResultsAsync(electionGuid);
        _logger.LogInformation("Completed single-name tally calculation for election {ElectionGuid}", electionGuid);

        return result;
    }

    /// <summary>
    /// Retrieves the current tally results for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A TallyResultDto containing the current election results.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<TallyResultDto> GetTallyResultsAsync(Guid electionGuid)
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

        var statistics = await GetTallyStatisticsAsync(electionGuid);

        var ties = results
            .Where(r => r.IsTied == true && r.TieBreakGroup.HasValue)
            .GroupBy(r => r.TieBreakGroup!.Value)
            .Select(g => new TieInfoDto
            {
                TieBreakGroup = g.Key,
                VoteCount = g.First().VoteCount ?? 0,
                TieBreakRequired = g.First().TieBreakRequired == true,
                Section = g.First().Section ?? string.Empty,
                PersonNames = g.Select(r => r.Person?.FullNameFl ?? UnknownFallbackValue).ToList()
            })
            .ToList();

        return new TallyResultDto
        {
            ElectionGuid = electionGuid,
            ElectionName = election.Name ?? UnknownElectionName,
            CalculatedAt = DateTimeOffset.UtcNow,
            Statistics = statistics,
            Results = results.Select(r => new PersonResultDto
            {
                PersonGuid = r.PersonGuid,
                FullName = r.Person?.FullNameFl ?? UnknownFallbackValue,
                VoteCount = r.VoteCount ?? 0,
                Rank = r.Rank,
                Section = r.Section ?? SectionOther,
                IsTied = r.IsTied == true,
                TieBreakGroup = r.TieBreakGroup,
                TieBreakRequired = r.TieBreakRequired == true,
                CloseToNext = r.CloseToNext == true,
                CloseToPrev = r.CloseToPrev == true
            }).ToList(),
            Ties = ties
        };
    }

    /// <summary>
    /// Retrieves statistical information about an election's tally.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A TallyStatisticsDto containing statistical information about the election.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<TallyStatisticsDto> GetTallyStatisticsAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        var summary = await _context.ResultSummaries
            .FirstOrDefaultAsync(rs => rs.ElectionGuid == electionGuid);

        var numVoters = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true)
            .CountAsync();

        var numEligiblePeople = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanReceiveVotes == true)
            .CountAsync();

        if (summary == null)
        {
            return new TallyStatisticsDto
            {
                NumVoters = numVoters,
                NumEligiblePeople = numEligiblePeople,
                NumberToElect = election.NumberToElect ?? 9,
                NumberExtra = election.NumberExtra ?? 0
            };
        }

        return new TallyStatisticsDto
        {
            TotalBallots = (summary.BallotsReceived ?? 0) + (summary.SpoiledBallots ?? 0),
            BallotsReceived = summary.BallotsReceived ?? 0,
            SpoiledBallots = summary.SpoiledBallots ?? 0,
            BallotsNeedingReview = summary.BallotsNeedingReview ?? 0,
            TotalVotes = summary.TotalVotes ?? 0,
            ValidVotes = (summary.TotalVotes ?? 0) - (summary.SpoiledVotes ?? 0),
            InvalidVotes = summary.SpoiledVotes ?? 0,
            NumVoters = numVoters,
            NumEligiblePeople = numEligiblePeople,
            NumberToElect = election.NumberToElect ?? 9,
            NumberExtra = election.NumberExtra ?? 0
        };
    }
}
