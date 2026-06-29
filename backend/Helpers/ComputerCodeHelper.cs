using System.Text.RegularExpressions;

namespace Backend.Helpers;

/// <summary>
/// Pure helpers for sequential letter-only computer codes (A-Z, then AA-ZZ).
/// </summary>
public static partial class ComputerCodeHelper
{
    private const int SingleLetterCount = 26;
    private const int MaxIndex = SingleLetterCount + (SingleLetterCount * SingleLetterCount) - 1;

    [GeneratedRegex(@"^[A-Z]{1,2}$", RegexOptions.CultureInvariant)]
    private static partial Regex ValidCodeRegex();

    public static bool IsValidCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return false;
        }

        return ValidCodeRegex().IsMatch(code.Trim().ToUpperInvariant());
    }

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public static int CodeToIndex(string code)
    {
        var normalized = NormalizeCode(code);
        if (!IsValidCode(normalized))
        {
            throw new ArgumentException($"Invalid computer code: {code}", nameof(code));
        }

        if (normalized.Length == 1)
        {
            return normalized[0] - 'A';
        }

        return SingleLetterCount
            + ((normalized[0] - 'A') * SingleLetterCount)
            + (normalized[1] - 'A');
    }

    public static string IndexToCode(int index)
    {
        if (index < 0 || index > MaxIndex)
        {
            throw new ArgumentOutOfRangeException(nameof(index), "No available computer codes remaining");
        }

        if (index < SingleLetterCount)
        {
            return ((char)('A' + index)).ToString();
        }

        var remainder = index - SingleLetterCount;
        var first = (char)('A' + (remainder / SingleLetterCount));
        var second = (char)('A' + (remainder % SingleLetterCount));
        return $"{first}{second}";
    }

    public static string GetNextCodeAfterMax(IEnumerable<string> activeCodes)
    {
        var maxIndex = -1;
        foreach (var code in activeCodes)
        {
            if (!IsValidCode(code))
            {
                continue;
            }

            var index = CodeToIndex(code);
            if (index > maxIndex)
            {
                maxIndex = index;
            }
        }

        return IndexToCode(maxIndex + 1);
    }
}