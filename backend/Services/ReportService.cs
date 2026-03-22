using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.DTOs.Reports;
using Microsoft.EntityFrameworkCore;

using LocationTypeEnum = Backend.Domain.Enumerations.LocationType;

namespace Backend.Services;

public class ReportService : IReportService
{
    private readonly MainDbContext _context;

    private static readonly Dictionary<string, string> VotingMethodNames = new()
    {
        ["P"] = "In Person",
        ["M"] = "Mailed In",
        ["D"] = "Dropped Off",
        ["C"] = "Called In",
        ["O"] = "Online",
        ["K"] = "Kiosk",
        ["I"] = "Imported",
        ["1"] = "Custom 1",
        ["2"] = "Custom 2",
        ["3"] = "Custom 3"
    };

    public ReportService(MainDbContext context)
    {
        _context = context;
    }

    private async Task<Election> GetElectionAsync(Guid electionGuid)
    {
        return await _context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid)
               ?? throw new ArgumentException($"Election {electionGuid} not found");
    }

    private static string GetVotingMethodText(string? code)
    {
        if (string.IsNullOrEmpty(code)) return "-";
        return VotingMethodNames.TryGetValue(code, out var name) ? name : code;
    }

    private bool IsSingleNameElection(Election election)
    {
        return election.ElectionType is "Con" or "NSA";
    }

    private static string? GetIneligibleDescription(string? code)
    {
        if (string.IsNullOrEmpty(code)) return null;
        var reason = IneligibleReasonEnum.GetByCode(code);
        return reason?.Description;
    }

    private static string? GetIneligibleDescriptionByGuid(Guid? guid)
    {
        if (guid == null) return null;
        var reason = IneligibleReasonEnum.GetByGuid(guid);
        return reason?.Description;
    }

    private string? ParseCustomMethodName(string? customMethods, int index)
    {
        if (string.IsNullOrEmpty(customMethods)) return null;
        var parts = customMethods.Split('|');
        return index < parts.Length ? parts[index] : null;
    }

    public async Task<List<ReportListItemDto>> GetAvailableReportsAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();
        var hasMultipleLocations = locations.Count > 1;
        var onlineEnabled = election.OnlineWhenOpen.HasValue;
        var hasImported = election.VotingMethods?.Split(',').Contains("IM") == true;

        const string ballotReports = "Ballot Reports";
        const string voterReports = "Voter Reports";

        var reports = new List<ReportListItemDto>
        {
            new() { Code = "Main", Name = "Main Election Report", Category =  ballotReports },
            new() { Code = "VotesByNum", Name = "Tellers' Report, by Votes", Category = ballotReports },
            new() { Code = "VotesByName", Name = "Tellers' Report, by Name", Category = ballotReports },
            new() { Code = "Ballots", Name = "Ballots (All for Review)", Category = ballotReports },
        };

        if (onlineEnabled)
            reports.Add(new() { Code = "BallotsOnline", Name = "Ballots (Online Only)", Category = ballotReports });

        if (hasImported)
            reports.Add(new() { Code = "BallotsImported", Name = "Ballots (Imported Only)", Category = ballotReports });

        reports.AddRange(new[]
        {
            new ReportListItemDto { Code = "BallotsTied", Name = "Ballots (For Tied)", Category = ballotReports },
            new() { Code = "SpoiledVotes", Name = "Spoiled Votes", Category = ballotReports },
            new() { Code = "BallotAlignment", Name = "Ballot Alignment", Category = ballotReports },
            new() { Code = "BallotsSame", Name = "Duplicate Ballots", Category = ballotReports },
            new() { Code = "BallotsSummary", Name = "Ballots Summary", Category = ballotReports },
        });

        reports.Add(new() { Code = "AllCanReceive", Name = "Can Be Voted For", Category = voterReports });
        reports.Add(new() { Code = "Voters", Name = "Participation", Category = voterReports });
        reports.Add(new() { Code = "Flags", Name = "Attendance Checklists", Category = voterReports });

        if (onlineEnabled)
            reports.Add(new() { Code = "VotersOnline", Name = "Voted Online", Category = voterReports });

        reports.Add(new() { Code = "VotersByArea", Name = "Eligible and Voted by Area", Category = voterReports });

        if (hasMultipleLocations)
        {
            reports.Add(new() { Code = "VotersByLocation", Name = "Voting Method by Venue", Category = voterReports });
            reports.Add(new() { Code = "VotersByLocationArea", Name = "Attendance by Venue", Category = voterReports });
        }

        reports.Add(new() { Code = "ChangedPeople", Name = "Updated People Records", Category = voterReports });
        reports.Add(new() { Code = "AllNonEligible", Name = "With Eligibility Status", Category = voterReports });

        if (onlineEnabled)
            reports.Add(new() { Code = "VoterEmails", Name = "Email & Phone List", Category = voterReports });

        return reports;
    }

    public async Task<MainReportDto> GetMainReportAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var summary = await _context.ResultSummaries
            .Where(rs => rs.ElectionGuid == electionGuid && rs.ResultType == "F")
            .FirstOrDefaultAsync();

        var numToElect = election.NumberToElect ?? 0;
        var numExtra = election.NumberExtra ?? 0;

        var results = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid)
            .OrderBy(r => r.Rank)
            .Take(numToElect + numExtra)
            .ToListAsync();

        var elected = results.Select(r => new ElectedPersonDto
        {
            Rank = r.Section == "X"
                ? "Next " + r.RankInExtra
                : r.Rank.ToString(),
            Name = r.Person?.FullName ?? "",
            BahaiId = r.Person?.BahaiId,
            VoteCountDisplay = (r.VoteCount ?? 0).ToString("N0") +
                               (r.TieBreakRequired == true ? " / " + r.TieBreakCount : ""),
            Section = r.Section
        }).ToList();

        var ballots = await _context.Ballots
            .Include(b => b.Location)
            .Where(b => b.Location.ElectionGuid == electionGuid && b.StatusCode != BallotStatus.Ok)
            .ToListAsync();

        var spoiledBallotReasons = ballots
            .GroupBy(b => b.StatusCode)
            .Select(g => new SpoiledBallotGroupDto
            {
                Reason = BallotStatusEnum.GetDescription(g.Key) ?? g.Key.ToString(),
                BallotCount = g.Count()
            })
            .OrderByDescending(x => x.BallotCount)
            .ToList();

        if (summary?.SpoiledManualBallots > 0)
        {
            spoiledBallotReasons.Add(new SpoiledBallotGroupDto
            {
                Reason = "Unknown (Manual Count)",
                BallotCount = summary.SpoiledManualBallots.Value
            });
        }

        var votes = await _context.Votes
            .Include(v => v.Person)
            .Where(v => _context.Ballots.Any(b =>
                b.BallotGuid == v.BallotGuid &&
                b.StatusCode == BallotStatus.Ok &&
                b.Location.ElectionGuid == electionGuid))
            .ToListAsync();
        var spoiledVoteReasons = votes
            .Where(v =>
            {
                if (v.IneligibleReasonCode != null) return true;
                if (v.Person != null && v.Person.CanReceiveVotes != true && v.Person.IneligibleReasonGuid != null) return true;
                return false;
            })
            .GroupBy(v =>
            {
                if (v.IneligibleReasonCode != null) return v.IneligibleReasonCode;
                return v.Person?.IneligibleReasonGuid?.ToString() ?? "";
            })
            .Select(g =>
            {
                var desc = GetIneligibleDescription(g.First().IneligibleReasonCode)
                           ?? GetIneligibleDescriptionByGuid(g.First().Person?.IneligibleReasonGuid);
                return new SpoiledVoteGroupDto
                {
                    Reason = desc ?? "Unknown",
                    VoteCount = g.Count()
                };
            })
            .Where(x => !string.IsNullOrEmpty(x.Reason) && x.Reason != "Unknown")
            .OrderByDescending(x => x.VoteCount)
            .ThenBy(x => x.Reason)
            .ToList();

        var numEligible = summary?.NumEligibleToVote ?? 0;
        var numVoted = summary?.NumVoters ?? 0;
        var totalBallots = summary?.BallotsReceived ?? 0;
        var participation = numEligible > 0 ? (double)numVoted / numEligible * 100 : 0;

        return new MainReportDto
        {
            ElectionName = election.Name,
            Convenor = election.Convenor,
            DateOfElection = election.DateOfElection,
            NumEligibleToVote = numEligible,
            SumOfEnvelopesCollected = numVoted,
            NumBallotsWithManual = totalBallots,
            PercentParticipation = participation,
            InPersonBallots = summary?.InPersonBallots ?? 0,
            MailedInBallots = summary?.MailedInBallots ?? 0,
            DroppedOffBallots = summary?.DroppedOffBallots ?? 0,
            OnlineBallots = summary?.OnlineBallots ?? 0,
            ImportedBallots = summary?.ImportedBallots ?? 0,
            CalledInBallots = summary?.CalledInBallots ?? 0,
            Custom1Ballots = summary?.Custom1Ballots ?? 0,
            Custom2Ballots = summary?.Custom2Ballots ?? 0,
            Custom3Ballots = summary?.Custom3Ballots ?? 0,
            Custom1Name = ParseCustomMethodName(election.CustomMethods, 0),
            Custom2Name = ParseCustomMethodName(election.CustomMethods, 1),
            Custom3Name = ParseCustomMethodName(election.CustomMethods, 2),
            SpoiledBallots = summary?.SpoiledBallots ?? 0,
            SpoiledVotes = summary?.SpoiledVotes ?? 0,
            SpoiledBallotReasons = spoiledBallotReasons,
            SpoiledVoteReasons = spoiledVoteReasons,
            Elected = elected,
            HasTies = elected.Any(e => e.VoteCountDisplay.Contains('/'))
        };
    }

    public async Task<VotesByNumDto> GetVotesByNumAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var results = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid)
            .OrderBy(r => r.Rank)
            .ThenBy(r => r.Person!.FullNameFl)
            .ToListAsync();

        var people = new List<VotePersonDto>();
        for (var i = 0; i < results.Count; i++)
        {
            var r = results[i];
            people.Add(new VotePersonDto
            {
                PersonName = r.Person?.FullNameFl ?? "",
                VoteCount = r.VoteCount ?? 0,
                TieBreakCount = r.TieBreakCount,
                TieBreakRequired = r.TieBreakRequired == true,
                Section = r.Section,
                ShowBreak = i == 0 || r.Section != results[i - 1].Section
            });
        }

        return new VotesByNumDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = people
        };
    }

    public async Task<VotesByNameDto> GetVotesByNameAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var results = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid)
            .OrderBy(r => r.Person!.FullName)
            .ToListAsync();

        var people = results.Select(r => new VotePersonDto
        {
            PersonName = r.Person?.FullName ?? "",
            VoteCount = r.VoteCount ?? 0,
            TieBreakCount = r.TieBreakCount,
            TieBreakRequired = r.TieBreakRequired == true,
            Section = r.Section
        }).ToList();

        return new VotesByNameDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = people
        };
    }

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
                    PersonName = v.Person?.FullNameFl ?? "",
                    SingleNameElectionCount = v.SingleNameElectionCount,
                    OnlineVoteRaw = v.OnlineVoteRaw,
                    Spoiled = v.VoteStatus != VoteStatus.Ok,
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

        var validBallotGuids = await _context.Ballots
            .Include(b => b.Location)
            .Where(b => b.Location.ElectionGuid == electionGuid && b.StatusCode == BallotStatus.Ok)
            .Select(b => b.BallotGuid)
            .ToListAsync();

        var votes = await _context.Votes
            .Include(v => v.Person)
            .Where(v => validBallotGuids.Contains(v.BallotGuid))
            .Where(v => v.IneligibleReasonCode != null || (v.Person != null && v.Person.IneligibleReasonGuid != null))
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

    public async Task<AllCanReceiveReportDto> GetAllCanReceiveAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanReceiveVotes == true)
            .OrderBy(p => p.FullName)
            .Select(p => p.FullName ?? "")
            .ToListAsync();

        return new AllCanReceiveReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = people
        };
    }

    public async Task<VotersReportDto> GetVotersAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();
        var hasMultipleLocations = locations.Count > 1;
        var locationMap = locations.ToDictionary(l => l.LocationGuid, l => l.Name);

        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true)
            .OrderBy(p => p.FullName)
            .ToListAsync();

        return new VotersReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            HasMultipleLocations = hasMultipleLocations,
            TotalCount = people.Count,
            People = people.Select(p => new VoterItemDto
            {
                PersonName = p.FullName ?? "",
                VotingMethod = GetVotingMethodText(p.VotingMethod),
                BahaiId = p.BahaiId,
                Location = p.VotingLocationGuid.HasValue && locationMap.TryGetValue(p.VotingLocationGuid.Value, out var name) ? name : null,
                RegistrationTime = p.RegistrationTime,
                Teller1 = p.Teller1,
                Teller2 = p.Teller2,
                RegistrationLog = p.RegistrationHistory
            }).ToList()
        };
    }

    public async Task<FlagsReportDto> GetFlagsReportAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();
        var hasMultipleLocations = locations.Count > 1;
        var locationMap = locations.ToDictionary(l => l.LocationGuid, l => l.Name);

        var flagNames = string.IsNullOrEmpty(election.Flags)
            ? new List<string>()
            : election.Flags.Split('|').Where(f => !string.IsNullOrEmpty(f)).ToList();

        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid)
            .OrderBy(p => p.FullName)
            .ToListAsync();

        return new FlagsReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            HasMultipleLocations = hasMultipleLocations,
            FlagNames = flagNames,
            People = people.Select(p => new FlagPersonDto
            {
                RowId = p.RowId,
                PersonName = p.FullName ?? "",
                Location = p.VotingLocationGuid.HasValue && locationMap.TryGetValue(p.VotingLocationGuid.Value, out var name) ? name : null,
                Flags = string.IsNullOrEmpty(p.Flags) ? new List<string>() : p.Flags.Split('|').Where(f => !string.IsNullOrEmpty(f)).ToList()
            }).ToList()
        };
    }

    public async Task<VotersOnlineReportDto> GetVotersOnlineAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);

        var onlineInfos = await _context.OnlineVotingInfos
            .Where(ovi => ovi.ElectionGuid == electionGuid)
            .Join(_context.People.Where(p => p.ElectionGuid == electionGuid),
                ovi => ovi.PersonGuid, p => p.PersonGuid, (ovi, p) => new { ovi, p })
            .OrderByDescending(j => j.ovi.WhenStatus)
            .ThenBy(j => j.p.FullName)
            .ToListAsync();

        return new VotersOnlineReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = onlineInfos.Select(j => new OnlineVoterItemDto
            {
                PersonId = j.p.RowId,
                FullName = j.p.FullName ?? "",
                VotingMethodDisplay = GetVotingMethodText(j.p.VotingMethod),
                Status = j.ovi.Status,
                WhenStatus = j.ovi.WhenStatus,
                Email = j.p.Email,
                Phone = j.p.Phone
            }).ToList()
        };
    }

    public async Task<VotersByAreaReportDto> GetVotersByAreaAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true)
            .ToListAsync();

        var areas = people
            .GroupBy(p => p.Area ?? "(unknown)")
            .OrderBy(g => g.Key)
            .Select(g => BuildAreaRow(g.Key, g.ToList()))
            .ToList();

        return new VotersByAreaReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            Custom1Name = ParseCustomMethodName(election.CustomMethods, 0),
            Custom2Name = ParseCustomMethodName(election.CustomMethods, 1),
            Custom3Name = ParseCustomMethodName(election.CustomMethods, 2),
            Areas = areas,
            Total = BuildAreaRow("Total", people)
        };
    }

    private static AreaRowDto BuildAreaRow(string name, List<Person> people)
    {
        return new AreaRowDto
        {
            AreaName = name,
            TotalEligible = people.Count,
            Voted = people.Count(p => !string.IsNullOrEmpty(p.VotingMethod)),
            InPerson = people.Count(p => p.VotingMethod == "P"),
            MailedIn = people.Count(p => p.VotingMethod == "M"),
            DroppedOff = people.Count(p => p.VotingMethod == "D"),
            CalledIn = people.Count(p => p.VotingMethod == "C"),
            Custom1 = people.Count(p => p.VotingMethod == "1"),
            Custom2 = people.Count(p => p.VotingMethod == "2"),
            Custom3 = people.Count(p => p.VotingMethod == "3"),
            Online = people.Count(p => p.VotingMethod == "O"),
            OnlineKiosk = people.Count(p => p.VotingMethod == "K"),
            Imported = people.Count(p => p.VotingMethod == "I")
        };
    }

    public async Task<VotersByLocationReportDto> GetVotersByLocationAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true)
            .ToListAsync();
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();

        var locationRows = locations
            .GroupJoin(people, l => l.LocationGuid, p => p.VotingLocationGuid ?? Guid.Empty,
                (l, pList) => BuildLocationRow(l.Name, pList.ToList()))
            .OrderBy(r => r.LocationName)
            .ToList();

        var totalRow = BuildLocationRow("Total", people);

        return new VotersByLocationReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            Custom1Name = ParseCustomMethodName(election.CustomMethods, 0),
            Custom2Name = ParseCustomMethodName(election.CustomMethods, 1),
            Custom3Name = ParseCustomMethodName(election.CustomMethods, 2),
            Locations = locationRows,
            Total = totalRow
        };
    }

    private static LocationRowDto BuildLocationRow(string name, List<Person> people)
    {
        return new LocationRowDto
        {
            LocationName = name,
            TotalVoters = people.Count,
            InPerson = people.Count(p => p.VotingMethod == "P"),
            MailedIn = people.Count(p => p.VotingMethod == "M"),
            DroppedOff = people.Count(p => p.VotingMethod == "D"),
            CalledIn = people.Count(p => p.VotingMethod == "C"),
            Custom1 = people.Count(p => p.VotingMethod == "1"),
            Custom2 = people.Count(p => p.VotingMethod == "2"),
            Custom3 = people.Count(p => p.VotingMethod == "3"),
            Online = people.Count(p => p.VotingMethod == "O"),
            OnlineKiosk = people.Count(p => p.VotingMethod == "K"),
            Imported = people.Count(p => p.VotingMethod == "I")
        };
    }

    public async Task<VotersByLocationAreaReportDto> GetVotersByLocationAreaAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true && p.VotingMethod == "P")
            .ToListAsync();
        var locations = await _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();

        var locationGroups = people
            .Where(p => p.VotingLocationGuid.HasValue)
            .Join(locations, p => p.VotingLocationGuid, l => l.LocationGuid, (p, l) => new { l, p })
            .GroupBy(x => x.l.Name)
            .OrderBy(g => g.Key)
            .Select(g => new LocationAreaGroupDto
            {
                LocationName = g.Key,
                TotalCount = g.Count(),
                Areas = g.GroupBy(x => x.p.Area ?? "(unknown)")
                    .OrderBy(a => a.Key)
                    .Select(a => new AreaCountDto { AreaName = a.Key, Count = a.Count() })
                    .ToList()
            })
            .ToList();

        return new VotersByLocationAreaReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            Locations = locationGroups
        };
    }

    public async Task<ChangedPeopleReportDto> GetChangedPeopleAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CombinedInfo != p.CombinedInfoAtStart)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return new ChangedPeopleReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = people.Select(p => new ChangedPersonDto
            {
                Change = string.IsNullOrEmpty(p.CombinedInfoAtStart) ? "New" : "Changed",
                FirstName = p.FirstName,
                LastName = p.LastName,
                OtherNames = p.OtherNames,
                OtherLastNames = p.OtherLastNames,
                OtherInfo = p.OtherInfo,
                BahaiId = p.BahaiId,
                CanVote = p.CanVote == true,
                CanReceiveVotes = p.CanReceiveVotes == true,
                InvalidReasonDesc = GetIneligibleDescriptionByGuid(p.IneligibleReasonGuid)
            }).ToList()
        };
    }

    public async Task<AllNonEligibleReportDto> GetAllNonEligibleAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && (p.CanVote != true || p.CanReceiveVotes != true))
            .OrderBy(p => p.FullName)
            .ToListAsync();

        return new AllNonEligibleReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = people.Select(p => new NonEligiblePersonDto
            {
                PersonName = p.FullName ?? "",
                CanReceiveVotes = p.CanReceiveVotes == true,
                CanVote = p.CanVote == true,
                InvalidReasonDesc = GetIneligibleDescriptionByGuid(p.IneligibleReasonGuid),
                VotingMethod = GetVotingMethodText(p.VotingMethod)
            }).ToList()
        };
    }

    public async Task<VoterEmailsReportDto> GetVoterEmailsAsync(Guid electionGuid)
    {
        var election = await GetElectionAsync(electionGuid);
        var people = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && (p.Email != null || p.Phone != null))
            .OrderBy(p => p.FullName)
            .ToListAsync();

        var onlineVoters = await _context.OnlineVoters.ToListAsync(); // across all elections
        var emailVoterIds = onlineVoters.Where(ov => ov.VoterIdType == "E").Select(ov => ov.VoterId).ToHashSet();
        var phoneVoterIds = onlineVoters.Where(ov => ov.VoterIdType == "P").Select(ov => ov.VoterId).ToHashSet();

        return new VoterEmailsReportDto
        {
            ElectionName = election.Name,
            DateOfElection = election.DateOfElection,
            People = people.Select(p => new VoterEmailItemDto
            {
                FullName = p.FullName ?? "",
                BahaiId = p.BahaiId,
                Email = p.Email,
                Phone = p.Phone,
                CanVote = p.CanVote == true,
                HasSignedInEmail = !string.IsNullOrEmpty(p.Email) && emailVoterIds.Contains(p.Email),
                HasSignedInPhone = !string.IsNullOrEmpty(p.Phone) && phoneVoterIds.Contains(p.Phone),
                VotingMethod = GetVotingMethodText(p.VotingMethod)
            }).ToList()
        };
    }
}
