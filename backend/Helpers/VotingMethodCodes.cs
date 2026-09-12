namespace Backend.Helpers;

/// <summary>
/// Single-letter codes stored on <see cref="Entities.Person.VotingMethod"/>
/// (varchar(1)). Election setup may store the same letters concatenated
/// (<c>PMD</c>) or comma-separated aliases (<c>IP,OL</c>).
/// </summary>
public static class VotingMethodCodes
{
    public const string InPerson = "P";
    public const string Mailed = "M";
    public const string DroppedOff = "D";
    public const string CalledIn = "C";
    public const string Online = "O";
    public const string Kiosk = "K";
    public const string Imported = "I";
    public const string Custom1 = "1";
    public const string Custom2 = "2";
    public const string Custom3 = "3";

    /// <summary>
    /// Front Desk / imported methods that are a different counted path from
    /// voter-initiated online. Online (<c>O</c>) is the same path as
    /// <c>OnlineVotingInfo</c>. Kiosk is listed separately from Online on
    /// monitor/reports but is still a Front Desk-recorded method.
    /// </summary>
    private static readonly HashSet<string> RecordedOtherThanOnline = new(StringComparer.Ordinal)
    {
        InPerson, Mailed, DroppedOff, CalledIn, Imported, Kiosk, Custom1, Custom2, Custom3
    };

    /// <summary>
    /// Paper and imported methods that, together with a Processed online row,
    /// mean two counted voting paths for one person.
    /// </summary>
    private static readonly HashSet<string> PaperOrImported = new(StringComparer.Ordinal)
    {
        InPerson, Mailed, DroppedOff, CalledIn, Imported, Custom1, Custom2, Custom3
    };

    private static readonly Dictionary<string, string> ElectionMethodAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["IP"] = InPerson,
        ["IN"] = InPerson,
        ["OL"] = Online,
        ["ON"] = Online,
        ["IM"] = Imported,
        ["KI"] = Kiosk,
        ["MA"] = Mailed,
        ["DO"] = DroppedOff,
        ["DR"] = DroppedOff,
        ["CA"] = CalledIn
    };

    /// <summary>
    /// Default Front Desk methods when election setup has no <c>VotingMethods</c>.
    /// </summary>
    public static readonly IReadOnlyList<string> DefaultFrontDeskMethods =
        [InPerson, Mailed, DroppedOff];

    public static bool IsRecordedOtherThanOnline(string? votingMethod) =>
        !string.IsNullOrEmpty(votingMethod) && RecordedOtherThanOnline.Contains(votingMethod);

    public static bool IsPaperOrImported(string? votingMethod) =>
        !string.IsNullOrEmpty(votingMethod) && PaperOrImported.Contains(votingMethod);

    public static bool IsOnline(string? votingMethod) =>
        string.Equals(votingMethod, Online, StringComparison.Ordinal);

    public static bool IsKiosk(string? votingMethod) =>
        string.Equals(votingMethod, Kiosk, StringComparison.Ordinal);

    /// <summary>
    /// Parse <c>Election.VotingMethods</c> into Person.VotingMethod letters,
    /// preserving first-seen order. Unknown tokens are skipped.
    /// </summary>
    public static IReadOnlyList<string> ParseElectionVotingMethods(string? votingMethods)
    {
        if (string.IsNullOrWhiteSpace(votingMethods))
        {
            return DefaultFrontDeskMethods;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();

        if (votingMethods.Contains(','))
        {
            foreach (var raw in votingMethods.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                TryAddParsedToken(raw, seen, result);
            }
        }
        else
        {
            var trimmed = votingMethods.Trim();
            if (trimmed.Length == 2 && ElectionMethodAliases.TryGetValue(trimmed, out var mapped))
            {
                AddOnce(mapped, seen, result);
            }
            else
            {
                foreach (var ch in trimmed)
                {
                    TryAddParsedToken(ch.ToString(), seen, result);
                }
            }
        }

        return result.Count > 0 ? result : DefaultFrontDeskMethods;
    }

    public static bool ElectionSupportsKiosk(string? votingMethods) =>
        ParseElectionVotingMethods(votingMethods).Contains(Kiosk);

    /// <summary>
    /// Count people by recorded method. Processed online without a
    /// recorded-other-than-online method counts as Online (v4 Accept-all
    /// does not set <c>VotingMethod</c>).
    /// </summary>
    public static VotingMethodBreakdown Count(
        IEnumerable<(string? VotingMethod, bool HasProcessedOnline)> people)
    {
        var breakdown = new VotingMethodBreakdown();
        foreach (var (method, processed) in people)
        {
            if (string.Equals(method, InPerson, StringComparison.Ordinal))
            {
                breakdown.InPerson++;
            }
            else if (string.Equals(method, Mailed, StringComparison.Ordinal))
            {
                breakdown.Mailed++;
            }
            else if (string.Equals(method, DroppedOff, StringComparison.Ordinal))
            {
                breakdown.DroppedOff++;
            }
            else if (string.Equals(method, CalledIn, StringComparison.Ordinal))
            {
                breakdown.CalledIn++;
            }
            else if (string.Equals(method, Imported, StringComparison.Ordinal))
            {
                breakdown.Imported++;
            }
            else if (string.Equals(method, Custom1, StringComparison.Ordinal))
            {
                breakdown.Custom1++;
            }
            else if (string.Equals(method, Custom2, StringComparison.Ordinal))
            {
                breakdown.Custom2++;
            }
            else if (string.Equals(method, Custom3, StringComparison.Ordinal))
            {
                breakdown.Custom3++;
            }
            else if (IsKiosk(method))
            {
                breakdown.Kiosk++;
            }
            else if (IsOnline(method) || (processed && !IsRecordedOtherThanOnline(method)))
            {
                breakdown.Online++;
            }
        }

        return breakdown;
    }

    public static bool HasVotedForCounts(string? votingMethod, bool hasProcessedOnline) =>
        !string.IsNullOrEmpty(votingMethod) || hasProcessedOnline;

    private static void TryAddParsedToken(string token, HashSet<string> seen, List<string> result)
    {
        if (ElectionMethodAliases.TryGetValue(token, out var mapped))
        {
            AddOnce(mapped, seen, result);
            return;
        }

        if (token.Length != 1)
        {
            return;
        }

        var code = char.IsDigit(token[0]) ? token : token.ToUpperInvariant();
        if (IsKnownCode(code))
        {
            AddOnce(code, seen, result);
        }
    }

    private static bool IsKnownCode(string code) =>
        code is InPerson or Mailed or DroppedOff or CalledIn or Online or Kiosk or Imported
            or Custom1 or Custom2 or Custom3;

    private static void AddOnce(string code, HashSet<string> seen, List<string> result)
    {
        if (seen.Add(code))
        {
            result.Add(code);
        }
    }
}

/// <summary>
/// Ballot / voter counts by voting method. Online is voter-initiated
/// (processed or <c>VotingMethod</c> O). Kiosk is Front Desk <c>K</c>.
/// </summary>
public sealed class VotingMethodBreakdown
{
    public int InPerson { get; set; }
    public int Mailed { get; set; }
    public int DroppedOff { get; set; }
    public int CalledIn { get; set; }
    public int Kiosk { get; set; }
    public int Online { get; set; }
    public int Imported { get; set; }
    public int Custom1 { get; set; }
    public int Custom2 { get; set; }
    public int Custom3 { get; set; }
}
