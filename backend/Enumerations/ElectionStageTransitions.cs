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
    /// Validates whether a stage change is allowed. Any move to a different known stage is permitted.
    /// </summary>
    public static bool CanTransition(ElectionStage current, ElectionStage target, out string reason)
    {
        reason = string.Empty;

        if (current == target)
        {
            reason = ElectionStageMessageKeys.AlreadyInStage;
            return false;
        }

        var currentIndex = Array.IndexOf(OrderedStages, current);
        var targetIndex = Array.IndexOf(OrderedStages, target);

        if (currentIndex < 0 || targetIndex < 0)
        {
            reason = ElectionStageMessageKeys.InvalidStage;
            return false;
        }

        return true;
    }
}