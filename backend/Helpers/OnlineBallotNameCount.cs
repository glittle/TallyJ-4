using Backend.DTOs.OnlineVoting;
using Backend.Enumerations;

namespace Backend.Helpers;

/// <summary>
/// How many distinct valid names an online ballot contains.
/// Paper, imported, and tally "Too Few" rules do not use this.
/// </summary>
public static class OnlineBallotNameCount
{
    public const string ExactCountError = "voting.submit.exactCount";

    /// <summary>
    /// Random (B) and Both (C) let the voter type a name. List (A), and a
    /// missing code (the ballot page treats that as List), do not.
    /// </summary>
    public static bool AllowsWriteIn(string? selectionProcess)
    {
        return selectionProcess == OnlineSelectionProcessEnum.Random.Code
               || selectionProcess == OnlineSelectionProcessEnum.Both.Code;
    }

    /// <summary>
    /// Counts names that can fill a slot. An eligible person id counts once.
    /// The same display name does not count again. A typed name with no person
    /// id counts only when write-ins are allowed. A person id for someone who
    /// cannot receive votes does not count, even if a name was sent with it.
    /// </summary>
    public static int CountDistinctValidNames(
        IReadOnlyList<OnlineVoteDto>? votes,
        string? selectionProcess,
        IReadOnlyDictionary<Guid, string?> eligibleDisplayNames)
    {
        var writeIn = AllowsWriteIn(selectionProcess);
        var seenGuids = new HashSet<Guid>();
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        var count = 0;

        foreach (var vote in votes ?? [])
        {
            var submitted = NormalizeName(vote.VoteName);
            if (vote.PersonGuid is Guid personGuid)
            {
                if (!eligibleDisplayNames.TryGetValue(personGuid, out var displayName))
                {
                    continue;
                }

                if (!seenGuids.Add(personGuid))
                {
                    continue;
                }

                var name = submitted ?? NormalizeName(displayName);
                if (name != null && !seenNames.Add(name))
                {
                    continue;
                }

                count++;
                continue;
            }

            if (writeIn && submitted != null && seenNames.Add(submitted))
            {
                count++;
            }
        }

        return count;
    }

    public static string? NormalizeName(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var collapsed = string.Join(
            ' ',
            value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return collapsed.ToLowerInvariant();
    }
}
