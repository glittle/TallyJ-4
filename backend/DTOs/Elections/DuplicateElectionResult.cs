namespace Backend.DTOs.Elections;

/// <summary>
/// Result of duplicating an election as a test copy.
/// </summary>
public class DuplicateElectionResult
{
    public ElectionDto? Election { get; init; }

    /// <summary>
    /// Phrase key when a short teller passcode was not copied onto the new election.
    /// </summary>
    public string? Warning { get; init; }

    public bool IsNotFound { get; init; }

    public bool IsForbidden { get; init; }

    public bool IsSuccess => Election != null && !IsNotFound && !IsForbidden;

    public static DuplicateElectionResult Success(ElectionDto election, string? warning = null) =>
        new() { Election = election, Warning = warning };

    public static DuplicateElectionResult NotFound() =>
        new() { IsNotFound = true };

    public static DuplicateElectionResult Forbidden() =>
        new() { IsForbidden = true };
}
