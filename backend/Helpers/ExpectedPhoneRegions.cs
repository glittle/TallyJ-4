using PhoneNumbers;

namespace Backend.Helpers;

/// <summary>
/// ISO region codes stored on an election for the voter-list phone check.
/// </summary>
public static class ExpectedPhoneRegions
{
    /// <summary>
    /// Column length of <c>Elections.ExpectedPhoneRegions</c>.
    /// </summary>
    public const int MaxLength = 80;

    /// <summary>
    /// Phrase key when a code is not a libphonenumber region.
    /// </summary>
    public const string InvalidMessageKey = "elections.form.expectedPhoneRegionsInvalid";

    /// <summary>
    /// Phrase key when the stored list is longer than <see cref="MaxLength"/>.
    /// </summary>
    public const string TooLongMessageKey = "elections.form.expectedPhoneRegionsTooLong";

    private static readonly HashSet<string> Supported = new(
        PhoneNumberUtil.GetInstance().GetSupportedRegions(),
        StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// True when every comma, semicolon, or space separated code is a supported region.
    /// Empty is valid and means the site default.
    /// </summary>
    public static bool AreSupported(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        foreach (var code in Split(value))
        {
            if (!Supported.Contains(code))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// True when <paramref name="code"/> is a libphonenumber region such as CA or US.
    /// </summary>
    public static bool IsSupported(string code)
    {
        return Supported.Contains(code);
    }

    /// <summary>
    /// Splits a stored list into region codes.
    /// </summary>
    public static string[] Split(string value)
    {
        return value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }
}
