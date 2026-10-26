namespace Backend.Helpers;

/// <summary>
/// Bulk announcement channels. SMS and WhatsApp are paid. Email is not.
/// </summary>
public static class AnnouncementChannels
{
    /// <summary>
    /// Election <c>EmailText</c> to each selected person's email.
    /// </summary>
    public const string Email = "email";

    /// <summary>
    /// Election <c>SmsText</c> via Twilio.
    /// </summary>
    public const string Sms = "sms";

    /// <summary>
    /// Election <c>SmsText</c> via GreenAPI to known-OK WhatsApp numbers.
    /// </summary>
    public const string WhatsApp = "whatsapp";

    /// <summary>
    /// Channels the new announcement queue sends. WhatsApp stays on its own queue.
    /// </summary>
    public static bool IsDirect(string? channel) =>
        channel is Email or Sms;

    /// <summary>
    /// Channels a retry-failed call accepts.
    /// </summary>
    public static bool IsRetryable(string? channel) =>
        channel is Email or Sms or WhatsApp;
}
