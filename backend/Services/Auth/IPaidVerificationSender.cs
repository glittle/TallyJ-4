namespace Backend.Services.Auth;

/// <summary>
/// Sends a verification code over a paid channel (SMS / voice / WhatsApp).
/// Implementations must not call a provider when <see cref="Helpers.PaidDestinationPhone"/> rejects the destination.
/// </summary>
public interface IPaidVerificationSender
{
    /// <summary>
    /// Sends the code via SMS.
    /// </summary>
    Task<bool> SendSmsAsync(string phone, string code);

    /// <summary>
    /// Sends the code via a voice call.
    /// </summary>
    Task<bool> SendVoiceAsync(string phone, string code);

    /// <summary>
    /// Sends the code via WhatsApp.
    /// </summary>
    Task<bool> SendWhatsAppAsync(string phone, string code);
}
