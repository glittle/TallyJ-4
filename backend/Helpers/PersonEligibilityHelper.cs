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
}
