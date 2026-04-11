namespace Backend.DTOs.Results;

/// <summary>
/// Data transfer object containing real-time monitoring information for an election.
/// </summary>
public class MonitorInfoDto
{
    /// <summary>
    /// The unique identifier of the election being monitored.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// List of computers participating in the election with their status information.
    /// </summary>
    public List<ComputerInfoDto> Computers { get; set; } = new();

    /// <summary>
    /// List of voting locations with their current statistics.
    /// </summary>
    public List<LocationInfoDto> Locations { get; set; } = new();

    /// <summary>
    /// Information about online voting status and statistics.
    /// </summary>
    public OnlineVotingInfoDto OnlineVotingInfo { get; set; } = new();

    /// <summary>
    /// Total number of ballots cast across all locations and online voting.
    /// </summary>
    public int TotalBallots { get; set; }

    /// <summary>
    /// Total number of votes recorded across all ballots.
    /// </summary>
    public int TotalVotes { get; set; }

    /// <summary>
    /// Timestamp of the last update to this monitoring information.
    /// </summary>
    public DateTimeOffset LastUpdated { get; set; }
}

/// <summary>
/// Data transfer object containing information about a computer participating in the election.
/// </summary>
public class ComputerInfoDto
{
    /// <summary>
    /// Unique code identifying the computer.
    /// </summary>
    public string ComputerCode { get; set; } = string.Empty;

    /// <summary>
    /// Name of the location where this computer is deployed.
    /// </summary>
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// Number of ballots processed by this computer.
    /// </summary>
    public int BallotCount { get; set; }

    /// <summary>
    /// Timestamp of the last contact from this computer.
    /// </summary>
    public DateTimeOffset LastContact { get; set; }

    /// <summary>
    /// Current status of the computer ("Active", "Inactive", "Offline").
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object containing information about a voting location.
/// </summary>
public class LocationInfoDto
{
    /// <summary>
    /// Unique identifier of the voting location.
    /// </summary>
    public Guid LocationGuid { get; set; }

    /// <summary>
    /// Name of the voting location.
    /// </summary>
    public string LocationName { get; set; } = string.Empty;

    /// <summary>
    /// Number of ballots cast at this location.
    /// </summary>
    public int BallotCount { get; set; }

    /// <summary>
    /// Total number of votes recorded at this location.
    /// </summary>
    public int VoteCount { get; set; }

    /// <summary>
    /// Number of registered voters at this location.
    /// </summary>
    public int VoterCount { get; set; }

    /// <summary>
    /// Current status of the location.
    /// </summary>
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Data transfer object containing information about online voting status and statistics.
/// </summary>
public class OnlineVotingInfoDto
{
    /// <summary>
    /// Total number of online ballots submitted.
    /// </summary>
    public int TotalOnlineBallots { get; set; }

    /// <summary>
    /// Number of online ballots that have been processed.
    /// </summary>
    public int ProcessedOnlineBallots { get; set; }

    /// <summary>
    /// Number of online ballots that are still pending processing.
    /// </summary>
    public int PendingOnlineBallots { get; set; }

    /// <summary>
    /// Indicates whether online voting is currently enabled.
    /// </summary>
    public bool OnlineVotingEnabled { get; set; }

    /// <summary>
    /// Start date and time for online voting period.
    /// </summary>
    public DateTimeOffset? OnlineVotingStart { get; set; }

    /// <summary>
    /// End date and time for online voting period.
    /// </summary>
    public DateTimeOffset? OnlineVotingEnd { get; set; }
}


