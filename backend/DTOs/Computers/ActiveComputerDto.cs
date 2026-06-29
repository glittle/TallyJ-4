namespace Backend.DTOs.Computers;

/// <summary>
/// Represents a teller workstation currently connected to an election.
/// </summary>
public class ActiveComputerDto
{
    public string ComputerCode { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public bool IsMainTeller { get; set; }

    public DateTimeOffset ConnectedAt { get; set; }
}