using System.Globalization;
using Backend.Enumerations;
using Backend.Localization;

namespace Backend.Services;

/// <summary>
/// Resolves eligibility column values from a people import file.
/// Accepts person-form codes and exact descriptions in the current UI language
/// (plus English). Unrecognized values are not treated as eligible.
/// </summary>
internal static class PeopleImportEligibility
{
    /// <summary>
    /// Builds a lookup of accepted import values. A null reason means fully eligible.
    /// </summary>
    public static IReadOnlyDictionary<string, IneligibleReason?> BuildValueLookup(
        IJsonLocalizationProvider localization,
        CultureInfo culture)
    {
        var lookup = new Dictionary<string, IneligibleReason?>(StringComparer.Ordinal);

        Add(lookup, "Eligible", null);
        Add(lookup, localization.GetString("eligibility.eligible", culture), null);

        foreach (var reason in IneligibleReasonEnum.PersonReasons)
        {
            Add(lookup, reason.Code, reason);
            Add(lookup, reason.Description, reason);
            Add(lookup, localization.GetString($"eligibility.{reason.Code}", culture), reason);
        }

        return lookup;
    }

    /// <summary>
    /// Resolves a trimmed cell value. Empty is eligible. Returns false when the value is unrecognized.
    /// </summary>
    public static bool TryResolve(
        string? value,
        IReadOnlyDictionary<string, IneligibleReason?> lookup,
        out IneligibleReason? reason)
    {
        reason = null;
        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        return lookup.TryGetValue(value, out reason);
    }

    private static void Add(
        Dictionary<string, IneligibleReason?> lookup,
        string? value,
        IneligibleReason? reason)
    {
        if (!string.IsNullOrEmpty(value))
        {
            lookup.TryAdd(value, reason);
        }
    }
}
