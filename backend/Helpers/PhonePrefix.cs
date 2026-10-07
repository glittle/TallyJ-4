using PhoneNumbers;

namespace Backend.Helpers;

/// <summary>
/// Destination prefix for paid-send limits: the first digits of the E.164 number
/// after <c>+</c>, parsed with libphonenumber. Default length is 6
/// (country code plus the next digits).
/// </summary>
public static class PhonePrefix
{
    /// <summary>
    /// Writes the prefix when <paramref name="phone"/> parses as a phone number.
    /// The result is digits only, with no leading <c>+</c>.
    /// </summary>
    public static bool TryExtract(string? phone, int digitCount, out string prefix)
    {
        prefix = string.Empty;
        if (string.IsNullOrWhiteSpace(phone) || digitCount < 1)
        {
            return false;
        }

        var util = PhoneNumberUtil.GetInstance();
        PhoneNumber parsed;
        try
        {
            parsed = util.Parse(phone.Trim(), null);
        }
        catch (NumberParseException)
        {
            return false;
        }

        var e164 = util.Format(parsed, PhoneNumberFormat.E164);
        if (e164.Length < 2 || e164[0] != '+')
        {
            return false;
        }

        var digits = e164.AsSpan(1);
        for (var i = 0; i < digits.Length; i++)
        {
            if (digits[i] is < '0' or > '9')
            {
                return false;
            }
        }

        var take = Math.Min(digitCount, digits.Length);
        prefix = digits[..take].ToString();
        return prefix.Length > 0;
    }
}
