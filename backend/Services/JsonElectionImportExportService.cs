using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.Domain.Context;
using Backend.DTOs.Import;
using Backend.DTOs.Elections;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using System.Xml;
using System.Xml.Schema;

namespace Backend.Services;

public class JsonElectionImportExportService : ElectionImportExportBase
{
    public JsonElectionImportExportService(MainDbContext context, IElectionService electionService)
        : base(context, electionService)
    {
    }

    // Job 3: Export election to new JSON format
    public async Task<string> ExportElectionToJsonAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .Include(e => e.Locations)
            .Include(e => e.People)
            .Include(e => e.Tellers)
            .Include(e => e.ResultSummaries)
            .Include(e => e.ResultTies)
            .Include(e => e.Results)
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null) throw new ArgumentException("Election not found");

        var ballots = await _context.Ballots
            .Where(b => election.Locations.Select(l => l.LocationGuid).Contains(b.LocationGuid))
            .Include(b => b.Votes)
            .ToListAsync();

        var onlineVotingInfos = await _context.OnlineVotingInfos
            .Where(ovi => ovi.ElectionGuid == electionGuid)
            .ToListAsync();

        var logs = await _context.Logs
            .Where(l => l.ElectionGuid == electionGuid)
            .ToListAsync();

        var exportData = new
        {
            format = "TallyJ4",
            version = "1.0",
            exportedAt = DateTimeOffset.UtcNow.ToString("o"),
            election = new
            {
                election.ElectionGuid,
                election.Name,
                election.Convenor,
                DateOfElection = election.DateOfElection?.ToString("o"),
                election.ElectionType,
                election.ElectionMode,
                election.NumberToElect,
                election.NumberExtra,
                election.ElectionPasscode,
                election.LastEnvNum,
                election.ListForPublic,
                election.ShowFullReport,
                OnlineWhenOpen = election.OnlineWhenOpen?.ToString("o"),
                OnlineWhenClose = election.OnlineWhenClose?.ToString("o"),
                election.OnlineCloseIsEstimate,
                election.OnlineSelectionProcess,
                election.EmailFromAddress,
                election.EmailFromName,
                election.EmailText,
                election.SmsText,
                election.EmailSubject,
                election.CustomMethods,
                election.VotingMethods,
                election.Flags
            },
            locations = election.Locations.Select(l => new
            {
                l.LocationGuid,
                l.Name,
                l.ContactInfo,
                l.Long,
                l.Lat,
                l.TallyStatus,
                l.SortOrder,
                l.BallotsCollected,
                l.LocationTypeCode
            }),
            people = election.People.Select(p => new
            {
                p.PersonGuid,
                p.LastName,
                p.FirstName,
                p.OtherLastNames,
                p.OtherNames,
                p.OtherInfo,
                p.Area,
                p.BahaiId,
                p.CombinedInfo,
                p.CombinedSoundCodes,
                p.CombinedInfoAtStart,
                p.AgeGroup,
                p.CanVote,
                p.CanReceiveVotes,
                p.IneligibleReasonGuid,
                RegistrationTime = p.RegistrationTime?.ToString("o"),
                p.VotingLocationGuid,
                p.VotingMethod,
                p.EnvNum,
                p.Teller1,
                p.Teller2,
                p.Email,
                p.Phone,
                p.HasOnlineBallot,
                p.Flags,
                p.UnitName,
                p.KioskCode,
                p.RegistrationHistory
            }),
            ballots = ballots.Select(b => new
            {
                b.BallotGuid,
                b.LocationGuid,
                StatusCode = b.StatusCode.ToString(),
                b.ComputerCode,
                b.BallotNumAtComputer,
                b.Teller1,
                b.Teller2,
                votes = b.Votes.Select(v => new
                {
                    v.PositionOnBallot,
                    v.PersonGuid,
                    VoteStatus = v.VoteStatus.ToString(),
                    v.IneligibleReasonCode,
                    v.SingleNameElectionCount,
                    v.PersonCombinedInfo,
                    v.OnlineVoteRaw
                })
            }),
            tellers = election.Tellers.Select(t => new
            {
                t.Name,
                t.IsHeadTeller,
                t.UsingComputerCode
            }),
            results = election.Results.Select(r => new
            {
                r.PersonGuid,
                r.VoteCount,
                r.Rank,
                r.Section,
                r.IsTied,
                r.IsTieResolved,
                r.TieBreakGroup,
                r.CloseToPrev,
                r.CloseToNext,
                r.RankInExtra,
                r.TieBreakRequired,
                r.TieBreakCount,
                r.ForceShowInOther
            }),
            resultSummaries = election.ResultSummaries.Select(rs => new
            {
                rs.ResultType,
                rs.NumVoters,
                rs.NumEligibleToVote,
                rs.BallotsReceived,
                rs.InPersonBallots,
                rs.DroppedOffBallots,
                rs.MailedInBallots,
                rs.CalledInBallots,
                rs.OnlineBallots,
                rs.ImportedBallots,
                rs.Custom1Ballots,
                rs.Custom2Ballots,
                rs.Custom3Ballots,
                rs.BallotsNeedingReview,
                rs.SpoiledBallots,
                rs.SpoiledVotes,
                rs.TotalVotes,
                rs.UseOnReports,
                rs.SpoiledManualBallots
            }),
            resultTies = election.ResultTies.Select(rt => new
            {
                rt.TieBreakGroup,
                rt.NumInTie,
                rt.IsResolved,
                rt.TieBreakRequired,
                rt.NumToElect
            }),
            onlineVotingInfos = onlineVotingInfos.Select(ovi => new
            {
                ovi.PersonGuid,
                WhenBallotCreated = ovi.WhenBallotCreated?.ToString("o"),
                ovi.Status,
                WhenStatus = ovi.WhenStatus?.ToString("o"),
                ovi.ListPool,
                ovi.PoolLocked,
                ovi.HistoryStatus,
                ovi.NotifiedAboutOpening
            }),
            logs = logs.Select(l => new
            {
                AsOf = l.AsOf.ToString("o"),
                l.LocationGuid,
                l.VoterId,
                l.ComputerCode,
                l.Details
            })
        };

        return JsonSerializer.Serialize(exportData, new JsonSerializerOptions { WriteIndented = true });
    }

    // Job 3: Import from new JSON format
    public async Task<ElectionDto> ImportElectionFromJsonAsync(Stream jsonStream, Guid? userId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            using var reader = new StreamReader(jsonStream, System.Text.Encoding.UTF8, true, 1024, leaveOpen: true);
            var jsonContent = await reader.ReadToEndAsync();

            var importData = JsonSerializer.Deserialize<JsonImportData>(jsonContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (importData?.format != "TallyJ4")
                throw new InvalidOperationException("Invalid format - expected TallyJ4");

            // Create new election with remapped GUIDs
            var guidMap = new Dictionary<Guid, Guid>();
            var newElectionGuid = Guid.NewGuid();

            var election = new Election
            {
                ElectionGuid = newElectionGuid,
                Name = importData.election.Name ?? "Imported Election",
                Convenor = importData.election.Convenor,
                DateOfElection = ParseDateTime(importData.election.DateOfElection),
                ElectionType = importData.election.ElectionType,
                ElectionMode = importData.election.ElectionMode,
                NumberToElect = importData.election.NumberToElect,
                NumberExtra = importData.election.NumberExtra,
                ElectionPasscode = importData.election.ElectionPasscode,
                LastEnvNum = importData.election.LastEnvNum,
                ListForPublic = importData.election.ListForPublic,
                ShowFullReport = importData.election.ShowFullReport,
                OnlineWhenOpen = ParseDateTime(importData.election.OnlineWhenOpen),
                OnlineWhenClose = ParseDateTime(importData.election.OnlineWhenClose),
                OnlineCloseIsEstimate = importData.election.OnlineCloseIsEstimate ?? true,
                OnlineSelectionProcess = importData.election.OnlineSelectionProcess,
                EmailFromAddress = importData.election.EmailFromAddress,
                EmailFromName = importData.election.EmailFromName,
                EmailText = importData.election.EmailText,
                SmsText = importData.election.SmsText,
                EmailSubject = importData.election.EmailSubject,
                CustomMethods = importData.election.CustomMethods,
                VotingMethods = importData.election.VotingMethods,
                Flags = importData.election.Flags,
                RowVersion = new byte[8]
            };

            _context.Elections.Add(election);

            ImportLocations(importData, newElectionGuid, guidMap);
            ImportPeople(importData, newElectionGuid, guidMap);
            ImportBallotsAndVotes(importData, guidMap);
            ImportTellers(importData, newElectionGuid);
            ImportResults(importData, newElectionGuid, guidMap);
            ImportResultSummaries(importData, newElectionGuid);
            ImportResultTies(importData, newElectionGuid);
            ImportOnlineVotingInfos(importData, newElectionGuid, guidMap);
            ImportLogs(importData, newElectionGuid, guidMap);

            await _context.SaveChangesAsync();

            if (userId.HasValue)
            {
                await AssociateUserWithElectionAsync(election.ElectionGuid, userId.Value);
            }

            await transaction.CommitAsync();

            return await _electionService.GetElectionByGuidAsync(election.ElectionGuid) ?? throw new InvalidOperationException("Failed to create election");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException($"Import failed: {ex.Message}", ex);
        }
    }

    private void ImportLocations(JsonImportData importData, Guid electionGuid, Dictionary<Guid, Guid> guidMap)
    {
        foreach (var loc in importData.locations)
        {
            var oldGuid = loc.LocationGuid;
            var newGuid = Guid.NewGuid();
            guidMap[oldGuid] = newGuid;

            var location = new Location
            {
                ElectionGuid = electionGuid,
                LocationGuid = newGuid,
                Name = loc.Name ?? "",
                ContactInfo = loc.ContactInfo,
                Long = loc.Long,
                Lat = loc.Lat,
                TallyStatus = loc.TallyStatus,
                SortOrder = loc.SortOrder,
                BallotsCollected = loc.BallotsCollected,
                LocationTypeCode = loc.LocationTypeCode ?? LocationType.Manual.ToString()
            };
            _context.Locations.Add(location);
        }
    }

    private void ImportPeople(JsonImportData importData, Guid electionGuid, Dictionary<Guid, Guid> guidMap)
    {
        foreach (var person in importData.people)
        {
            var oldGuid = person.PersonGuid;
            var newGuid = Guid.NewGuid();
            guidMap[oldGuid] = newGuid;

            var p = new Person
            {
                ElectionGuid = electionGuid,
                PersonGuid = newGuid,
                LastName = person.LastName ?? "",
                FirstName = person.FirstName,
                OtherLastNames = person.OtherLastNames,
                OtherNames = person.OtherNames,
                OtherInfo = person.OtherInfo,
                Area = person.Area,
                BahaiId = person.BahaiId,
                CombinedInfo = person.CombinedInfo,
                CombinedSoundCodes = person.CombinedSoundCodes,
                CombinedInfoAtStart = person.CombinedInfoAtStart,
                AgeGroup = person.AgeGroup,
                CanVote = person.CanVote,
                CanReceiveVotes = person.CanReceiveVotes,
                IneligibleReasonGuid = person.IneligibleReasonGuid,
                RegistrationTime = ParseDateTime(person.RegistrationTime),
                VotingLocationGuid = person.VotingLocationGuid.HasValue && guidMap.ContainsKey(person.VotingLocationGuid.Value)
                    ? guidMap[person.VotingLocationGuid.Value]
                    : null,
                VotingMethod = person.VotingMethod,
                EnvNum = person.EnvNum,
                Teller1 = person.Teller1,
                Teller2 = person.Teller2,
                Email = person.Email,
                Phone = person.Phone,
                HasOnlineBallot = person.HasOnlineBallot,
                Flags = person.Flags,
                UnitName = person.UnitName,
                KioskCode = person.KioskCode,
                RegistrationHistory = person.RegistrationHistory,
                RowVersion = new byte[8]
            };
            _context.People.Add(p);
        }
    }

    private void ImportBallotsAndVotes(JsonImportData importData, Dictionary<Guid, Guid> guidMap)
    {
        foreach (var ballot in importData.ballots)
        {
            var now = DateTimeOffset.UtcNow;
            var b = new Ballot
            {
                BallotGuid = Guid.NewGuid(),
                LocationGuid = guidMap[ballot.LocationGuid],
                StatusCode = Enum.Parse<BallotStatus>(ballot.StatusCode ?? "Ok"),
                ComputerCode = ballot.ComputerCode ?? "??",
                BallotNumAtComputer = ballot.BallotNumAtComputer,
                Teller1 = ballot.Teller1,
                Teller2 = ballot.Teller2,
                DateCreated = now,
                DateUpdated = now,
                RowVersion = new byte[8]
            };
            _context.Ballots.Add(b);

            foreach (var vote in ballot.votes)
            {
                var v = new Vote
                {
                    BallotGuid = b.BallotGuid,
                    PositionOnBallot = vote.PositionOnBallot,
                    PersonGuid = vote.PersonGuid.HasValue && guidMap.ContainsKey(vote.PersonGuid.Value)
                        ? guidMap[vote.PersonGuid.Value]
                        : null,
                    VoteStatus = Enum.Parse<VoteStatus>(vote.VoteStatus ?? "Ok"),
                    IneligibleReasonCode = vote.IneligibleReasonCode,
                    SingleNameElectionCount = vote.SingleNameElectionCount,
                    PersonCombinedInfo = vote.PersonCombinedInfo,
                    OnlineVoteRaw = vote.OnlineVoteRaw,
                    RowVersion = new byte[8]
                };
                _context.Votes.Add(v);
            }
        }
    }

    private void ImportTellers(JsonImportData importData, Guid electionGuid)
    {
        foreach (var teller in importData.tellers)
        {
            var t = new Teller
            {
                ElectionGuid = electionGuid,
                Name = teller.Name ?? "",
                IsHeadTeller = teller.IsHeadTeller,
                UsingComputerCode = teller.UsingComputerCode,
                RowVersion = new byte[8]
            };
            _context.Tellers.Add(t);
        }
    }

    private void ImportResults(JsonImportData importData, Guid electionGuid, Dictionary<Guid, Guid> guidMap)
    {
        foreach (var result in importData.results)
        {
            var r = new Result
            {
                ElectionGuid = electionGuid,
                PersonGuid = guidMap[result.PersonGuid],
                VoteCount = result.VoteCount,
                Rank = result.Rank,
                Section = result.Section ?? "T",
                IsTied = result.IsTied,
                IsTieResolved = result.IsTieResolved,
                TieBreakGroup = result.TieBreakGroup,
                CloseToPrev = result.CloseToPrev,
                CloseToNext = result.CloseToNext,
                RankInExtra = result.RankInExtra,
                TieBreakRequired = result.TieBreakRequired,
                TieBreakCount = result.TieBreakCount,
                ForceShowInOther = result.ForceShowInOther
            };
            _context.Results.Add(r);
        }
    }

    private void ImportResultSummaries(JsonImportData importData, Guid electionGuid)
    {
        foreach (var rs in importData.resultSummaries)
        {
            var resultSummary = new ResultSummary
            {
                ElectionGuid = electionGuid,
                ResultType = rs.ResultType ?? "M",
                NumVoters = rs.NumVoters,
                NumEligibleToVote = rs.NumEligibleToVote,
                BallotsReceived = rs.BallotsReceived,
                InPersonBallots = rs.InPersonBallots,
                DroppedOffBallots = rs.DroppedOffBallots,
                MailedInBallots = rs.MailedInBallots,
                CalledInBallots = rs.CalledInBallots,
                OnlineBallots = rs.OnlineBallots,
                ImportedBallots = rs.ImportedBallots,
                Custom1Ballots = rs.Custom1Ballots,
                Custom2Ballots = rs.Custom2Ballots,
                Custom3Ballots = rs.Custom3Ballots,
                BallotsNeedingReview = rs.BallotsNeedingReview,
                SpoiledBallots = rs.SpoiledBallots,
                SpoiledVotes = rs.SpoiledVotes,
                TotalVotes = rs.TotalVotes,
                UseOnReports = rs.UseOnReports,
                SpoiledManualBallots = rs.SpoiledManualBallots
            };
            _context.ResultSummaries.Add(resultSummary);
        }
    }

    private void ImportResultTies(JsonImportData importData, Guid electionGuid)
    {
        foreach (var rt in importData.resultTies)
        {
            var resultTie = new ResultTie
            {
                ElectionGuid = electionGuid,
                TieBreakGroup = rt.TieBreakGroup,
                NumInTie = rt.NumInTie ?? 0,
                IsResolved = rt.IsResolved,
                TieBreakRequired = rt.TieBreakRequired,
                NumToElect = rt.NumToElect ?? 0
            };
            _context.ResultTies.Add(resultTie);
        }
    }

    private void ImportOnlineVotingInfos(JsonImportData importData, Guid electionGuid, Dictionary<Guid, Guid> guidMap)
    {
        foreach (var ovi in importData.onlineVotingInfos)
        {
            var onlineVotingInfo = new OnlineVotingInfo
            {
                ElectionGuid = electionGuid,
                PersonGuid = guidMap[ovi.PersonGuid],
                WhenBallotCreated = ParseDateTime(ovi.WhenBallotCreated),
                Status = ovi.Status ?? "",
                WhenStatus = ParseDateTime(ovi.WhenStatus),
                ListPool = ovi.ListPool,
                PoolLocked = ovi.PoolLocked,
                HistoryStatus = ovi.HistoryStatus,
                NotifiedAboutOpening = ovi.NotifiedAboutOpening
            };
            _context.OnlineVotingInfos.Add(onlineVotingInfo);
        }
    }

    private void ImportLogs(JsonImportData importData, Guid electionGuid, Dictionary<Guid, Guid> guidMap)
    {
        foreach (var log in importData.logs)
        {
            var l = new Log
            {
                ElectionGuid = electionGuid,
                LocationGuid = log.LocationGuid.HasValue && guidMap.ContainsKey(log.LocationGuid.Value)
                    ? guidMap[log.LocationGuid.Value]
                    : null,
                VoterId = log.VoterId,
                ComputerCode = log.ComputerCode,
                Details = log.Details,
                AsOf = ParseDateTime(log.AsOf) ?? DateTimeOffset.UtcNow
            };
            _context.Logs.Add(l);
        }
    }
}