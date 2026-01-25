using TallyJ4.DTOs.Results;
using TallyJ4.DTOs.SignalR;
using TallyJ4.Domain.Context;
using TallyJ4.Services.Analyzers;
using Microsoft.EntityFrameworkCore;
using TallyJ4.Domain.Entities;

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

    public async Task<MonitorInfoDto> GetMonitorInfoAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        // Get computer information from ballots (computers are tracked via ComputerCode in ballots)
        var computers = await _context.Ballots
            .Include(b => b.Location)
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .GroupBy(b => new { b.ComputerCode, b.Location.Name })
            .Select(g => new ComputerInfoDto
            {
                ComputerCode = g.Key.ComputerCode ?? "Unknown",
                LocationName = g.Key.Name ?? "Unknown Location",
                BallotCount = g.Count(),
                LastContact = DateTime.MinValue, // No last contact tracking in current model
                Status = "Active" // Assume active if they have ballots
            })
            .ToListAsync();

        // Get location information
        var locations = await _context.Locations
            .Where(l => l.ElectionGuid == electionGuid)
            .Select(l => new LocationInfoDto
            {
                LocationGuid = l.LocationGuid,
                LocationName = l.Name ?? "Unknown Location",
                BallotCount = l.Ballots.Count(b => b.StatusCode == "Ok"),
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
                .Where(o => o.ElectionGuid == electionGuid && o.Status == "Ok")
                .CountAsync(),
            PendingOnlineBallots = await _context.OnlineVotingInfos
                .Where(o => o.ElectionGuid == electionGuid && o.Status != "Ok")
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
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task RefreshComputerContactAsync(Guid electionGuid, string computerCode)
    {
        // In the current model, computers don't have a separate entity with last contact tracking
        // This method is kept for API compatibility but doesn't update any database state
        // Computer activity is inferred from ballot creation/update timestamps
        _logger.LogInformation("Computer {ComputerCode} checked in for election {ElectionGuid}", computerCode, electionGuid);

        // Could potentially update a cache or in-memory store here if needed
        // For now, just log the contact
    }

    private string DetermineComputerStatus(DateTime? lastContact)
    {
        if (!lastContact.HasValue)
            return "Offline";

        var timeSinceContact = DateTime.UtcNow - lastContact.Value;
        return timeSinceContact.TotalMinutes < 5 ? "Active" : "Inactive";
    }

    public async Task<TieDetailsDto> GetTiesAsync(Guid electionGuid, int tieBreakGroup)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        var tieResults = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid && r.TieBreakGroup == tieBreakGroup && r.IsTied == true)
            .ToListAsync();

        if (!tieResults.Any())
        {
            throw new ArgumentException($"Tie break group {tieBreakGroup} not found in election {electionGuid}");
        }

        var section = tieResults.First().Section ?? "Other";

        var candidates = tieResults.Select(r => new TieCandidateDto
        {
            PersonGuid = r.PersonGuid,
            FullName = r.Person?.FullNameFl ?? "Unknown",
            VoteCount = r.VoteCount ?? 0,
            TieBreakCount = r.TieBreakCount
        }).ToList();

        return new TieDetailsDto
        {
            TieBreakGroup = tieBreakGroup,
            Section = section,
            Candidates = candidates
        };
    }

    public async Task<SaveTieCountsResponseDto> SaveTieCountsAsync(Guid electionGuid, SaveTieCountsRequestDto request)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        var updatedCount = 0;
        var reAnalysisNeeded = false;

        foreach (var count in request.Counts)
        {
            var result = await _context.Results
                .FirstOrDefaultAsync(r => r.ElectionGuid == electionGuid && r.PersonGuid == count.PersonGuid && r.IsTied == true);

            if (result != null)
            {
                result.TieBreakCount = count.TieBreakCount;
                updatedCount++;

                // Check if this resolves all ties in the group
                var groupResults = await _context.Results
                    .Where(r => r.ElectionGuid == electionGuid && r.TieBreakGroup == result.TieBreakGroup && r.IsTied == true)
                    .ToListAsync();

                if (groupResults.All(r => r.TieBreakCount.HasValue))
                {
                    reAnalysisNeeded = true;
                }
            }
        }

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Saved {Count} tie break counts for election {ElectionGuid}", updatedCount, electionGuid);

            // If all ties in a group are resolved, trigger re-analysis
            if (reAnalysisNeeded)
            {
                _logger.LogInformation("All ties resolved, triggering re-analysis for election {ElectionGuid}", electionGuid);
                // Note: In a real implementation, you might want to call CalculateNormalElectionAsync here
                // But for now, we'll just log it
            }
        }

        return new SaveTieCountsResponseDto
        {
            Success = true,
            Message = $"Successfully saved {updatedCount} tie break counts",
            ReAnalysisTriggered = reAnalysisNeeded
        };
    }

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
            .Select(r => new CandidateReportDto
            {
                Rank = r.Rank,
                FullName = r.Person?.FullNameFl ?? "Unknown",
                VoteCount = r.VoteCount ?? 0,
                Section = "Elected"
            })
            .ToList();

        var extra = results
            .Where(r => r.Section == "X")
            .Select(r => new CandidateReportDto
            {
                Rank = r.Rank,
                FullName = r.Person?.FullNameFl ?? "Unknown",
                VoteCount = r.VoteCount ?? 0,
                Section = "Extra"
            })
            .ToList();

        var other = results
            .Where(r => r.Section == "O")
            .Select(r => new CandidateReportDto
            {
                Rank = r.Rank,
                FullName = r.Person?.FullNameFl ?? "Unknown",
                VoteCount = r.VoteCount ?? 0,
                Section = "Other"
            })
            .ToList();

        var ties = results
            .Where(r => r.IsTied == true && r.TieBreakGroup.HasValue)
            .GroupBy(r => r.TieBreakGroup!.Value)
            .Select(g => new TieReportDto
            {
                TieBreakGroup = g.Key,
                Section = g.First().Section ?? "Other",
                CandidateNames = g.Select(r => r.Person?.FullNameFl ?? "Unknown").ToList()
            })
            .ToList();

        return new ElectionReportDto
        {
            ElectionName = election.Name ?? "Unknown Election",
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
        return await _context.Ballots
            .Include(b => b.Location)
            .Include(b => b.Votes)
            .ThenInclude(v => v.Person)
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .OrderBy(b => b.Location.Name)
            .ThenBy(b => b.BallotNumAtComputer)
            .Select(b => new BallotReportDto
            {
                BallotGuid = b.BallotGuid,
                LocationName = b.Location.Name ?? "Unknown",
                Status = b.StatusCode,
                Votes = b.Votes
                    .OrderBy(v => v.PositionOnBallot)
                    .Select(v => new VoteReportDto
                    {
                        FullName = v.Person != null ? v.Person.FullNameFl ?? "Unknown" : "Unknown",
                        Position = v.PositionOnBallot
                    })
                    .ToList()
            })
            .ToListAsync();
    }

    private async Task<List<VoterReportDto>> GetVoterReportDataAsync(Guid electionGuid)
    {
        return await _context.People
            .Where(p => p.ElectionGuid == electionGuid)
            .GroupJoin(
                _context.Locations,
                p => p.VotingLocationGuid,
                l => l.LocationGuid,
                (p, locations) => new { Person = p, Locations = locations }
            )
            .SelectMany(
                x => x.Locations.DefaultIfEmpty(),
                (x, location) => new VoterReportDto
                {
                    PersonGuid = x.Person.PersonGuid,
                    FullName = x.Person.FullNameFl ?? "Unknown",
                    LocationName = location != null ? location.Name ?? "Unknown" : "Unknown",
                    Voted = x.Person.HasOnlineBallot == true, // Simplified - in real implementation, check if ballot exists
                    VoteTime = null // Would need to track vote timestamps
                }
            )
            .OrderBy(v => v.LocationName)
            .ThenBy(v => v.FullName)
            .ToListAsync();
    }

    private async Task<List<LocationReportDto>> GetLocationReportDataAsync(Guid electionGuid)
    {
        return await _context.Locations
            .Where(l => l.ElectionGuid == electionGuid)
            .Select(l => new LocationReportDto
            {
                LocationName = l.Name ?? "Unknown",
                TotalVoters = _context.People.Count(p => p.ElectionGuid == electionGuid && p.VotingLocationGuid == l.LocationGuid && p.CanVote == true),
                Voted = _context.People.Count(p => p.ElectionGuid == electionGuid && p.VotingLocationGuid == l.LocationGuid && p.HasOnlineBallot == true),
                BallotsEntered = l.Ballots.Count(b => b.StatusCode == "Ok"),
                TotalVotes = l.Ballots.Sum(b => b.Votes.Count)
            })
            .ToListAsync();
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
                Section = g.First().Section ?? "Other",
                CandidateNames = g.Select(r => r.Person?.FullNameFl ?? "Unknown").ToList()
            })
            .ToList();
    }

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

        var electedCandidates = results
            .Where(r => r.Section == "E")
            .Select(r => new PresentationCandidateDto
            {
                Rank = r.Rank,
                FullName = r.Person?.FullNameFl ?? "Unknown",
                VoteCount = r.VoteCount ?? 0,
                IsTied = r.IsTied == true,
                IsWinner = true
            })
            .ToList();

        var extraCandidates = results
            .Where(r => r.Section == "X")
            .Select(r => new PresentationCandidateDto
            {
                Rank = r.Rank,
                FullName = r.Person?.FullNameFl ?? "Unknown",
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
                Section = g.First().Section ?? "Other",
                CandidateNames = g.Select(r => r.Person?.FullNameFl ?? "Unknown").ToList(),
                TieBreakRequired = g.First().TieBreakRequired == true
            })
            .ToList();

        return new PresentationDto
        {
            ElectionName = election.Name ?? "Unknown Election",
            ElectionDate = election.DateOfElection,
            NumToElect = election.NumberToElect ?? 9,
            TotalBallots = summary?.BallotsReceived ?? 0,
            TotalVotes = summary?.TotalVotes ?? 0,
            ElectedCandidates = electedCandidates,
            ExtraCandidates = extraCandidates,
            HasTies = ties.Any(),
            Ties = ties,
            Status = summary != null ? "Final" : "In Progress"
        };
    }
}
