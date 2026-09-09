namespace Backend.Enumerations;

/// <summary>
/// i18n phrase keys returned to the client for election stage errors.
/// Values match keys in frontend/src/locales/en/elections.json
/// and frontend/src/locales/en/voting.json.
/// </summary>
public static class ElectionStageMessageKeys
{
    public const string AlreadyInStage = "elections.stageChangeError.alreadyInStage";
    public const string InvalidStage = "elections.stageChangeError.invalidStage";
    public const string NotFound = "elections.stageChangeError.notFound";
    public const string ConfirmLeaveFinalized = "elections.stageChangeError.confirmLeaveFinalized";
    public const string FinalizedWriteBlocked = "elections.finalizedWriteBlocked";
    public const string FinalizedOnlineSubmit = "voting.submit.finalized";
    public const string AnalysisNotCompleted = "elections.stageChangeError.analysisNotCompleted";
    public const string AnalysisNotReady = "elections.stageChangeError.analysisNotReady";
    public const string UnresolvedTies = "elections.stageChangeError.unresolvedTies";
    public const string BallotsNeedReview = "elections.stageChangeError.ballotsNeedReview";
    public const string BallotsOutstanding = "elections.stageChangeError.ballotsOutstanding";
    public const string CountsDoNotReconcile = "elections.stageChangeError.countsDoNotReconcile";
    public const string OnlineVotingStillOpen = "elections.stageChangeError.onlineVotingStillOpen";

    /// <summary>
    /// Builds a phrase key with interpolation parameters (e.g. key|count=3).
    /// </summary>
    public static string WithParam(string key, string paramName, int value) =>
        $"{key}|{paramName}={value}";
}