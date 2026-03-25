using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.Domain.Context;
using Backend.DTOs.Import;
using Backend.DTOs.Elections;
using Backend.Models;
using Backend.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Linq;

namespace Backend.Services;

public class ElectionExportImportService
{
    private readonly MainDbContext _context;
    private readonly IElectionService _electionService;

    public ElectionExportImportService(MainDbContext context, IElectionService electionService)
    {
        _context = context;
        _electionService = electionService;
    }

    private async Task AssociateUserWithElectionAsync(Guid electionGuid, Guid userId)
    {
        var joinEntry = new JoinElectionUser
        {
            ElectionGuid = electionGuid,
            UserId = userId,
            Role = "Admin"
        };
        _context.JoinElectionUsers.Add(joinEntry);
        await _context.SaveChangesAsync();
    }

    // Job 1: Import from CdnBallotImport.xsd format
    public async Task<ImportResultDto> ImportCdnBallotsAsync(Guid electionGuid, Stream xmlStream)
    {
        var result = new ImportResultDto();
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas", "CdnBallotImport.xsd");
            var xmlDoc = new XmlDocument();

            using (var reader = new StreamReader(xmlStream))
            {
                var xmlContent = await reader.ReadToEndAsync();
                xmlDoc.LoadXml(xmlContent);
            }

            xmlDoc.Schemas.Add("", schemaPath);
            var validationErrors = new List<string>();
            xmlDoc.Validate((sender, args) =>
            {
                if (args.Severity == XmlSeverityType.Error)
                    validationErrors.Add(args.Message);
            });

            if (validationErrors.Any())
            {
                result.Errors.AddRange(validationErrors);
                result.Success = false;
                return result;
            }

            var electionNode = xmlDoc.DocumentElement;
            var voters = new List<CdnVoter>();
            var ballots = new List<CdnBallot>();

            foreach (XmlElement voterNode in electionNode.SelectNodes("descendant::voter"))
            {
                var voter = new CdnVoter();
                voter.bahaiid = voterNode.GetAttribute("bahaiid");
                voter.firstname = voterNode.GetAttribute("firstname");
                voter.lastname = voterNode.GetAttribute("lastname");
                voters.Add(voter);
            }

            foreach (XmlElement ballotNode in electionNode.SelectNodes("descendant::ballot"))
            {
                var ballot = new CdnBallot();
                ballot.index = int.Parse(ballotNode.GetAttribute("index"));
                ballot.guid = ballotNode.GetAttribute("guid");

                foreach (XmlElement voteNode in ballotNode.SelectNodes("vote"))
                {
                    var rawVote = new OnlineRawVote(voteNode.InnerText);
                    ballot.Votes.Add(rawVote);
                }
                ballots.Add(ballot);
            }

            if (voters.Count != ballots.Count)
            {
                result.Errors.Add($"Voter count ({voters.Count}) must match ballot count ({ballots.Count})");
                result.Success = false;
                return result;
            }

            var importedLocation = await _context.Locations
                .FirstOrDefaultAsync(l => l.ElectionGuid == electionGuid && l.LocationTypeEnum == LocationType.Imported);

            if (importedLocation == null)
            {
                importedLocation = new Location
                {
                    LocationGuid = Guid.NewGuid(),
                    ElectionGuid = electionGuid,
                    Name = "Imported",
                    LocationTypeCode = LocationType.Imported.ToString()
                };
                _context.Locations.Add(importedLocation);
            }

            var election = await _context.Elections.FindAsync(electionGuid);
            if (election != null && !(election.VotingMethods?.Contains("I") == true))
            {
                election.VotingMethods = (election.VotingMethods ?? "") + "I";
                _context.Elections.Update(election);
            }

            var peopleCache = await _context.People
                .Where(p => p.ElectionGuid == electionGuid && p.BahaiId != null)
                .ToDictionaryAsync(p => p.BahaiId!);

            var ballotCounter = await _context.Ballots
                .Where(b => b.LocationGuid == importedLocation.LocationGuid)
                .CountAsync() + 1;

            foreach (var voter in voters)
            {
                if (!peopleCache.TryGetValue(voter.bahaiid, out var person))
                {
                    result.Errors.Add($"Voter with BahaiId {voter.bahaiid} not found in election");
                    continue;
                }

                if (!string.IsNullOrEmpty(person.VotingMethod) && person.VotingMethod != "I")
                {
                    result.Warnings.Add($"{person.FullNameFl} has already voted with method {person.VotingMethod}");
                    continue;
                }

                person.VotingMethod = "I";
                person.VotingLocationGuid = importedLocation.LocationGuid;
                person.RegistrationTime = DateTime.UtcNow;
                person.EnvNum = null;
                _context.People.Update(person);
            }

            foreach (var ballot in ballots)
            {
                var ballotEntity = new Ballot
                {
                    BallotGuid = Guid.NewGuid(),
                    LocationGuid = importedLocation.LocationGuid,
                    StatusCode = BallotStatus.Ok,
                    ComputerCode = "IM",
                    BallotNumAtComputer = ballotCounter++,
                    RowVersion = new byte[8]
                };

                _context.Ballots.Add(ballotEntity);

                foreach (var vote in ballot.Votes)
                {
                    var matchedPerson = peopleCache.Values
                        .FirstOrDefault(p =>
                            p.FirstName?.ToLower() == vote.First?.ToLower() &&
                            p.LastName?.ToLower() == vote.Last?.ToLower());

                    if (matchedPerson != null)
                    {
                        var voteEntity = new Vote
                        {
                            BallotGuid = ballotEntity.BallotGuid,
                            PersonGuid = matchedPerson.PersonGuid,
                            PositionOnBallot = ballot.Votes.IndexOf(vote) + 1,
                            VoteStatus = VoteStatus.Ok,
                            PersonCombinedInfo = matchedPerson.CombinedInfo,
                            RowVersion = new byte[8]
                        };
                        _context.Votes.Add(voteEntity);
                        result.VotesCreated++;
                    }
                    else
                    {
                        result.Warnings.Add($"Could not match vote '{vote.First} {vote.Last}' in ballot {ballot.index}");
                    }
                }

                result.BallotsCreated++;
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            result.Success = result.Errors.Count == 0;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            result.Errors.Add($"Import failed: {ex.Message}");
            result.Success = false;
        }

        return result;
    }

    // Job 2: Import from TallyJv2-Export.xsd format
    public async Task<ElectionDto> ImportTallyJv2ElectionAsync(Stream xmlStream, Guid? userId = null)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // Validate XML against schema
            var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas", "TallyJv2-Export.xsd");
            var xmlDoc = new XmlDocument();

            using (var reader = new StreamReader(xmlStream))
            {
                var xmlContent = await reader.ReadToEndAsync();
                xmlDoc.LoadXml(xmlContent);
            }

            // Validate against schema
            xmlDoc.Schemas.Add("urn:tallyj.bahai:v2", schemaPath);
            var validationErrors = new List<string>();
            xmlDoc.Validate((sender, args) =>
            {
                if (args.Severity == XmlSeverityType.Error)
                    validationErrors.Add(args.Message);
            });

            if (validationErrors.Any())
            {
                throw new InvalidOperationException("XML validation failed: " + string.Join("; ", validationErrors));
            }

            // Parse and create new election
            var root = xmlDoc.DocumentElement;
            var nsm = new XmlNamespaceManager(xmlDoc.NameTable);
            nsm.AddNamespace("t", "urn:tallyj.bahai:v2");

            var electionNode = root.SelectSingleNode("t:election", nsm) as XmlElement;
            if (electionNode == null) throw new InvalidOperationException("No election element found");

            // Create new election with remapped GUIDs
            var guidMap = new Dictionary<Guid, Guid>();
            var newElectionGuid = Guid.NewGuid();

            var election = new Election
            {
                ElectionGuid = newElectionGuid,
                Name = electionNode.GetAttribute("Name") ?? "Imported Election",
                Convenor = electionNode.GetAttribute("Convenor"),
                DateOfElection = ParseDateTime(electionNode.GetAttribute("DateOfElection")),
                ElectionType = electionNode.GetAttribute("ElectionType"),
                ElectionMode = electionNode.GetAttribute("ElectionMode"),
                NumberToElect = ParseInt(electionNode.GetAttribute("NumberToElect")),
                NumberExtra = ParseInt(electionNode.GetAttribute("NumberExtra")),
                CanVote = electionNode.GetAttribute("CanVote"),
                CanReceive = electionNode.GetAttribute("CanReceive"),
                ElectionPasscode = electionNode.GetAttribute("ElectionPasscode"),
                LastEnvNum = ParseInt(electionNode.GetAttribute("LastEnvNum")),
                ListForPublic = ParseBool(electionNode.GetAttribute("ListForPublic")),
                ShowFullReport = ParseBool(electionNode.GetAttribute("ShowFullReport")),
                OnlineWhenOpen = ParseDateTime(electionNode.GetAttribute("OnlineWhenOpen")),
                OnlineWhenClose = ParseDateTime(electionNode.GetAttribute("OnlineWhenClose")),
                OnlineCloseIsEstimate = ParseBool(electionNode.GetAttribute("OnlineCloseIsEstimate")) ?? true,
                OnlineSelectionProcess = electionNode.GetAttribute("OnlineSelectionProcess"),
                EmailFromAddress = electionNode.GetAttribute("EmailFromAddress"),
                EmailFromName = electionNode.GetAttribute("EmailFromName"),
                EmailText = electionNode.GetAttribute("EmailText"),
                SmsText = electionNode.GetAttribute("SmsText"),
                EmailSubject = electionNode.GetAttribute("EmailSubject"),
                CustomMethods = electionNode.GetAttribute("CustomMethods"),
                VotingMethods = electionNode.GetAttribute("VotingMethods"),
                Flags = electionNode.GetAttribute("Flags"),
                RowVersion = new byte[8]
            };

            _context.Elections.Add(election);

            // Load tellers
            var tellerNodes = root.SelectNodes("t:teller", nsm);
            if (tellerNodes != null)
            {
                foreach (XmlElement tellerNode in tellerNodes)
                {
                    var teller = new Teller
                    {
                        ElectionGuid = newElectionGuid,
                        Name = tellerNode.GetAttribute("Name") ?? "",
                        IsHeadTeller = ParseBool(tellerNode.GetAttribute("IsHeadTeller")),
                        RowVersion = new byte[8]
                    };
                    _context.Tellers.Add(teller);
                }
            }

            // Load locations
            var locationNodes = root.SelectNodes("t:location", nsm);
            if (locationNodes != null)
            {
                foreach (XmlElement locationNode in locationNodes)
                {
                    var oldGuid = Guid.Parse(locationNode.GetAttribute("LocationGuid") ?? Guid.NewGuid().ToString());
                    var newGuid = Guid.NewGuid();
                    guidMap[oldGuid] = newGuid;

                    var location = new Location
                    {
                        ElectionGuid = newElectionGuid,
                        LocationGuid = newGuid,
                        Name = locationNode.GetAttribute("Name") ?? "",
                        ContactInfo = locationNode.GetAttribute("ContactInfo"),
                        Long = locationNode.GetAttribute("Long"),
                        Lat = locationNode.GetAttribute("Lat"),
                        TallyStatus = locationNode.GetAttribute("TallyStatus"),
                        SortOrder = ParseInt(locationNode.GetAttribute("SortOrder")),
                        BallotsCollected = ParseInt(locationNode.GetAttribute("BallotsCollected")),
                        LocationTypeCode = locationNode.GetAttribute("LocationTypeCode") ?? LocationType.Manual.ToString()
                    };
                    _context.Locations.Add(location);
                }
            }

            // Load people
            var personNodes = root.SelectNodes("t:person", nsm);
            if (personNodes != null)
            {
                foreach (XmlElement personNode in personNodes)
                {
                    var oldGuid = Guid.Parse(personNode.GetAttribute("PersonGuid") ?? Guid.NewGuid().ToString());
                    var newGuid = Guid.NewGuid();
                    guidMap[oldGuid] = newGuid;

                    var person = new Person
                    {
                        ElectionGuid = newElectionGuid,
                        PersonGuid = newGuid,
                        LastName = personNode.GetAttribute("LastName") ?? "",
                        FirstName = personNode.GetAttribute("FirstName"),
                        OtherLastNames = personNode.GetAttribute("OtherLastNames"),
                        OtherNames = personNode.GetAttribute("OtherNames"),
                        OtherInfo = personNode.GetAttribute("OtherInfo"),
                        Area = personNode.GetAttribute("Area"),
                        BahaiId = personNode.GetAttribute("BahaiId"),
                        CombinedInfo = personNode.GetAttribute("CombinedInfo"),
                        CombinedSoundCodes = personNode.GetAttribute("CombinedSoundCodes"),
                        CombinedInfoAtStart = personNode.GetAttribute("CombinedInfoAtStart"),
                        AgeGroup = personNode.GetAttribute("AgeGroup"),
                        CanVote = ParseBool(personNode.GetAttribute("CanVote")) ?? true,
                        CanReceiveVotes = ParseBool(personNode.GetAttribute("CanReceiveVotes")),
                        IneligibleReasonGuid = ParseGuid(personNode.GetAttribute("IneligibleReasonGuid")),
                        RegistrationTime = ParseDateTime(personNode.GetAttribute("RegistrationTime")),
                        VotingLocationGuid = guidMap.ContainsKey(ParseGuid(personNode.GetAttribute("VotingLocationGuid")) ?? Guid.Empty)
                            ? guidMap[ParseGuid(personNode.GetAttribute("VotingLocationGuid")) ?? Guid.Empty]
                            : null,
                        VotingMethod = personNode.GetAttribute("VotingMethod"),
                        EnvNum = ParseInt(personNode.GetAttribute("EnvNum")),
                        Teller1 = personNode.GetAttribute("TellerAtKeyboard"),
                        Teller2 = personNode.GetAttribute("TellerAssisting"),
                        Email = personNode.GetAttribute("Email"),
                        Phone = personNode.GetAttribute("Phone"),
                        HasOnlineBallot = ParseBool(personNode.GetAttribute("HasOnlineBallot")),
                        Flags = personNode.GetAttribute("Flags"),
                        UnitName = personNode.GetAttribute("UnitName"),
                        KioskCode = personNode.GetAttribute("KioskCode"),
                        RegistrationHistory = personNode.GetAttribute("RegistrationHistory"),
                        RowVersion = new byte[8]
                    };
                    _context.People.Add(person);
                }
            }

            // Load ballots and votes
            foreach (XmlElement locationNode in locationNodes)
            {
                var locationGuid = guidMap[Guid.Parse(locationNode.GetAttribute("LocationGuid") ?? Guid.Empty.ToString())];
                var ballotNodes = locationNode.SelectNodes("t:ballot", nsm);

                if (ballotNodes != null)
                {
                    foreach (XmlElement ballotNode in ballotNodes)
                    {
                        var ballotGuid = Guid.NewGuid();
                        var ballot = new Ballot
                        {
                            BallotGuid = ballotGuid,
                            LocationGuid = locationGuid,
                            StatusCode = Enum.Parse<BallotStatus>(ballotNode.GetAttribute("StatusCode") ?? "Ok"),
                            ComputerCode = ballotNode.GetAttribute("ComputerCode"),
                            BallotNumAtComputer = ParseInt(ballotNode.GetAttribute("BallotNumAtComputer")) ?? 0,
                            Teller1 = ballotNode.GetAttribute("TellerAtKeyboard"),
                            Teller2 = ballotNode.GetAttribute("TellerAssisting"),
                            RowVersion = new byte[8]
                        };
                        _context.Ballots.Add(ballot);

                        var voteNodes = ballotNode.SelectNodes("t:vote", nsm);
                        if (voteNodes != null)
                        {
                            var position = 1;
                            foreach (XmlElement voteNode in voteNodes)
                            {
                                var vote = new Vote
                                {
                                    BallotGuid = ballotGuid,
                                    PositionOnBallot = position++,
                                    PersonGuid = guidMap.ContainsKey(ParseGuid(voteNode.GetAttribute("PersonGuid")) ?? Guid.Empty)
                                        ? guidMap[ParseGuid(voteNode.GetAttribute("PersonGuid")) ?? Guid.Empty]
                                        : null,
                                    VoteStatus = Enum.Parse<VoteStatus>(voteNode.GetAttribute("StatusCode") ?? "Ok"),
                                    IneligibleReasonCode = voteNode.GetAttribute("InvalidReasonGuid"),
                                    SingleNameElectionCount = ParseInt(voteNode.GetAttribute("SingleNameElectionCount")),
                                    RowVersion = new byte[8]
                                };
                                _context.Votes.Add(vote);
                            }
                        }
                    }
                }
            }

            // Load result summaries
            var resultSummaryNodes = root.SelectNodes("t:resultSummary", nsm);
            if (resultSummaryNodes != null)
            {
                foreach (XmlElement rsNode in resultSummaryNodes)
                {
                    var resultSummary = new ResultSummary
                    {
                        ElectionGuid = newElectionGuid,
                        ResultType = rsNode.GetAttribute("ResultType") ?? "M",
                        NumVoters = ParseInt(rsNode.GetAttribute("NumVoters")),
                        NumEligibleToVote = ParseInt(rsNode.GetAttribute("NumEligibleToVote")),
                        BallotsReceived = ParseInt(rsNode.GetAttribute("NumBallotsEntered")) ?? ParseInt(rsNode.GetAttribute("BallotsReceived")),
                        InPersonBallots = ParseInt(rsNode.GetAttribute("EnvelopesInPerson")) ?? ParseInt(rsNode.GetAttribute("InPersonBallots")),
                        DroppedOffBallots = ParseInt(rsNode.GetAttribute("EnvelopesDroppedOff")) ?? ParseInt(rsNode.GetAttribute("DroppedOffBallots")),
                        MailedInBallots = ParseInt(rsNode.GetAttribute("EnvelopesMailedIn")) ?? ParseInt(rsNode.GetAttribute("MailedInBallots")),
                        CalledInBallots = ParseInt(rsNode.GetAttribute("EnvelopesCalledIn")) ?? ParseInt(rsNode.GetAttribute("CalledInBallots")),
                        OnlineBallots = ParseInt(rsNode.GetAttribute("EnvelopesOnline")) ?? ParseInt(rsNode.GetAttribute("OnlineBallots")),
                        ImportedBallots = ParseInt(rsNode.GetAttribute("EnvelopesImported")) ?? ParseInt(rsNode.GetAttribute("ImportedBallots")),
                        Custom1Ballots = ParseInt(rsNode.GetAttribute("EnvelopesCustom1")) ?? ParseInt(rsNode.GetAttribute("Custom1Ballots")),
                        Custom2Ballots = ParseInt(rsNode.GetAttribute("EnvelopesCustom2")) ?? ParseInt(rsNode.GetAttribute("Custom2Ballots")),
                        Custom3Ballots = ParseInt(rsNode.GetAttribute("EnvelopesCustom3")) ?? ParseInt(rsNode.GetAttribute("Custom3Ballots")),
                        BallotsNeedingReview = ParseInt(rsNode.GetAttribute("BallotsNeedingReview")),
                        SpoiledBallots = ParseInt(rsNode.GetAttribute("SpoiledBallots")),
                        SpoiledVotes = ParseInt(rsNode.GetAttribute("SpoiledVotes")),
                        TotalVotes = ParseInt(rsNode.GetAttribute("TotalVotes")),
                        UseOnReports = ParseBool(rsNode.GetAttribute("UseOnReports")),
                        SpoiledManualBallots = ParseInt(rsNode.GetAttribute("SpoiledManualBallots"))
                    };
                    _context.ResultSummaries.Add(resultSummary);
                }
            }

            // Load results
            var resultNodes = root.SelectNodes("t:result", nsm);
            if (resultNodes != null)
            {
                foreach (XmlElement resultNode in resultNodes)
                {
                    var result = new Result
                    {
                        ElectionGuid = newElectionGuid,
                        PersonGuid = guidMap[Guid.Parse(resultNode.GetAttribute("PersonGuid") ?? Guid.Empty.ToString())],
                        VoteCount = ParseInt(resultNode.GetAttribute("VoteCount")),
                        Rank = ParseInt(resultNode.GetAttribute("Rank")) ?? 0,
                        Section = resultNode.GetAttribute("Section") ?? "T",
                        IsTied = ParseBool(resultNode.GetAttribute("IsTied")),
                        IsTieResolved = ParseBool(resultNode.GetAttribute("IsTieResolved")),
                        TieBreakGroup = ParseInt(resultNode.GetAttribute("TieBreakGroup")),
                        CloseToPrev = ParseBool(resultNode.GetAttribute("CloseToPrev")),
                        CloseToNext = ParseBool(resultNode.GetAttribute("CloseToNext")),
                        RankInExtra = ParseInt(resultNode.GetAttribute("RankInExtra")),
                        TieBreakRequired = ParseBool(resultNode.GetAttribute("TieBreakRequired")),
                        TieBreakCount = ParseInt(resultNode.GetAttribute("TieBreakCount")),
                        ForceShowInOther = ParseBool(resultNode.GetAttribute("ForceShowInOther"))
                    };
                    _context.Results.Add(result);
                }
            }

            // Load result ties
            var resultTieNodes = root.SelectNodes("t:resultTie", nsm);
            if (resultTieNodes != null)
            {
                foreach (XmlElement rtNode in resultTieNodes)
                {
                    var resultTie = new ResultTie
                    {
                        ElectionGuid = newElectionGuid,
                        TieBreakGroup = ParseInt(rtNode.GetAttribute("TieBreakGroup")) ?? 0,
                        NumInTie = ParseInt(rtNode.GetAttribute("NumInTie")) ?? 0,
                        IsResolved = ParseBool(rtNode.GetAttribute("IsResolved")) ?? false,
                        TieBreakRequired = ParseBool(rtNode.GetAttribute("TieBreakRequired")),
                        NumToElect = ParseInt(rtNode.GetAttribute("NumToElect")) ?? 0
                    };
                    _context.ResultTies.Add(resultTie);
                }
            }

            // Load online voting info
            var oviNodes = root.SelectNodes("t:onlineVoterInfo", nsm);
            if (oviNodes != null)
            {
                foreach (XmlElement oviNode in oviNodes)
                {
                    var ovi = new OnlineVotingInfo
                    {
                        ElectionGuid = newElectionGuid,
                        PersonGuid = guidMap[Guid.Parse(oviNode.GetAttribute("PersonGuid") ?? Guid.Empty.ToString())],
                        WhenBallotCreated = ParseDateTime(oviNode.GetAttribute("WhenBallotCreated")),
                        Status = oviNode.GetAttribute("Status") ?? "",
                        WhenStatus = ParseDateTime(oviNode.GetAttribute("WhenStatus")),
                        ListPool = oviNode.GetAttribute("ListPool"),
                        PoolLocked = ParseBool(oviNode.GetAttribute("PoolLocked")),
                        HistoryStatus = oviNode.GetAttribute("HistoryStatus"),
                        NotifiedAboutOpening = ParseBool(oviNode.GetAttribute("NotifiedAboutOpening"))
                    };
                    _context.OnlineVotingInfos.Add(ovi);
                }
            }

            // Load logs
            var logNodes = root.SelectNodes("t:log", nsm);
            if (logNodes != null)
            {
                foreach (XmlElement logNode in logNodes)
                {
                    var log = new Log
                    {
                        ElectionGuid = newElectionGuid,
                        LocationGuid = guidMap.ContainsKey(ParseGuid(logNode.GetAttribute("LocationGuid")) ?? Guid.Empty)
                            ? guidMap[ParseGuid(logNode.GetAttribute("LocationGuid")) ?? Guid.Empty]
                            : null,
                        VoterId = logNode.GetAttribute("VoterId"),
                        ComputerCode = logNode.GetAttribute("ComputerCode"),
                        Details = logNode.GetAttribute("Details"),
                        AsOf = ParseDateTime(logNode.GetAttribute("AsOf")) ?? DateTime.UtcNow
                    };
                    _context.Logs.Add(log);
                }
            }

            await _context.SaveChangesAsync();

            if (userId.HasValue)
            {
                await AssociateUserWithElectionAsync(newElectionGuid, userId.Value);
            }

            await transaction.CommitAsync();

            return await _electionService.GetElectionByGuidAsync(newElectionGuid) ?? throw new InvalidOperationException("Failed to create election");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException($"Import failed: {ex.Message}", ex);
        }
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
            exportedAt = DateTime.UtcNow.ToString("o"),
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
                election.CanVote,
                election.CanReceive,
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
            using var reader = new StreamReader(jsonStream);
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
                CanVote = importData.election.CanVote,
                CanReceive = importData.election.CanReceive,
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

            // Import locations
            foreach (var loc in importData.locations)
            {
                var oldGuid = loc.LocationGuid;
                var newGuid = Guid.NewGuid();
                guidMap[oldGuid] = newGuid;

                var location = new Location
                {
                    ElectionGuid = newElectionGuid,
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

            // Import people
            foreach (var person in importData.people)
            {
                var oldGuid = person.PersonGuid;
                var newGuid = Guid.NewGuid();
                guidMap[oldGuid] = newGuid;

                var p = new Person
                {
                    ElectionGuid = newElectionGuid,
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

            // Import ballots and votes
            foreach (var ballot in importData.ballots)
            {
                var b = new Ballot
                {
                    BallotGuid = Guid.NewGuid(),
                    LocationGuid = guidMap[ballot.LocationGuid],
                    StatusCode = Enum.Parse<BallotStatus>(ballot.StatusCode ?? "Ok"),
                    ComputerCode = ballot.ComputerCode,
                    BallotNumAtComputer = ballot.BallotNumAtComputer,
                    Teller1 = ballot.Teller1,
                    Teller2 = ballot.Teller2,
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

            // Import tellers
            foreach (var teller in importData.tellers)
            {
                var t = new Teller
                {
                    ElectionGuid = newElectionGuid,
                    Name = teller.Name ?? "",
                    IsHeadTeller = teller.IsHeadTeller,
                    UsingComputerCode = teller.UsingComputerCode,
                    RowVersion = new byte[8]
                };
                _context.Tellers.Add(t);
            }

            // Import results
            foreach (var result in importData.results)
            {
                var r = new Result
                {
                    ElectionGuid = newElectionGuid,
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

            // Import result summaries
            foreach (var rs in importData.resultSummaries)
            {
                var resultSummary = new ResultSummary
                {
                    ElectionGuid = newElectionGuid,
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

            // Import result ties
            foreach (var rt in importData.resultTies)
            {
                var resultTie = new ResultTie
                {
                    ElectionGuid = newElectionGuid,
                    TieBreakGroup = rt.TieBreakGroup,
                    NumInTie = rt.NumInTie,
                    IsResolved = rt.IsResolved,
                    TieBreakRequired = rt.TieBreakRequired,
                    NumToElect = rt.NumToElect ?? 0
                };
                _context.ResultTies.Add(resultTie);
            }

            // Import online voting info
            foreach (var ovi in importData.onlineVotingInfos)
            {
                var onlineVotingInfo = new OnlineVotingInfo
                {
                    ElectionGuid = newElectionGuid,
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

            // Import logs
            foreach (var log in importData.logs)
            {
                var l = new Log
                {
                    ElectionGuid = newElectionGuid,
                    LocationGuid = log.LocationGuid.HasValue && guidMap.ContainsKey(log.LocationGuid.Value)
                        ? guidMap[log.LocationGuid.Value]
                        : null,
                    VoterId = log.VoterId,
                    ComputerCode = log.ComputerCode,
                    Details = log.Details,
                    AsOf = ParseDateTime(log.AsOf) ?? DateTime.UtcNow
                };
                _context.Logs.Add(l);
            }

            await _context.SaveChangesAsync();

            if (userId.HasValue)
            {
                await AssociateUserWithElectionAsync(newElectionGuid, userId.Value);
            }

            await transaction.CommitAsync();

            return await _electionService.GetElectionByGuidAsync(newElectionGuid) ?? throw new InvalidOperationException("Failed to create election");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            throw new InvalidOperationException($"Import failed: {ex.Message}", ex);
        }
    }

    // Helper methods
    private static DateTime? ParseDateTime(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : DateTime.Parse(value);
    }

    private static int? ParseInt(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : int.Parse(value);
    }

    private static bool? ParseBool(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : bool.Parse(value);
    }

    private static Guid? ParseGuid(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : Guid.Parse(value);
    }

    // Data classes for import
    private class CdnVoter
    {
        public string bahaiid { get; set; } = "";
        public string firstname { get; set; } = "";
        public string lastname { get; set; } = "";
        public Guid? PersonGuid { get; set; }
        public string? VotingMethod { get; set; }
        public bool ImportBlocked { get; set; }
    }

    private class CdnBallot
    {
        public int index { get; set; }
        public string guid { get; set; } = "";
        public List<OnlineRawVote> Votes { get; set; } = new();
    }

    private class JsonImportData
    {
        public string format { get; set; } = "";
        public string version { get; set; } = "";
        public string exportedAt { get; set; } = "";
        public JsonElection election { get; set; } = new();
        public List<JsonLocation> locations { get; set; } = new();
        public List<JsonPerson> people { get; set; } = new();
        public List<JsonBallot> ballots { get; set; } = new();
        public List<JsonTeller> tellers { get; set; } = new();
        public List<JsonResult> results { get; set; } = new();
        public List<JsonResultSummary> resultSummaries { get; set; } = new();
        public List<JsonResultTie> resultTies { get; set; } = new();
        public List<JsonOnlineVotingInfo> onlineVotingInfos { get; set; } = new();
        public List<JsonLog> logs { get; set; } = new();
    }

    private class JsonElection
    {
        public Guid ElectionGuid { get; set; }
        public string? Name { get; set; }
        public string? Convenor { get; set; }
        public string? DateOfElection { get; set; }
        public string? ElectionType { get; set; }
        public string? ElectionMode { get; set; }
        public int? NumberToElect { get; set; }
        public int? NumberExtra { get; set; }
        public string? CanVote { get; set; }
        public string? CanReceive { get; set; }
        public string? ElectionPasscode { get; set; }
        public int? LastEnvNum { get; set; }
        public bool? ListForPublic { get; set; }
        public bool? ShowFullReport { get; set; }
        public string? OnlineWhenOpen { get; set; }
        public string? OnlineWhenClose { get; set; }
        public bool? OnlineCloseIsEstimate { get; set; }
        public string? OnlineSelectionProcess { get; set; }
        public string? EmailFromAddress { get; set; }
        public string? EmailFromName { get; set; }
        public string? EmailText { get; set; }
        public string? SmsText { get; set; }
        public string? EmailSubject { get; set; }
        public string? CustomMethods { get; set; }
        public string? VotingMethods { get; set; }
        public string? Flags { get; set; }
    }

    private class JsonLocation
    {
        public Guid LocationGuid { get; set; }
        public string? Name { get; set; }
        public string? ContactInfo { get; set; }
        public string? Long { get; set; }
        public string? Lat { get; set; }
        public string? TallyStatus { get; set; }
        public int? SortOrder { get; set; }
        public int? BallotsCollected { get; set; }
        public string? LocationTypeCode { get; set; }
    }

    private class JsonPerson
    {
        public Guid PersonGuid { get; set; }
        public string? LastName { get; set; }
        public string? FirstName { get; set; }
        public string? OtherLastNames { get; set; }
        public string? OtherNames { get; set; }
        public string? OtherInfo { get; set; }
        public string? Area { get; set; }
        public string? BahaiId { get; set; }
        public string? CombinedInfo { get; set; }
        public string? CombinedSoundCodes { get; set; }
        public string? CombinedInfoAtStart { get; set; }
        public string? AgeGroup { get; set; }
        public bool? CanVote { get; set; }
        public bool? CanReceiveVotes { get; set; }
        public Guid? IneligibleReasonGuid { get; set; }
        public string? RegistrationTime { get; set; }
        public Guid? VotingLocationGuid { get; set; }
        public string? VotingMethod { get; set; }
        public int? EnvNum { get; set; }
        public string? Teller1 { get; set; }
        public string? Teller2 { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool? HasOnlineBallot { get; set; }
        public string? Flags { get; set; }
        public string? UnitName { get; set; }
        public string? KioskCode { get; set; }
        public string? RegistrationHistory { get; set; }
    }

    private class JsonBallot
    {
        public Guid BallotGuid { get; set; }
        public Guid LocationGuid { get; set; }
        public string? StatusCode { get; set; }
        public string? ComputerCode { get; set; }
        public int BallotNumAtComputer { get; set; }
        public string? Teller1 { get; set; }
        public string? Teller2 { get; set; }
        public List<JsonVote> votes { get; set; } = new();
    }

    private class JsonVote
    {
        public int PositionOnBallot { get; set; }
        public Guid? PersonGuid { get; set; }
        public string? VoteStatus { get; set; }
        public string? IneligibleReasonCode { get; set; }
        public int? SingleNameElectionCount { get; set; }
        public string? PersonCombinedInfo { get; set; }
        public string? OnlineVoteRaw { get; set; }
    }

    private class JsonTeller
    {
        public string? Name { get; set; }
        public bool? IsHeadTeller { get; set; }
        public string? UsingComputerCode { get; set; }
    }

    private class JsonResult
    {
        public Guid PersonGuid { get; set; }
        public int? VoteCount { get; set; }
        public int Rank { get; set; }
        public string? Section { get; set; }
        public bool? IsTied { get; set; }
        public bool? IsTieResolved { get; set; }
        public int? TieBreakGroup { get; set; }
        public bool? CloseToPrev { get; set; }
        public bool? CloseToNext { get; set; }
        public int? RankInExtra { get; set; }
        public bool? TieBreakRequired { get; set; }
        public int? TieBreakCount { get; set; }
        public bool? ForceShowInOther { get; set; }
    }

    private class JsonResultSummary
    {
        public string? ResultType { get; set; }
        public int? NumVoters { get; set; }
        public int? NumEligibleToVote { get; set; }
        public int? BallotsReceived { get; set; }
        public int? InPersonBallots { get; set; }
        public int? DroppedOffBallots { get; set; }
        public int? MailedInBallots { get; set; }
        public int? CalledInBallots { get; set; }
        public int? OnlineBallots { get; set; }
        public int? ImportedBallots { get; set; }
        public int? Custom1Ballots { get; set; }
        public int? Custom2Ballots { get; set; }
        public int? Custom3Ballots { get; set; }
        public int? BallotsNeedingReview { get; set; }
        public int? SpoiledBallots { get; set; }
        public int? SpoiledVotes { get; set; }
        public int? TotalVotes { get; set; }
        public bool? UseOnReports { get; set; }
        public int? SpoiledManualBallots { get; set; }
    }

    private class JsonResultTie
    {
        public int TieBreakGroup { get; set; }
        public int NumInTie { get; set; }
        public bool IsResolved { get; set; }
        public bool? TieBreakRequired { get; set; }
        public int? NumToElect { get; set; }
    }

    private class JsonOnlineVotingInfo
    {
        public Guid PersonGuid { get; set; }
        public string? WhenBallotCreated { get; set; }
        public string? Status { get; set; }
        public string? WhenStatus { get; set; }
        public string? ListPool { get; set; }
        public bool? PoolLocked { get; set; }
        public string? HistoryStatus { get; set; }
        public bool? NotifiedAboutOpening { get; set; }
    }

    private class JsonLog
    {
        public string? AsOf { get; set; }
        public Guid? LocationGuid { get; set; }
        public string? VoterId { get; set; }
        public string? ComputerCode { get; set; }
        public string? Details { get; set; }
    }
}  
