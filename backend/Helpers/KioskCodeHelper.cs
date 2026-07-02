using System.Globalization;
using System.Text;

namespace Backend.Helpers;

/// <summary>
/// Generates kiosk voter codes: last-name initial plus four distinct, easy-to-read letters.
/// </summary>
public static class KioskCodeHelper
{
    /// <summary>
    /// Letters that are visually and audibly distinct (excludes I, L, O, Q).
    /// </summary>
    public const string DistinctLetters = "ABCDEFGHJKMNPQRSTUVWXYZ";

    public static char GetLastNameInitial(string? lastName)
    {
        if (string.IsNullOrWhiteSpace(lastName))
        {
            return 'A';
        }

        var normalized = lastName.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (!char.IsLetter(c))
            {
                continue;
            }

            builder.Append(c);
        }

        if (builder.Length == 0)
        {
            return 'A';
        }

        var letter = char.ToUpperInvariant(builder[0]);
        return letter is >= 'A' and <= 'Z' ? letter : 'A';
    }

    public static string GenerateCode(string? lastName, Random random)
    {
        var initial = GetLastNameInitial(lastName);
        var suffix = new char[4];

        for (var i = 0; i < suffix.Length; i++)
        {
            suffix[i] = DistinctLetters[random.Next(DistinctLetters.Length)];
        }

        return $"{initial}{new string(suffix)}";
    }

    public static string GenerateUniqueCode(string? lastName, IEnumerable<string?> existingCodes, Random? random = null)
    {
        random ??= Random.Shared;
        var used = new HashSet<string>(
            existingCodes
                .Where(code => !string.IsNullOrWhiteSpace(code))
                .Select(code => code!.Trim().ToUpperInvariant()),
            StringComparer.OrdinalIgnoreCase);

        for (var attempt = 0; attempt < 500; attempt++)
        {
            var possibleCode = GenerateCode(lastName, random);
            if (used.Add(possibleCode))
            {
                return possibleCode;
            }
        }

        throw new InvalidOperationException("Unable to generate a unique kiosk code for this election.");
    }
}