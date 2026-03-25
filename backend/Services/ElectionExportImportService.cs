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

public class ElectionExportImportService
{
    private const string LocationGuidAttribute = "LocationGuid";
    private const string PersonGuidAttribute = "PersonGuid";

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

    private static async Task<List<string>> ValidateXmlAgainstSchemaAsync(Stream xmlStream, string schemaPath)
    {
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

        return validationErrors;
    }

    private static async Task<XmlDocument> ValidateAndLoadTallyJv2XmlDocumentAsync(Stream xmlStream)
    {
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

        return xmlDoc;
    }

    private static (Election election, Dictionary<Guid, Guid> guidMap) CreateElectionFromXml(XmlElement root, XmlNamespaceManager nsm)
    {
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

        return (election, guidMap);
    }

    private void ImportTellersFromXml(XmlElement root, XmlNamespaceManager nsm, Guid electionGuid)
    {
        var tellerNodes = root.SelectNodes("t:teller", nsm);
        if (tellerNodes != null)
        {
            foreach (XmlElement tellerNode in tellerNodes)
            {
                var teller = new Teller
                {
                    ElectionGuid = electionGuid,
                    Name = tellerNode.GetAttribute("Name") ?? "",
                    IsHeadTeller = ParseBool(tellerNode.GetAttribute("IsHeadTeller")),
                    RowVersion = new byte[8]
                };
                _context.Tellers.Add(teller);
            }
        }
    }

    private void ImportLocationsFromXml(XmlElement root, XmlNamespaceManager nsm, Guid electionGuid, Dictionary<Guid, Guid> guidMap)
    {
        var locationNodes = root.SelectNodes("t:location", nsm);
        if (locationNodes != null)
        {
            foreach (XmlElement locationNode in locationNodes)
            {
                var oldGuid = Guid.Parse(locationNode.GetAttribute(LocationGuidAttribute) ?? Guid.NewGuid().ToString());
                var newGuid = Guid.NewGuid();
                guidMap[oldGuid] = newGuid;

                var location = new Location
                {
                    ElectionGuid = electionGuid,
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
    }

    private void ImportPeopleFromXml(XmlElement root, XmlNamespaceManager nsm, Guid electionGuid, Dictionary<Guid, Guid> guidMap)
    {
        var personNodes = root.SelectNodes("t:person", nsm);
        if (personNodes != null)
        {
            foreach (XmlElement personNode in personNodes)
            {
                var oldGuid = Guid.Parse(personNode.GetAttribute(PersonGuidAttribute) ?? Guid.NewGuid().ToString());
                var newGuid = Guid.NewGuid();
                guidMap[oldGuid] = newGuid;

                var person = new Person
                {
                    ElectionGuid = electionGuid,
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
    }

    private void ImportVotesFromXml(Guid ballotGuid, XmlNodeList voteNodes, Dictionary<Guid, Guid> guidMap)
    {
        if (voteNodes != null)
        {
            var position = 1;
            foreach (XmlElement voteNode in voteNodes)
            {
                var vote = new Vote
                {
                    BallotGuid = ballotGuid,
                    PositionOnBallot = position++,
                    PersonGuid = guidMap.ContainsKey(ParseGuid(voteNode.GetAttribute(PersonGuidAttribute)) ?? Guid.Empty)
                        ? guidMap[ParseGuid(voteNode.GetAttribute(PersonGuidAttribute)) ?? Guid.Empty]
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

    private void ImportBallotFromXml(XmlElement ballotNode, Guid locationGuid, XmlNamespaceManager nsm, Dictionary<Guid, Guid> guidMap)
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
        ImportVotesFromXml(ballotGuid, voteNodes!, guidMap);
    }

    private void ImportBallotsForLocationFromXml(XmlElement locationNode, XmlNamespaceManager nsm, Dictionary<Guid, Guid> guidMap)
    {
        var attr = locationNode.GetAttribute(LocationGuidAttribute);
        var oldLocationGuid = attr != null ? Guid.Parse(attr) : Guid.Empty;
        if (!guidMap.ContainsKey(oldLocationGuid))
        {
            throw new InvalidOperationException($"Location GUID {oldLocationGuid} not found in map for ballot loading");
        }
        var locationGuid = guidMap[oldLocationGuid];
        var ballotNodes = locationNode.SelectNodes("t:ballot", nsm);

        if (ballotNodes != null)
        {
            foreach (XmlElement ballotNode in ballotNodes)
            {
                ImportBallotFromXml(ballotNode, locationGuid, nsm, guidMap);
            }
        }
    }

    private void ImportBallotsAndVotesFromXml(XmlNodeList locationNodes, XmlNamespaceManager nsm, Dictionary<Guid, Guid> guidMap)
    {
        foreach (XmlElement locationNode in locationNodes)
        {
            ImportBallotsForLocationFromXml(locationNode, nsm, guidMap);
        }
    }

    private void ImportResultsFromXml(XmlElement root, XmlNamespaceManager nsm, Guid electionGuid, Dictionary<Guid, Guid> guidMap)
    {
        // Load result summaries
        var resultSummaryNodes = root.SelectNodes("t:resultSummary", nsm);
        if (resultSummaryNodes != null)
        {
            foreach (XmlElement rsNode in resultSummaryNodes)
            {
                var resultSummary = new ResultSummary
                {
                    ElectionGuid = electionGuid,
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
                    ElectionGuid = electionGuid,
                    PersonGuid = guidMap[Guid.Parse(resultNode.GetAttribute(PersonGuidAttribute) ?? Guid.Empty.ToString())],
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
                    ElectionGuid = electionGuid,
                    TieBreakGroup = ParseInt(rtNode.GetAttribute("TieBreakGroup")) ?? 0,
                    NumInTie = ParseInt(rtNode.GetAttribute("NumInTie")) ?? 0,
                    IsResolved = ParseBool(rtNode.GetAttribute("IsResolved")) ?? false,
                    TieBreakRequired = ParseBool(rtNode.GetAttribute("TieBreakRequired")),
                    NumToElect = ParseInt(rtNode.GetAttribute("NumToElect")) ?? 0
                };
                _context.ResultTies.Add(resultTie);
            }
        }
    }

    private void ImportOnlineVotingInfoFromXml(XmlElement root, XmlNamespaceManager nsm, Guid electionGuid, Dictionary<Guid, Guid> guidMap)
    {
        var oviNodes = root.SelectNodes("t:onlineVoterInfo", nsm);
        if (oviNodes != null)
        {
            foreach (XmlElement oviNode in oviNodes)
            {
                var ovi = new OnlineVotingInfo
                {
                    ElectionGuid = electionGuid,
                    PersonGuid = guidMap[Guid.Parse(oviNode.GetAttribute(PersonGuidAttribute) ?? Guid.Empty.ToString())],
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
    }

    private void ImportLogsFromXml(XmlElement root, XmlNamespaceManager nsm, Guid electionGuid, Dictionary<Guid, Guid> guidMap)
    {
        var logNodes = root.SelectNodes("t:log", nsm);
        if (logNodes != null)
        {
            foreach (XmlElement logNode in logNodes)
            {
                var log = new Log
                {
                    ElectionGuid = electionGuid,
                    LocationGuid = guidMap.ContainsKey(ParseGuid(logNode.GetAttribute(LocationGuidAttribute)) ?? Guid.Empty)
                        ? guidMap[ParseGuid(logNode.GetAttribute(LocationGuidAttribute)) ?? Guid.Empty]
                        : null,
                    VoterId = logNode.GetAttribute("VoterId"),
                    ComputerCode = logNode.GetAttribute("ComputerCode"),
                    Details = logNode.GetAttribute("Details"),
                    AsOf = ParseDateTime(logNode.GetAttribute("AsOf")) ?? DateTime.UtcNow
                };
                _context.Logs.Add(log);
            }
        }
    }

    private static (List<CdnVoter> voters, List<string> errors) ParseVotersFromXml(XmlElement electionNode)
    {
        var voters = new List<CdnVoter>();
        var errors = new List<string>();

        foreach (XmlElement voterNode in electionNode.SelectNodes("descendant::voter")!)
        {
            var bahaiid = voterNode.GetAttribute("bahaiid");
            var firstname = voterNode.GetAttribute("firstname");
            var lastname = voterNode.GetAttribute("lastname");

            if (string.IsNullOrEmpty(bahaiid))
            {
                errors.Add("Voter is missing required bahaiid attribute");
                continue;
            }
            if (string.IsNullOrEmpty(firstname))
            {
                errors.Add("Voter is missing required firstname attribute");
                continue;
            }
            if (string.IsNullOrEmpty(lastname))
            {
                errors.Add("Voter is missing required lastname attribute");
                continue;
            }

            var voter = new CdnVoter();
            voter.bahaiid = bahaiid;
            voter.firstname = firstname;
            voter.lastname = lastname;
            voters.Add(voter);
        }

        return (voters, errors);
    }

    private static (List<CdnBallot> ballots, List<string> errors) ParseBallotsFromXml(XmlElement electionNode)
    {
        var ballots = new List<CdnBallot>();
        var errors = new List<string>();

        foreach (XmlElement ballotNode in electionNode.SelectNodes("descendant::ballot")!)
        {
            var indexAttr = ballotNode.GetAttribute("index");
            var guidAttr = ballotNode.GetAttribute("guid");

            if (string.IsNullOrEmpty(indexAttr))
            {
                errors.Add("Ballot is missing required index attribute");
                continue;
            }
            if (string.IsNullOrEmpty(guidAttr))
            {
                errors.Add("Ballot is missing required guid attribute");
                continue;
            }

            if (!int.TryParse(indexAttr, out var index))
            {
                errors.Add($"Ballot has invalid index attribute: {indexAttr}");
                continue;
            }

            var ballot = new CdnBallot();
            ballot.index = index;
            ballot.guid = guidAttr;

            foreach (XmlElement voteNode in ballotNode.SelectNodes("vote")!)
            {
                var rawVote = new OnlineRawVote(voteNode.InnerText);
                ballot.Votes.Add(rawVote);
            }
            ballots.Add(ballot);
        }

        return (ballots, errors);
    }

    private static string? ValidateVoterBallotCounts(List<CdnVoter> voters, List<CdnBallot> ballots)
    {
        if (voters.Count != ballots.Count)
        {
            return $"Voter count ({voters.Count}) must match ballot count ({ballots.Count})";
        }
        return null;
    }

    private async Task<Location> EnsureImportedLocationExistsAsync(Guid electionGuid)
    {
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

        return importedLocation;
    }

    private async Task UpdateElectionVotingMethodsAsync(Guid electionGuid)
    {
        var election = await _context.Elections.FindAsync(electionGuid);
        if (election != null && (election.VotingMethods?.Contains("I") != true))
        {
            election.VotingMethods = (election.VotingMethods ?? "") + "I";
            _context.Elections.Update(election);
        }
    }

    private async Task<(Dictionary<string, Person> peopleCache, int ballotCounter, List<string> missingBahaiIds)> PrepareVoterProcessingAsync(
        Guid electionGuid, Guid importedLocationGuid, List<CdnVoter> voters)
    {
        var peopleCache = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.BahaiId != null)
            .ToDictionaryAsync(p => p.BahaiId!);

        var ballotCounter = await _context.Ballots
            .Where(b => b.LocationGuid == importedLocationGuid)
            .CountAsync() + 1;

        var voterBahaiIds = voters.Select(v => v.bahaiid).ToList();
        var missingBahaiIds = voterBahaiIds.Where(id => !peopleCache.ContainsKey(id)).ToList();

        return (peopleCache, ballotCounter, missingBahaiIds);
    }

    private void ProcessVoters(List<CdnVoter> voters, Dictionary<string, Person> peopleCache,
        Guid importedLocationGuid, ImportResultDto result)
    {
        var validVoters = voters.Where(v => peopleCache.ContainsKey(v.bahaiid)).ToList();
        foreach (var voter in validVoters)
        {
            var person = peopleCache[voter.bahaiid];

            if (!string.IsNullOrEmpty(person.VotingMethod) && person.VotingMethod != "I")
            {
                result.Warnings.Add($"{person.FullNameFl} has already voted with method {person.VotingMethod}");
                continue;
            }

            person.VotingMethod = "I";
            person.VotingLocationGuid = importedLocationGuid;
            person.RegistrationTime = DateTime.UtcNow;
            person.EnvNum = null;
            _context.People.Update(person);
        }
    }

    private void ProcessBallots(List<CdnBallot> ballots, Dictionary<string, Person> peopleCache,
        Guid importedLocationGuid, ref int ballotCounter, ImportResultDto result)
    {
        foreach (var ballot in ballots)
        {
            var ballotEntity = new Ballot
            {
                BallotGuid = Guid.NewGuid(),
                LocationGuid = importedLocationGuid,
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
    }

    // Job 1: Import from CdnBallotImport.xsd format
    public async Task<ImportResultDto> ImportCdnBallotsAsync(Guid electionGuid, Stream xmlStream)
    {
        var result = new ImportResultDto();
        using var transaction = await _context.Database.BeginTransactionAsync();

        try
        {
            var schemaPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Schemas", "CdnBallotImport.xsd");
            var validationErrors = await ValidateXmlAgainstSchemaAsync(xmlStream, schemaPath);

            if (validationErrors.Any())
            {
                result.Errors.AddRange(validationErrors);
                result.Success = false;
                return result;
            }

            // Re-load the XML since the stream was consumed by validation
            var xmlDoc = new XmlDocument();
            using (var reader = new StreamReader(xmlStream))
            {
                var xmlContent = await reader.ReadToEndAsync();
                xmlDoc.LoadXml(xmlContent);
            }

            var electionNode = xmlDoc.DocumentElement!;
            var (voters, voterErrors) = ParseVotersFromXml(electionNode);
            result.Errors.AddRange(voterErrors);

            var (ballots, ballotErrors) = ParseBallotsFromXml(electionNode);
            result.Errors.AddRange(ballotErrors);

            var countValidationError = ValidateVoterBallotCounts(voters, ballots);
            if (countValidationError != null)
            {
                result.Errors.Add(countValidationError);
                result.Success = false;
                return result;
            }

            var importedLocation = await EnsureImportedLocationExistsAsync(electionGuid);
            await UpdateElectionVotingMethodsAsync(electionGuid);

            var (peopleCache, ballotCounter, missingBahaiIds) = await PrepareVoterProcessingAsync(
                electionGuid, importedLocation.LocationGuid, voters);

            foreach (var missingId in missingBahaiIds)
            {
                result.Errors.Add($"Voter with BahaiId {missingId} not found in election");
            }

            ProcessVoters(voters, peopleCache, importedLocation.LocationGuid, result);

            ProcessBallots(ballots, peopleCache, importedLocation.LocationGuid, ref ballotCounter, result);

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
            var xmlDoc = await ValidateAndLoadTallyJv2XmlDocumentAsync(xmlStream);

            // Parse and create new election
            var root = xmlDoc.DocumentElement!;
            var nsm = new XmlNamespaceManager(xmlDoc.NameTable);
            nsm.AddNamespace("t", "urn:tallyj.bahai:v2");

            var (election, guidMap) = CreateElectionFromXml(root, nsm);
            _context.Elections.Add(election);

            ImportTellersFromXml(root, nsm, election.ElectionGuid);

            ImportLocationsFromXml(root, nsm, election.ElectionGuid, guidMap);

            ImportPeopleFromXml(root, nsm, election.ElectionGuid, guidMap);

            var locationNodes = root.SelectNodes("t:location", nsm);
            ImportBallotsAndVotesFromXml(locationNodes!, nsm, guidMap);

            ImportResultsFromXml(root, nsm, election.ElectionGuid, guidMap);

            ImportOnlineVotingInfoFromXml(root, nsm, election.ElectionGuid, guidMap);

            ImportLogsFromXml(root, nsm, election.ElectionGuid, guidMap);

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
            var b = new Ballot
            {
                BallotGuid = Guid.NewGuid(),
                LocationGuid = guidMap[ballot.LocationGuid],
                StatusCode = Enum.Parse<BallotStatus>(ballot.StatusCode ?? "Ok"),
                ComputerCode = ballot.ComputerCode ?? "??",
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
                NumInTie = rt.NumInTie,
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
                AsOf = ParseDateTime(log.AsOf) ?? DateTime.UtcNow
            };
            _context.Logs.Add(l);
        }
    }

    // Helper methods
    private static DateTime? ParseDateTime(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : DateTime.Parse(value, CultureInfo.InvariantCulture);
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
    private sealed class CdnVoter
    {
        public string bahaiid { get; set; } = "";
        public string firstname { get; set; } = "";
        public string lastname { get; set; } = "";
    }

    private sealed class CdnBallot
    {
        public int index { get; set; }
        public string guid { get; set; } = "";
        public List<OnlineRawVote> Votes { get; set; } = new();
    }

    private sealed class JsonImportData
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

    private sealed class JsonElection
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

    private sealed class JsonLocation
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

    private sealed class JsonPerson
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

    private sealed class JsonBallot
    {
        public Guid BallotGuid { get; set; }
        public Guid LocationGuid { get; set; }
        public string? StatusCode { get; set; }
        public string? ComputerCode { get; set; }
        public int BallotNumAtComputer { get; set; }
        public string? Teller1 { get; set; }
        public string? Teller2 { get; set; }
        public List<JsonVote> votes { get; } = new();
    }

    private sealed class JsonVote
    {
        public int PositionOnBallot { get; }
        public Guid? PersonGuid { get; }
        public string? VoteStatus { get; }
        public string? IneligibleReasonCode { get; }
        public int? SingleNameElectionCount { get; }
        public string? PersonCombinedInfo { get; }
        public string? OnlineVoteRaw { get; }
    }

    private sealed class JsonTeller
    {
        public string? Name { get; }
        public bool? IsHeadTeller { get; }
        public string? UsingComputerCode { get; }
    }

    private sealed class JsonResult
    {
        public Guid PersonGuid { get; }
        public int? VoteCount { get; }
        public int Rank { get; }
        public string? Section { get; }
        public bool? IsTied { get; }
        public bool? IsTieResolved { get; }
        public int? TieBreakGroup { get; }
        public bool? CloseToPrev { get; }
        public bool? CloseToNext { get; }
        public int? RankInExtra { get; }
        public bool? TieBreakRequired { get; }
        public int? TieBreakCount { get; }
        public bool? ForceShowInOther { get; }
    }

    private sealed class JsonResultSummary
    {
        public string? ResultType { get; }
        public int? NumVoters { get; }
        public int? NumEligibleToVote { get; }
        public int? BallotsReceived { get; }
        public int? InPersonBallots { get; }
        public int? DroppedOffBallots { get; }
        public int? MailedInBallots { get; }
        public int? CalledInBallots { get; }
        public int? OnlineBallots { get; }
        public int? ImportedBallots { get; }
        public int? Custom1Ballots { get; }
        public int? Custom2Ballots { get; }
        public int? Custom3Ballots { get; }
        public int? BallotsNeedingReview { get; }
        public int? SpoiledBallots { get; }
        public int? SpoiledVotes { get; }
        public int? TotalVotes { get; }
        public bool? UseOnReports { get; }
        public int? SpoiledManualBallots { get; }
    }

    private sealed class JsonResultTie
    {
        public int TieBreakGroup { get; }
        public int NumInTie { get; }
        public bool IsResolved { get; }
        public bool? TieBreakRequired { get; }
        public int? NumToElect { get; }
    }

    private sealed class JsonOnlineVotingInfo
    {
        public Guid PersonGuid { get; }
        public string? WhenBallotCreated { get; }
        public string? Status { get; }
        public string? WhenStatus { get; }
        public string? ListPool { get; }
        public bool? PoolLocked { get; }
        public string? HistoryStatus { get; }
        public bool? NotifiedAboutOpening { get; }
    }

    private sealed class JsonLog
    {
        public string? AsOf { get; }
        public Guid? LocationGuid { get; }
        public string? VoterId { get; }
        public string? ComputerCode { get; }
        public string? Details { get; }
    }
}
