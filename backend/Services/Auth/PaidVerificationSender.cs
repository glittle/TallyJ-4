using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Backend.Helpers;

namespace Backend.Services.Auth;

/// <summary>
/// Twilio (SMS / voice) and GreenAPI (WhatsApp) delivery for verification codes.
/// </summary>
public class PaidVerificationSender : IPaidVerificationSender
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<PaidVerificationSender> _logger;

    public PaidVerificationSender(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<PaidVerificationSender> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public Task<bool> SendSmsAsync(string phone, string code) =>
        SendIfAllowedAsync(phone, "SMS", () => SendSmsCoreAsync(phone, code));

    /// <inheritdoc/>
    public Task<bool> SendVoiceAsync(string phone, string code) =>
        SendIfAllowedAsync(phone, "voice", () => SendVoiceCoreAsync(phone, code));

    /// <inheritdoc/>
    public Task<bool> SendWhatsAppAsync(string phone, string code) =>
        SendIfAllowedAsync(phone, "WhatsApp", () => SendWhatsAppCoreAsync(phone, code));

    private async Task<bool> SendIfAllowedAsync(string phone, string channel, Func<Task<bool>> send)
    {
        if (!PaidDestinationPhone.TryExplain(phone, out var reason))
        {
            _logger.LogWarning(
                "Skipping {Channel} send; destination rejected ({Reason}): {Phone}",
                channel, reason, phone);
            return false;
        }

        return await send();
    }

    private async Task<bool> SendSmsCoreAsync(string phone, string code)
    {
        var accountSid = _configuration["Twilio:AccountSid"];
        if (string.IsNullOrWhiteSpace(accountSid) || accountSid.StartsWith('<'))
        {
            _logger.LogWarning("Twilio not configured; skipping SMS for {Phone}", phone);
            return true;
        }

        var authToken = _configuration["Twilio:AuthToken"];
        var fromNumber = _configuration["Twilio:FromNumber"];

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("To", phone),
            new KeyValuePair<string, string>("From", fromNumber ?? ""),
            new KeyValuePair<string, string>("Body", $"Your TallyJ voting code is: {code}\n\nThis code expires in 15 minutes.")
        });

        var response = await client.PostAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json",
            formData);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Twilio SMS failed for {Phone}: {Status} - {Body}", phone, response.StatusCode, body);
            return false;
        }

        _logger.LogInformation("SMS verification code sent to {Phone}", phone);
        return true;
    }

    private async Task<bool> SendVoiceCoreAsync(string phone, string code)
    {
        var accountSid = _configuration["Twilio:AccountSid"];
        if (string.IsNullOrWhiteSpace(accountSid) || accountSid.StartsWith('<'))
        {
            _logger.LogWarning("Twilio not configured; skipping voice call for {Phone}", phone);
            return true;
        }

        var authToken = _configuration["Twilio:AuthToken"];
        var fromNumber = _configuration["Twilio:FromNumber"];

        var spokenCode = string.Join(". ", code.ToCharArray());
        var twiml = $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<Response>
  <Say language=""en-US"">Your TallyJ voting code is: {spokenCode}. I repeat: {spokenCode}. This code expires in 15 minutes.</Say>
</Response>";

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("To", phone),
            new KeyValuePair<string, string>("From", fromNumber ?? ""),
            new KeyValuePair<string, string>("Twiml", twiml)
        });

        var response = await client.PostAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Calls.json",
            formData);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("Twilio voice call failed for {Phone}: {Status} - {Body}", phone, response.StatusCode, body);
            return false;
        }

        _logger.LogInformation("Voice verification code sent to {Phone}", phone);
        return true;
    }

    private async Task<bool> SendWhatsAppCoreAsync(string phone, string code)
    {
        var idInstance = _configuration["GreenApi:IdInstance"];
        var apiToken = _configuration["GreenApi:ApiToken"];
        var baseUrl = _configuration["GreenApi:BaseUrl"] ?? "https://api.green-api.com";

        if (string.IsNullOrWhiteSpace(idInstance) || idInstance.StartsWith('<'))
        {
            _logger.LogWarning("GreenAPI not configured; skipping WhatsApp for {Phone}", phone);
            return true;
        }

        var normalizedPhone = NormalizePhoneForWhatsApp(phone);
        var chatId = $"{normalizedPhone}@c.us";

        var client = _httpClientFactory.CreateClient("GreenApi");
        var url = $"{baseUrl}/waInstance{idInstance}/sendMessage/{apiToken}";

        var payload = new
        {
            chatId,
            message = $"Your TallyJ voting code is: {code}\n\nThis code expires in 15 minutes."
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await client.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            _logger.LogError("GreenAPI WhatsApp failed for {Phone}: {Status} - {Body}", phone, response.StatusCode, body);
            return false;
        }

        _logger.LogInformation("WhatsApp verification code sent to {Phone}", phone);
        return true;
    }

    private static string NormalizePhoneForWhatsApp(string phone)
    {
        return new string(phone.Where(char.IsDigit).ToArray());
    }
}
