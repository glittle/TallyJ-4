using TallyJ4.DTOs.Results;
using TallyJ4.DTOs.SignalR;
using TallyJ4.EF.Context;
using TallyJ4.Services.Analyzers;
using Microsoft.EntityFrameworkCore;

namespace TallyJ4.Services;

public class TallyService : ITallyService
{
    private readonly MainDbContext _context;
    private readonly ILogger<TallyService> _logger;
    private readonly ISignalRNotificationService _signalRNotificationService;

    public TallyService(MainDbContext context, ILogger<TallyService> logger, ISignalRNotificationService signalRNotificationService)
    {
        _context = context;
        _logger = logger;
        _signalRNotificationService = signalRNotificationService;
    }

    public async Task<TallyResultDto> CalculateNormalElectionAsync(Guid electionGuid)
    {
        _logger.LogInformation("Starting normal election tally calculation for election {ElectionGuid}", electionGuid);

        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        var totalBallots = await _context.Ballots
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .CountAsync();

        await _signalRNotificationService.SendTallyProgressAsync(new TallyProgressDto
        {
            ElectionGuid = electionGuid,
            TotalBallots = totalBallots,
            ProcessedBallots = 0,
            TotalVotes = 0,
            Message = "Starting tally calculation...",
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
            Message = "Tally calculation complete!",
            PercentComplete = 100,
            IsComplete = true
        });

        var result = await GetTallyResultsAsync(electionGuid);
        _logger.LogInformation("Completed tally calculation for election {ElectionGuid}", electionGuid);

        return result;
    }

    public async Task<TallyResultDto> CalculateSingleNameElectionAsync(Guid electionGuid)
    {
        _logger.LogInformation("Starting single-name election tally calculation for election {ElectionGuid}", electionGuid);

        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        var totalBallots = await _context.Ballots
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .CountAsync();

        await _signalRNotificationService.SendTallyProgressAsync(new TallyProgressDto
        {
            ElectionGuid = electionGuid,
            TotalBallots = totalBallots,
            ProcessedBallots = 0,
            TotalVotes = 0,
            Message = "Starting single-name tally calculation...",
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
            Message = "Single-name tally calculation complete!",
            PercentComplete = 100,
            IsComplete = true
        });

        var result = await GetTallyResultsAsync(electionGuid);
        _logger.LogInformation("Completed single-name tally calculation for election {ElectionGuid}", electionGuid);

        return result;
    }

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
                CandidateNames = g.Select(r => r.Person?.FullNameFl ?? "Unknown").ToList()
            })
            .ToList();

        return new TallyResultDto
        {
            ElectionGuid = electionGuid,
            ElectionName = election.Name ?? "Unknown Election",
            CalculatedAt = DateTime.UtcNow,
            Statistics = statistics,
            Results = results.Select(r => new CandidateResultDto
            {
                PersonGuid = r.PersonGuid,
                FullName = r.Person?.FullNameFl ?? "Unknown",
                VoteCount = r.VoteCount ?? 0,
                Rank = r.Rank,
                Section = r.Section ?? "Other",
                IsTied = r.IsTied == true,
                TieBreakGroup = r.TieBreakGroup,
                TieBreakRequired = r.TieBreakRequired == true,
                CloseToNext = r.CloseToNext == true,
                CloseToPrev = r.CloseToPrev == true
            }).ToList(),
            Ties = ties
        };
    }

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

        var numEligibleCandidates = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanReceiveVotes == true)
            .CountAsync();

        if (summary == null)
        {
            return new TallyStatisticsDto
            {
                NumVoters = numVoters,
                NumEligibleCandidates = numEligibleCandidates,
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
            NumEligibleCandidates = numEligibleCandidates,
            NumberToElect = election.NumberToElect ?? 9,
            NumberExtra = election.NumberExtra ?? 0
        };
    }
}
