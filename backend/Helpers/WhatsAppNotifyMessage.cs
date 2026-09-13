namespace Backend.Helpers;

/// <summary>
/// Fills v3 head-teller notify placeholders in <c>Election.SmsText</c>.
/// </summary>
public static class WhatsAppNotifyMessage
{
    /// <summary>
    /// Replaces <c>{PersonName}</c>, <c>{FirstName}</c>, <c>{VoterContact}</c>, and <c>{hostSite}</c>.
    /// </summary>
    public static string Fill(
        string template,
        string? personName,
        string? firstName,
        string? voterContact,
        string? hostSite)
    {
        return template
            .Replace("{PersonName}", personName ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{FirstName}", firstName ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{VoterContact}", voterContact ?? "", StringComparison.OrdinalIgnoreCase)
            .Replace("{hostSite}", hostSite ?? "", StringComparison.OrdinalIgnoreCase);
    }
}
