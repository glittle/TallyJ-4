namespace TallyJ4.DTOs.Results;

public class MonitorInfoDto
{
    public Guid ElectionGuid { get; set; }
    public List<ComputerInfoDto> Computers { get; set; } = new();
    public List<LocationInfoDto> Locations { get; set; } = new();
    public OnlineVotingInfoDto OnlineVotingInfo { get; set; } = new();
    public int TotalBallots { get; set; }
    public int TotalVotes { get; set; }
    public DateTime LastUpdated { get; set; }
}

public class ComputerInfoDto
{
    public string ComputerCode { get; set; } = string.Empty;
    public string LocationName { get; set; } = string.Empty;
    public int BallotCount { get; set; }
    public DateTime LastContact { get; set; }
    public string Status { get; set; } = string.Empty; // "Active", "Inactive", "Offline"
}

public class LocationInfoDto
{
    public Guid LocationGuid { get; set; }
    public string LocationName { get; set; } = string.Empty;
    public int BallotCount { get; set; }
    public int VoteCount { get; set; }
    public int VoterCount { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class OnlineVotingInfoDto
{
    public int TotalOnlineBallots { get; set; }
    public int ProcessedOnlineBallots { get; set; }
    public int PendingOnlineBallots { get; set; }
    public bool OnlineVotingEnabled { get; set; }
    public DateTime? OnlineVotingStart { get; set; }
    public DateTime? OnlineVotingEnd { get; set; }
}