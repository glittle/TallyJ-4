using System.Text.Json;

namespace Backend.Helpers;

/// <summary>
/// GreenAPI <c>checkWhatsapp</c> URL, phone digits, and response mapping.
/// Does not call HTTP and does not persist.
/// </summary>
public static class GreenApiWhatsAppHelper
{
    /// <summary>
    /// True when IdInstance and ApiToken are present and not placeholder values.
    /// </summary>
    public static bool IsConfigured(string? idInstance, string? apiToken) =>
        !string.IsNullOrWhiteSpace(idInstance)
        && !idInstance.StartsWith('<')
        && !string.IsNullOrWhiteSpace(apiToken)
        && !apiToken.StartsWith('<');

    /// <summary>
    /// Digits-only phone for GreenAPI <c>checkWhatsapp</c> / <c>sendMessage</c>.
    /// </summary>
    public static string NormalizePhone(string phone) =>
        new(phone.Where(char.IsDigit).ToArray());

    /// <summary>
    /// <c>{baseUrl}/waInstance{idInstance}/checkWhatsapp/{apiToken}</c>.
    /// </summary>
    public static string BuildCheckUrl(string baseUrl, string idInstance, string apiToken)
    {
        var root = baseUrl.TrimEnd('/');
        return $"{root}/waInstance{idInstance}/checkWhatsapp/{apiToken}";
    }

    /// <summary>
    /// Maps a GreenAPI <c>checkWhatsapp</c> body to a stored status code.
    /// <c>existsWhatsapp: true</c> → <see cref="OnlineVoterWhatsAppStatus.Ok"/>;
    /// <c>false</c> → <see cref="OnlineVoterWhatsAppStatus.NoWa"/>;
    /// anything else (including HTTP failure) → <see cref="OnlineVoterWhatsAppStatus.CheckFailed"/>.
    /// </summary>
    public static string MapCheckResponse(string? json, bool successStatusCode)
    {
        if (!successStatusCode || string.IsNullOrWhiteSpace(json))
        {
            return OnlineVoterWhatsAppStatus.CheckFailed;
        }

        try
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("existsWhatsapp", out var el))
            {
                return OnlineVoterWhatsAppStatus.CheckFailed;
            }

            return el.ValueKind switch
            {
                JsonValueKind.True => OnlineVoterWhatsAppStatus.Ok,
                JsonValueKind.False => OnlineVoterWhatsAppStatus.NoWa,
                _ => OnlineVoterWhatsAppStatus.CheckFailed
            };
        }
        catch (JsonException)
        {
            return OnlineVoterWhatsAppStatus.CheckFailed;
        }
    }
}
