using Backend.Domain.Enumerations;

namespace Backend.DTOs.Reports;

public class ReportListItemDto
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
}

public class MainReportDto
{
    public string ElectionName { get; set; } = "";
    public string? Convenor { get; set; }
    public DateTimeOffset? DateOfElection { get; set; }
    public int NumEligibleToVote { get; set; }
    public int SumOfEnvelopesCollected { get; set; }
    public int NumBallotsWithManual { get; set; }
    public double PercentParticipation { get; set; }
    public int InPersonBallots { get; set; }
    public int MailedInBallots { get; set; }
    public int DroppedOffBallots { get; set; }
    public int OnlineBallots { get; set; }
    public int ImportedBallots { get; set; }
    public int CalledInBallots { get; set; }
    public int Custom1Ballots { get; set; }
    public int Custom2Ballots { get; set; }
    public int Custom3Ballots { get; set; }
    public string? Custom1Name { get; set; }
    public string? Custom2Name { get; set; }
    public string? Custom3Name { get; set; }
    public int SpoiledBallots { get; set; }
    public int SpoiledVotes { get; set; }
    public List<SpoiledBallotGroupDto> SpoiledBallotReasons { get; set; } = new();
    public List<SpoiledVoteGroupDto> SpoiledVoteReasons { get; set; } = new();
    public List<ElectedPersonDto> Elected { get; set; } = new();
    public bool HasTies { get; set; }
}

public class SpoiledBallotGroupDto
{
    public string Reason { get; set; } = "";
    public int BallotCount { get; set; }
}

public class SpoiledVoteGroupDto
{
    public string Reason { get; set; } = "";
    public int VoteCount { get; set; }
}

public class ElectedPersonDto
{
    public string Rank { get; set; } = "";
    public string Name { get; set; } = "";
    public string? BahaiId { get; set; }
    public string VoteCountDisplay { get; set; } = "";
    public ResultSection SectionCode { get; set; }
}

public class VotesByNumDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public List<VotePersonDto> People { get; set; } = new();
}

public class VotePersonDto
{
    public string PersonName { get; set; } = "";
    public int VoteCount { get; set; }
    public int? TieBreakCount { get; set; }
    public bool TieBreakRequired { get; set; }
    public ResultSection SectionCode { get; set; }
    public bool ShowBreak { get; set; }
}

public class VotesByNameDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public List<VotePersonDto> People { get; set; } = new();
}

public class BallotReportItemDto
{
    public string BallotCode { get; set; } = "";
    public string Location { get; set; } = "";
    public bool IsOnline { get; set; }
    public bool IsImported { get; set; }
    public int BallotId { get; set; }
    public int LocationId { get; set; }
    public string StatusCode { get; set; } = "";
    public bool Spoiled { get; set; }
    public List<BallotVoteDto> Votes { get; set; } = new();
}

public class BallotVoteDto
{
    public string PersonName { get; set; } = "";
    public int? SingleNameElectionCount { get; set; }
    public string? OnlineVoteRaw { get; set; }
    public bool Spoiled { get; set; }
    public bool TieBreakRequired { get; set; }
    public string? InvalidReasonDesc { get; set; }
}

public class BallotsReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public bool IsSingleNameElection { get; set; }
    public List<BallotReportItemDto> Ballots { get; set; } = new();
}

public class SpoiledVotesReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public List<SpoiledVoteItemDto> People { get; set; } = new();
}

public class SpoiledVoteItemDto
{
    public string PersonName { get; set; } = "";
    public int VoteCount { get; set; }
    public string InvalidReasonDesc { get; set; } = "";
}

public class BallotAlignmentReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public int NumToElect { get; set; }
    public bool IsSingleNameElection { get; set; }
    public List<AlignmentRowDto> Rows { get; set; } = new();
}

public class AlignmentRowDto
{
    public int MatchingNames { get; set; }
    public int BallotCount { get; set; }
}

public class BallotsSameReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public bool IsSingleNameElection { get; set; }
    public List<DuplicateGroupDto> Groups { get; set; } = new();
}

public class DuplicateGroupDto
{
    public int GroupNumber { get; set; }
    public List<BallotReportItemDto> Ballots { get; set; } = new();
}

public class BallotsSummaryReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public List<BallotSummaryItemDto> Ballots { get; set; } = new();
}

public class BallotSummaryItemDto
{
    public string BallotCode { get; set; } = "";
    public string Location { get; set; } = "";
    public int LocationId { get; set; }
    public int BallotId { get; set; }
    public string StatusCode { get; set; } = "";
    public bool Spoiled { get; set; }
    public int SpoiledVotes { get; set; }
    public string? Teller1 { get; set; }
    public string? Teller2 { get; set; }
}

public class AllCanReceiveReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public List<string> People { get; set; } = new();
}

public class VotersReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public bool HasMultipleLocations { get; set; }
    public int TotalCount { get; set; }
    public List<VoterItemDto> People { get; set; } = new();
}

public class VoterItemDto
{
    public string PersonName { get; set; } = "";
    public string VotingMethod { get; set; } = "-";
    public string? BahaiId { get; set; }
    public string? Location { get; set; }
    public DateTimeOffset? RegistrationTime { get; set; }
    public string? Teller1 { get; set; }
    public string? Teller2 { get; set; }
    public string? RegistrationLog { get; set; }
}

public class FlagsReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public bool HasMultipleLocations { get; set; }
    public List<string> FlagNames { get; set; } = new();
    public List<FlagPersonDto> People { get; set; } = new();
}

public class FlagPersonDto
{
    public int RowId { get; set; }
    public string PersonName { get; set; } = "";
    public string? Location { get; set; }
    public List<string> Flags { get; set; } = new();
}

public class VotersOnlineReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public List<OnlineVoterItemDto> People { get; set; } = new();
}

public class OnlineVoterItemDto
{
    public int PersonId { get; set; }
    public string FullName { get; set; } = "";
    public string VotingMethodDisplay { get; set; } = "-";
    public string? Status { get; set; }
    public DateTimeOffset? WhenStatus { get; set; }
    public string? Email { get; set; }
    public DateTimeOffset? WhenEmail { get; set; }
    public string? Phone { get; set; }
    public DateTimeOffset? WhenPhone { get; set; }
}

public class VotersByAreaReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public string? Custom1Name { get; set; }
    public string? Custom2Name { get; set; }
    public string? Custom3Name { get; set; }
    public List<AreaRowDto> Areas { get; set; } = new();
    public AreaRowDto Total { get; set; } = new();
}

public class AreaRowDto
{
    public string AreaName { get; set; } = "";
    public int TotalEligible { get; set; }
    public int Voted { get; set; }
    public int InPerson { get; set; }
    public int MailedIn { get; set; }
    public int DroppedOff { get; set; }
    public int CalledIn { get; set; }
    public int Custom1 { get; set; }
    public int Custom2 { get; set; }
    public int Custom3 { get; set; }
    public int Online { get; set; }
    public int OnlineKiosk { get; set; }
    public int Imported { get; set; }
}

public class VotersByLocationReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public string? Custom1Name { get; set; }
    public string? Custom2Name { get; set; }
    public string? Custom3Name { get; set; }
    public List<LocationRowDto> Locations { get; set; } = new();
    public LocationRowDto Total { get; set; } = new();
}

public class LocationRowDto
{
    public string LocationName { get; set; } = "";
    public int TotalVoters { get; set; }
    public int InPerson { get; set; }
    public int MailedIn { get; set; }
    public int DroppedOff { get; set; }
    public int CalledIn { get; set; }
    public int Custom1 { get; set; }
    public int Custom2 { get; set; }
    public int Custom3 { get; set; }
    public int Online { get; set; }
    public int OnlineKiosk { get; set; }
    public int Imported { get; set; }
}

public class VotersByLocationAreaReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public List<LocationAreaGroupDto> Locations { get; set; } = new();
}

public class LocationAreaGroupDto
{
    public string LocationName { get; set; } = "";
    public List<AreaCountDto> Areas { get; set; } = new();
    public int TotalCount { get; set; }
}

public class AreaCountDto
{
    public string AreaName { get; set; } = "";
    public int Count { get; set; }
}

public class ChangedPeopleReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public List<ChangedPersonDto> People { get; set; } = new();
}

public class ChangedPersonDto
{
    public string Change { get; set; } = "";
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? OtherNames { get; set; }
    public string? OtherLastNames { get; set; }
    public string? OtherInfo { get; set; }
    public string? BahaiId { get; set; }
    public bool CanVote { get; set; }
    public bool CanReceiveVotes { get; set; }
    public string? InvalidReasonDesc { get; set; }
}

public class AllNonEligibleReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public List<NonEligiblePersonDto> People { get; set; } = new();
}

public class NonEligiblePersonDto
{
    public string PersonName { get; set; } = "";
    public bool CanReceiveVotes { get; set; }
    public bool CanVote { get; set; }
    public string? InvalidReasonDesc { get; set; }
    public string? VotingMethod { get; set; }
}

public class VoterEmailsReportDto
{
    public string ElectionName { get; set; } = "";
    public DateTimeOffset? DateOfElection { get; set; }
    public List<VoterEmailItemDto> People { get; set; } = new();
}

public class VoterEmailItemDto
{
    public string FullName { get; set; } = "";
    public string? BahaiId { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public bool CanVote { get; set; }
    public bool HasSignedInEmail { get; set; }
    public bool HasSignedInPhone { get; set; }
    public string? VotingMethod { get; set; }
}
