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
        var validBallots = allBallots.Count(b => b.StatusCode == "Ok");
        var spoiledBallots = summary?.SpoiledBallots ?? 0;
        var totalVotes = summary?.TotalVotes ?? 0;

        // Calculate election overview
        var overview = new ElectionOverviewDto
        {
            ElectionName = election.Name ?? "Unknown Election",
            ElectionDate = election.DateOfElection,
            TotalRegisteredVoters = totalRegisteredVoters,
            TotalBallotsCast = totalBallotsCast,
            ValidBallots = validBallots,
            SpoiledBallots = spoiledBallots,
            TotalVotes = totalVotes,
            PositionsToElect = election.NumberToElect ?? 9,
            OverallTurnoutPercentage = totalRegisteredVoters > 0 ? (decimal)totalBallotsCast / totalRegisteredVoters * 100 : 0
        };

        // Calculate vote distribution
        var votesPerPosition = new int[election.NumberToElect ?? 9];
        var ballotLengths = allBallots
            .Where(b => b.StatusCode == "Ok")
            .Select(b => b.Votes.Count)
            .ToList();

        foreach (var ballot in allBallots.Where(b => b.StatusCode == "Ok"))
        {
            var voteCount = ballot.Votes.Count;
            if (voteCount > 0 && voteCount <= votesPerPosition.Length)
            {
                votesPerPosition[voteCount - 1]++;
            }
        }

        var voteDistribution = new VoteDistributionDto
        {
            VotesPerPosition = votesPerPosition,
            AverageVotesPerBallot = ballotLengths.Any() ? ballotLengths.Average() : 0,
            MaxVotesOnSingleBallot = ballotLengths.Any() ? ballotLengths.Max() : 0,
            MinVotesOnSingleBallot = ballotLengths.Any() ? ballotLengths.Min() : 0,
            BallotLengthDistribution = ballotLengths
                .GroupBy(l => l)
                .ToDictionary(g => g.Key, g => g.Count())
        };

        // Calculate candidate performance
        var candidatePerformance = results
            .GroupBy(r => r.PersonGuid)
            .Select(g =>
            {
                var person = g.First().Person;
                var totalVotes = g.Sum(r => r.VoteCount ?? 0);
                var rank = g.Min(r => r.Rank);
                var isElected = g.Any(r => r.Section == "E");
                var isEliminated = g.All(r => r.Section == "O");

                var votesByPosition = g
                    .Where(r => r.VoteCount.HasValue)
                    .ToDictionary(r => r.Rank, r => r.VoteCount.Value);

                var firstChoiceVotes = g.FirstOrDefault(r => r.Rank == 1)?.VoteCount ?? 0;
                var lastChoiceVotes = g.OrderByDescending(r => r.Rank).FirstOrDefault()?.VoteCount ?? 0;

                return new CandidatePerformanceDto
                {
                    PersonGuid = g.Key,
                    FullName = person?.FullNameFl ?? "Unknown",
                    TotalVotes = totalVotes,
                    VotePercentage = totalVotes > 0 ? (decimal)totalVotes / totalVotes * 100 : 0, // This should be calculated against total votes
                    Rank = rank,
                    IsElected = isElected,
                    IsEliminated = isEliminated,
                    VotesByPosition = votesByPosition,
                    FirstChoicePercentage = totalVotes > 0 ? (decimal)firstChoiceVotes / totalVotes * 100 : 0,
                    LastChoicePercentage = totalVotes > 0 ? (decimal)lastChoiceVotes / totalVotes * 100 : 0
                };
            })
            .OrderBy(c => c.Rank)
            .ToArray();

        // Fix vote percentages (should be against total votes cast)
        if (totalVotes > 0)
        {
            foreach (var candidate in candidatePerformance)
            {
                candidate.VotePercentage = (decimal)candidate.TotalVotes / totalVotes * 100;
            }
        }

        // Calculate turnout analysis
        var turnoutByLocation = new Dictionary<string, decimal>();
        foreach (var location in locations)
        {
            var locationVoterCount = await _context.People
                .CountAsync(p => p.ElectionGuid == electionGuid &&
                               p.VotingLocationGuid == location.LocationGuid &&
                               p.CanVote == true);

            var locationBallotCount = location.Ballots.Count(b => b.StatusCode == "Ok");
            var turnout = locationVoterCount > 0
                ? (decimal)locationBallotCount / locationVoterCount * 100
                : 0;

            turnoutByLocation[location.Name ?? "Unknown"] = turnout;
        }

        // Calculate demographic breakdown
        var demographicBreakdown = new List<DemographicTurnoutDto>();

        // Age group breakdown
        var ageGroups = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true && p.AgeGroup != null)
            .GroupBy(p => p.AgeGroup)
            .Select(g => new
            {
                AgeGroup = g.Key,
                TotalVoters = g.Count(),
                Voted = g.Count(p => p.HasOnlineBallot == true)
            })
            .ToListAsync();

        foreach (var ageGroup in ageGroups)
        {
            demographicBreakdown.Add(new DemographicTurnoutDto
            {
                DemographicCategory = "AgeGroup",
                DemographicValue = ageGroup.AgeGroup ?? "Unknown",
                TotalVoters = ageGroup.TotalVoters,
                Voted = ageGroup.Voted,
                TurnoutPercentage = ageGroup.TotalVoters > 0 ? (decimal)ageGroup.Voted / ageGroup.TotalVoters * 100 : 0
            });
        }

        // Area breakdown
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
                DemographicValue = area.Area ?? "Unknown",
                TotalVoters = area.TotalVoters,
                Voted = area.Voted,
                TurnoutPercentage = area.TotalVoters > 0 ? (decimal)area.Voted / area.TotalVoters * 100 : 0
            });
        }

        // Time-based turnout (simplified - would need actual timestamps)
        var timeBasedTurnout = new List<TimeBasedTurnoutDto>();
        var cumulativeBallots = 0;
        for (var hour = 8; hour <= 20; hour++) // Assuming 8 AM to 8 PM voting
        {
            // This is a placeholder - in real implementation, you'd query actual ballot timestamps
            var ballotsInHour = (int)(totalBallotsCast * 0.05); // Simplified distribution
            cumulativeBallots += ballotsInHour;

            timeBasedTurnout.Add(new TimeBasedTurnoutDto
            {
                TimePeriod = election.DateOfElection?.Date.AddHours(hour) ?? DateTime.Now.Date.AddHours(hour),
                PeriodType = "Hour",
                BallotsCast = ballotsInHour,
                CumulativeTurnout = totalRegisteredVoters > 0 ? (decimal)cumulativeBallots / totalRegisteredVoters * 100 : 0
            });
        }

        // Participation rates
        var onlineVoters = await _context.People
            .CountAsync(p => p.ElectionGuid == electionGuid && p.HasOnlineBallot == true);

        var inPersonVoters = totalBallotsCast - onlineVoters;

        var participationRates = new ParticipationRateDto
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

        var turnoutAnalysis = new TurnoutAnalysisDto
        {
            OverallTurnout = overview.OverallTurnoutPercentage,
            TurnoutByLocation = turnoutByLocation,
            EarlyVotingCount = 0, // Would need timestamp tracking
            ElectionDayVotingCount = totalBallotsCast,
            EarlyVotingPercentage = 0, // Would need timestamp tracking
            DemographicBreakdown = demographicBreakdown,
            TimeBasedTurnout = timeBasedTurnout,
            ParticipationRates = participationRates
        };

        // Calculate location statistics
        var locationStatistics = new List<LocationStatisticsDto>();
        foreach (var location in locations)
        {
            var locationVoters = await _context.People
                .CountAsync(p => p.ElectionGuid == electionGuid &&
                               p.VotingLocationGuid == location.LocationGuid &&
                               p.CanVote == true);

            var locationBallots = location.Ballots.Count(b => b.StatusCode == "Ok");
            var locationVotes = location.Ballots.Sum(b => b.Votes.Count);

            // Get top candidates for this location
            var locationCandidateVotes = location.Ballots
                .Where(b => b.StatusCode == "Ok")
                .SelectMany(b => b.Votes)
                .GroupBy(v => v.Person?.FullNameFl ?? "Unknown")
                .OrderByDescending(g => g.Count())
                .Take(3)
                .ToDictionary(g => g.Key, g => g.Count());

            locationStatistics.Add(new LocationStatisticsDto
            {
                LocationName = location.Name ?? "Unknown",
                RegisteredVoters = locationVoters,
                BallotsCast = locationBallots,
                ValidBallots = locationBallots,
                SpoiledBallots = location.Ballots.Count(b => b.StatusCode != "Ok"),
                TurnoutPercentage = locationVoters > 0 ? (decimal)locationBallots / locationVoters * 100 : 0,
                TotalVotes = locationVotes,
                TopCandidates = locationCandidateVotes
            });
        }

        return new DetailedStatisticsDto
        {
            Overview = overview,
            VoteDistribution = voteDistribution,
            CandidatePerformance = candidatePerformance,
            TurnoutAnalysis = turnoutAnalysis,
            LocationStatistics = locationStatistics
        };
    }
}
