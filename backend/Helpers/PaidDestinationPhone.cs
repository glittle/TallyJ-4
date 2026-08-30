using System.Text.RegularExpressions;

namespace Backend.Helpers;

/// <summary>
/// In-code checks for destinations that must never reach a paid SMS / voice / WhatsApp provider.
/// No database lookup — reserved NANP ranges and E.164 shape only.
/// </summary>
public static class PaidDestinationPhone
{
    /// <summary>
    /// Reason code when the value is not a usable E.164 (or E.164-shaped) number.
    /// </summary>
    public const string ReasonMalformedE164 = "malformed-e164";

    /// <summary>
    /// Reason code when the number is a reserved/fictional NANP 555 range.
    /// </summary>
    public const string ReasonNanp555 = "555-range";

    // Standard NANP national number: NXX-NXX-XXXX (area code 555, or exchange 555).
    private static readonly Regex NanpAreaCode555 = new(@"^1?555\d{7}$", RegexOptions.Compiled);
    private static readonly Regex NanpExchange555 = new(@"^1?[2-9]\d{2}555\d{4}$", RegexOptions.Compiled);

    /// <summary>
    /// Paid verification channels that incur provider charges.
    /// </summary>
    public static bool IsPaidChannel(string? deliveryMethod) =>
        deliveryMethod is "sms" or "voice" or "whatsapp";

    /// <summary>
    /// Whether <paramref name="phone"/> may be passed to a paid provider.
    /// </summary>
    public static bool IsAllowed(string? phone) => TryExplain(phone, out _);

    /// <summary>
    /// Validates <paramref name="phone"/> and returns a short reason code when it is not allowed.
    /// </summary>
    public static bool TryExplain(string? phone, out string? reason)
    {
        reason = null;

        if (!TryGetInternationalDigits(phone, out var digits))
        {
            reason = ReasonMalformedE164;
            return false;
        }

        if (IsReservedNanp555(digits))
        {
            reason = ReasonNanp555;
            return false;
        }

        return true;
    }

    /// <summary>
    /// True for NANP area code 555 or exchange 555 (including the 555-01xx fictional block).
    /// </summary>
    public static bool IsReservedNanp555(string digits) =>
        NanpAreaCode555.IsMatch(digits) || NanpExchange555.IsMatch(digits);

    /// <summary>
    /// Accepts true E.164 (<c>+</c> then 8–15 digits) or the same digit string without <c>+</c>.
    /// </summary>
    private static bool TryGetInternationalDigits(string? phone, out string digits)
    {
        digits = string.Empty;
        if (string.IsNullOrWhiteSpace(phone))
        {
            return false;
        }

        var trimmed = phone.Trim();
        if (trimmed[0] == '+')
        {
            trimmed = trimmed[1..];
        }

        if (trimmed.Length is < 8 or > 15)
        {
            return false;
        }

        if (trimmed[0] is < '1' or > '9')
        {
            return false;
        }

        for (var i = 1; i < trimmed.Length; i++)
        {
            if (trimmed[i] is < '0' or > '9')
            {
                return false;
            }
        }

        digits = trimmed;
        return true;
    }
}
