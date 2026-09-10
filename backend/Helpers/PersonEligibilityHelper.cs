using Backend.Entities;
using Backend.Enumerations;

namespace Backend.Helpers;

/// <summary>
/// Resolves effective vote / receive eligibility when <see cref="Person.CanVote"/> /
/// <see cref="Person.CanReceiveVotes"/> are null (legacy or partially imported rows).
/// Null flags mean "unset", not "ineligible" — the people list UI already treats only
/// an explicit reason / false as ineligible.
/// </summary>
public static class PersonEligibilityHelper
{
    /// <summary>
    /// Whether this person may appear on a ballot as a candidate (receive votes).
    /// </summary>
    public static bool CanReceiveVotes(Person? person)
    {
        if (person is null)
        {
            return false;
        }

        if (person.CanReceiveVotes.HasValue)
        {
            return person.CanReceiveVotes.Value;
        }

        if (!string.IsNullOrEmpty(person.IneligibleReasonCode))
        {
            return IneligibleReasonEnum.GetByCode(person.IneligibleReasonCode)?.CanReceiveVotes ?? false;
        }

        return true;
    }

    /// <summary>
    /// Whether this person may cast a ballot (vote).
    /// </summary>
    public static bool CanVote(Person? person)
    {
        if (person is null)
        {
            return false;
        }

        if (person.CanVote.HasValue)
        {
            return person.CanVote.Value;
        }

        if (!string.IsNullOrEmpty(person.IneligibleReasonCode))
        {
            return IneligibleReasonEnum.GetByCode(person.IneligibleReasonCode)?.CanVote ?? false;
        }

        return true;
    }

    /// <summary>
    /// Front Desk check-in (<see cref="Person.VotingMethod"/>) or an accepted online
    /// ballot (<see cref="Person.HasOnlineBallot"/>) is the v4 record that this person
    /// has already voted. Pending online rows do not set <see cref="Person.HasOnlineBallot"/>.
    /// </summary>
    public static bool HasAcceptedBallot(Person? person)
    {
        if (person is null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(person.VotingMethod) || person.HasOnlineBallot == true;
    }

    /// <summary>
    /// True when the reason would set <see cref="Person.CanVote"/> to false
    /// (X/R groups, or an unknown non-empty code). Empty/null is fully eligible.
    /// </summary>
    public static bool ReasonRemovesVoteEligibility(string? ineligibleReasonCode)
    {
        if (string.IsNullOrWhiteSpace(ineligibleReasonCode))
        {
            return false;
        }

        var reason = IneligibleReasonEnum.GetByCode(ineligibleReasonCode);
        return reason is null || !reason.CanVote;
    }
}
