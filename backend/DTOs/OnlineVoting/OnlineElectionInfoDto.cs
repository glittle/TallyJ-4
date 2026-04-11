namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// Data transfer object containing information about an online election.
/// </summary>
public class OnlineElectionInfoDto
{
    /// <summary>
    /// The unique identifier of the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the election.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The name of the election convenor.
    /// </summary>
    public string? Convenor { get; set; }

    /// <summary>
    /// The date when the election will be held.
    /// </summary>
    public DateTimeOffset? DateOfElection { get; set; }

    /// <summary>
    /// The number of positions to be elected.
    /// </summary>
    public int? NumberToElect { get; set; }

    /// <summary>
    /// The timestamp when online voting opens.
    /// </summary>
    public DateTimeOffset? OnlineWhenOpen { get; set; }

    /// <summary>
    /// The timestamp when online voting closes.
    /// </summary>
    public DateTimeOffset? OnlineWhenClose { get; set; }

    /// <summary>
    /// Indicates whether online voting is currently open.
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    /// Instructions for voters.
    /// </summary>
    public string? Instructions { get; set; }

    /// <summary>
    /// The name selection process mode: A = List only, B = Random (free text), C = Both (list + free entry).
    /// </summary>
    public string? OnlineSelectionProcess { get; set; }
}



