namespace Backend.DTOs.Elections;

/// <summary>
/// Result of an election stage change attempt.
/// </summary>
public class ChangeElectionStageResult
{
    public ElectionDto? Election { get; init; }

    public string? ErrorMessage { get; init; }

    public bool RequiresConfirmation { get; init; }

    public string? ConfirmationReason { get; init; }

    public bool IsNotFound => Election == null && ErrorMessage == null && !RequiresConfirmation;

    public bool IsSuccess => Election != null && ErrorMessage == null && !RequiresConfirmation;

    public static ChangeElectionStageResult NotFound() => new();

    public static ChangeElectionStageResult Success(ElectionDto election) =>
        new() { Election = election };

    public static ChangeElectionStageResult InvalidTransition(string message) =>
        new() { ErrorMessage = message };

    public static ChangeElectionStageResult ConfirmationRequired(string reason) =>
        new() { RequiresConfirmation = true, ConfirmationReason = reason };
}