namespace Backend.Enumerations;

/// <summary>
/// i18n phrase keys returned to the client for people-record errors.
/// Values match keys in frontend/src/locales/en/people.json.
/// </summary>
public static class PeopleMessageKeys
{
    /// <summary>
    /// A person who has already voted (voting method or accepted online ballot)
    /// cannot be given an eligibility reason that removes the right to vote.
    /// </summary>
    public const string CannotMarkCannotVoteAfterVoted = "people.cannotMarkCannotVoteAfterVoted";
}
