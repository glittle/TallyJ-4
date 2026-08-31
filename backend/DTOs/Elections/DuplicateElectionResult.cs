namespace Backend.DTOs.Elections;

/// <summary>
/// Result of duplicating an election as a test copy.
/// </summary>
public class DuplicateElectionResult
{
    public ElectionDto? Election { get; init; }

    public bool IsNotFound { get; init; }

    public bool IsForbidden { get; init; }

    public bool IsSuccess => Election != null && !IsNotFound && !IsForbidden;

    public static DuplicateElectionResult Success(ElectionDto election) =>
        new() { Election = election };

    public static DuplicateElectionResult NotFound() =>
        new() { IsNotFound = true };

    public static DuplicateElectionResult Forbidden() =>
        new() { IsForbidden = true };
}
