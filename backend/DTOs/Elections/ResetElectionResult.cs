namespace Backend.DTOs.Elections;

/// <summary>
/// Result of resetting runtime data on a ShowAsTest election.
/// </summary>
public class ResetElectionResult
{
    public ElectionDto? Election { get; init; }

    public bool IsNotFound { get; init; }

    public bool IsForbidden { get; init; }

    /// <summary>
    /// True when the election exists but ShowAsTest is false or null.
    /// </summary>
    public bool IsNotTest { get; init; }

    public bool IsSuccess => Election != null && !IsNotFound && !IsForbidden && !IsNotTest;

    public static ResetElectionResult Success(ElectionDto election) =>
        new() { Election = election };

    public static ResetElectionResult NotFound() =>
        new() { IsNotFound = true };

    public static ResetElectionResult Forbidden() =>
        new() { IsForbidden = true };

    public static ResetElectionResult NotTest() =>
        new() { IsNotTest = true };
}
