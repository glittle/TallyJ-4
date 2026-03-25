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

public class TallyJv2ElectionImportService : ElectionImportExportBase
{
    private const string LocationGuidAttribute = "LocationGuid";
    private const string PersonGuidAttribute = "PersonGuid";

    public TallyJv2ElectionImportService(MainDbContext context, IElectionService electionService)
        : base(context, electionService)
    {
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
        if (string.IsNullOrWhiteSpace(attr))
        {
            throw new InvalidOperationException($"Missing required '{LocationGuidAttribute}' attribute on location node for ballot loading");
        }
        var oldLocationGuid = Guid.Parse(attr);
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
            foreach (XmlElement rNode in resultNodes)
            {
                var result = new Result
                {
                    ElectionGuid = electionGuid,
                    PersonGuid = guidMap[ParseGuid(rNode.GetAttribute(PersonGuidAttribute)) ?? Guid.Empty],
                    VoteCount = ParseInt(rNode.GetAttribute("VoteCount")),
                    Rank = ParseInt(rNode.GetAttribute("Rank")) ?? 0,
                    Section = rNode.GetAttribute("Section") ?? "T",
                    IsTied = ParseBool(rNode.GetAttribute("IsTied")),
                    IsTieResolved = ParseBool(rNode.GetAttribute("IsTieResolved")),
                    TieBreakGroup = ParseInt(rNode.GetAttribute("TieBreakGroup")),
                    CloseToPrev = ParseBool(rNode.GetAttribute("CloseToPrev")),
                    CloseToNext = ParseBool(rNode.GetAttribute("CloseToNext")),
                    RankInExtra = ParseInt(rNode.GetAttribute("RankInExtra")),
                    TieBreakRequired = ParseBool(rNode.GetAttribute("TieBreakRequired")),
                    TieBreakCount = ParseInt(rNode.GetAttribute("TieBreakCount")),
                    ForceShowInOther = ParseBool(rNode.GetAttribute("ForceShowInOther"))
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
                    IsResolved = ParseBool(rtNode.GetAttribute("IsResolved")),
                    TieBreakRequired = ParseBool(rtNode.GetAttribute("TieBreakRequired")),
                    NumToElect = ParseInt(rtNode.GetAttribute("NumToElect")) ?? 0
                };
                _context.ResultTies.Add(resultTie);
            }
        }
    }

    private void ImportOnlineVotingInfoFromXml(XmlElement root, XmlNamespaceManager nsm, Guid electionGuid, Dictionary<Guid, Guid> guidMap)
    {
        var oviNodes = root.SelectNodes("t:onlineVotingInfo", nsm);
        if (oviNodes != null)
        {
            foreach (XmlElement oviNode in oviNodes)
            {
                var ovi = new OnlineVotingInfo
                {
                    ElectionGuid = electionGuid,
                    PersonGuid = guidMap[ParseGuid(oviNode.GetAttribute(PersonGuidAttribute)) ?? Guid.Empty],
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
}