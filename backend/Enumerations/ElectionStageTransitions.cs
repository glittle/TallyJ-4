namespace Backend.Enumerations;

/// <summary>
/// Centralized election stage ordering and transition guards.
/// </summary>
public static class ElectionStageTransitions
{
    private static readonly ElectionStage[] OrderedStages =
    [
        ElectionStage.SettingUp,
        ElectionStage.GatheringBallots,
        ElectionStage.ProcessingBallots,
        ElectionStage.Finalized
    ];

    /// <summary>
    /// Validates whether a stage change is allowed. Reverts to any earlier stage are permitted;
    /// forward moves must advance exactly one step.
    /// </summary>
    public static bool CanTransition(ElectionStage current, ElectionStage target, out string reason)
    {
        reason = string.Empty;

        if (current == target)
        {
            reason = "Election is already in the requested stage";
            return false;
        }

        var currentIndex = Array.IndexOf(OrderedStages, current);
        var targetIndex = Array.IndexOf(OrderedStages, target);

        if (currentIndex < 0 || targetIndex < 0)
        {
            reason = "Invalid election stage";
            return false;
        }

        if (targetIndex < currentIndex)
        {
            return true;
        }

        if (targetIndex == currentIndex + 1)
        {
            return true;
        }

        reason = $"Cannot skip from {current} directly to {target}";
        return false;
    }
}