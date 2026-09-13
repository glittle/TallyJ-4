namespace Backend.Helpers;

/// <summary>
/// Durable WhatsApp presence stored on <see cref="Entities.OnlineVoter.WhatsAppStatus"/>
/// for phone rows (<c>VoterIdType == "P"</c>). Separate from <see cref="OnlineVoterSmsStatus"/>.
/// </summary>
public static class OnlineVoterWhatsAppStatus
{
    /// <summary>
    /// GreenAPI reported the number has WhatsApp.
    /// </summary>
    public const string Ok = "OK";

    /// <summary>
    /// GreenAPI reported the number does not have WhatsApp.
    /// </summary>
    public const string NoWa = "no-wa";

    /// <summary>
    /// GreenAPI check failed (HTTP / parse / missing existsWhatsapp).
    /// </summary>
    public const string CheckFailed = "check-failed";

    /// <summary>
    /// WhatsApp send is allowed when status is unset (not yet checked) or explicitly OK.
    /// </summary>
    public static bool AllowsSend(string? whatsAppStatus) =>
        whatsAppStatus is null || whatsAppStatus == Ok;
}
