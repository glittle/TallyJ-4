using Backend.Domain.Entities;
using Backend.Domain.Context;
using Backend.Models;
using System.Globalization;
using System.Xml;
using System.Xml.Schema;

namespace Backend.Services;

public abstract class ElectionImportExportBase
{
    protected readonly MainDbContext _context;
    protected readonly IElectionService _electionService;

    protected ElectionImportExportBase(MainDbContext context, IElectionService electionService)
    {
        _context = context;
        _electionService = electionService;
    }

    protected async Task AssociateUserWithElectionAsync(Guid electionGuid, Guid userId)
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

    protected static async Task<(List<string> errors, XmlDocument xmlDoc)> ValidateXmlAgainstSchemaAsync(Stream xmlStream, string schemaPath)
    {
        var xmlDoc = new XmlDocument();

        using (var reader = new StreamReader(xmlStream, System.Text.Encoding.UTF8, true, 1024, leaveOpen: true))
        {
            var xmlContent = await reader.ReadToEndAsync();
            xmlDoc.LoadXml(xmlContent);
        }

        // Reset stream position if seekable, so caller can read it again if needed
        if (xmlStream.CanSeek)
        {
            xmlStream.Position = 0;
        }

        xmlDoc.Schemas.Add("", schemaPath);
        var validationErrors = new List<string>();
        xmlDoc.Validate((sender, args) =>
        {
            if (args.Severity == XmlSeverityType.Error)
                validationErrors.Add(args.Message);
        });

        return (validationErrors, xmlDoc);
    }

    // Helper methods
    protected static DateTimeOffset? ParseDateTime(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : DateTimeOffset.Parse(value, CultureInfo.InvariantCulture);
    }

    protected static int? ParseInt(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : int.Parse(value);
    }

    protected static bool? ParseBool(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : bool.Parse(value);
    }

    protected static Guid? ParseGuid(string? value)
    {
        return string.IsNullOrEmpty(value) ? null : Guid.Parse(value);
    }

    // Data classes for import
    protected sealed class CdnVoter
    {
        public string bahaiid { get; set; } = "";
        public string firstname { get; set; } = "";
        public string lastname { get; set; } = "";
    }

    protected sealed class CdnBallot
    {
        public int index { get; set; }
        public string guid { get; set; } = "";
        public List<OnlineRawVote> Votes { get; set; } = new();
    }

    protected sealed class JsonImportData
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

    protected sealed class JsonElection
    {
        public Guid ElectionGuid { get; set; }
        public string? Name { get; set; }
        public string? Convenor { get; set; }
        public string? DateOfElection { get; set; }
        public string? ElectionType { get; set; }
        public string? ElectionMode { get; set; }
        public int? NumberToElect { get; set; }
        public int? NumberExtra { get; set; }
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

    protected sealed class JsonLocation
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

    protected sealed class JsonPerson
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

    protected sealed class JsonBallot
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

    protected sealed class JsonVote
    {
        public int PositionOnBallot { get; set; }
        public Guid? PersonGuid { get; set; }
        public string? VoteStatus { get; set; }
        public string? IneligibleReasonCode { get; set; }
        public int? SingleNameElectionCount { get; set; }
        public string? PersonCombinedInfo { get; set; }
        public string? OnlineVoteRaw { get; set; }
    }

    protected sealed class JsonTeller
    {
        public string? Name { get; set; }
        public bool? IsHeadTeller { get; set; }
        public string? UsingComputerCode { get; set; }
    }

    protected sealed class JsonResult
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

    protected sealed class JsonResultSummary
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

    protected sealed class JsonResultTie
    {
        public int TieBreakGroup { get; }
        public int? NumInTie { get; }
        public bool? IsResolved { get; }
        public bool? TieBreakRequired { get; }
        public int? NumToElect { get; }
    }

    protected sealed class JsonOnlineVotingInfo
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

    protected sealed class JsonLog
    {
        public string AsOf { get; set; } = "";
        public Guid? LocationGuid { get; set; }
        public string? VoterId { get; set; }
        public string? ComputerCode { get; set; }
        public string? Details { get; set; }
    }
}