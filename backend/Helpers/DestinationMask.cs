namespace Backend.Helpers;

/// <summary>
/// Hides the middle of a phone number or the local part of an email before it is logged or emailed.
/// </summary>
public static class DestinationMask
{
    /// <summary>
    /// Returns a masked destination. Email keeps the domain. Phone keeps a short prefix and the last two digits.
    /// Stored rows and super-admin alerts use this form.
    /// </summary>
    public static string Mask(string? destination)
    {
        if (string.IsNullOrWhiteSpace(destination))
        {
            return "";
        }

        var trimmed = destination.Trim();
        if (trimmed.Contains('@'))
        {
            return MaskEmail(trimmed);
        }

        return MaskPhone(trimmed);
    }

    /// <summary>
    /// Mask for application logs. An email drops the domain so the log line does not name the mailbox host.
    /// Phone masking matches <see cref="Mask"/>.
    /// </summary>
    public static string MaskForLog(string? destination)
    {
        var masked = Mask(destination);
        var at = masked.IndexOf('@');
        return at >= 0 ? masked[..at] : masked;
    }

    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 0 || at == email.Length - 1)
        {
            return "***";
        }

        var local = email[..at];
        var domain = email[(at + 1)..];
        return local[..1] + "***@" + domain;
    }

    private static string MaskPhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.Length <= 4)
        {
            return "***";
        }

        var prefixLength = Math.Min(2, digits.Length - 2);
        var hidden = digits.Length - prefixLength - 2;
        return "+" + digits[..prefixLength] + new string('*', hidden) + digits[^2..];
    }
}
