using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Backend.Context;
using Backend.Helpers;

namespace Backend.Services.Auth;

/// <summary>
/// Twilio (SMS / voice) and GreenAPI (WhatsApp) delivery for verification codes.
/// On a successful provider send, writes an <c>SmsLog</c> row and (Twilio only)
/// sets StatusCallback to <c>POST /api/Public/smsStatus</c>.
/// </summary>
public class PaidVerificationSender : IPaidVerificationSender
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MainDbContext _context;
    private readonly ILogger<PaidVerificationSender> _logger;

    public PaidVerificationSender(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        MainDbContext context,
        ILogger<PaidVerificationSender> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _context = context;
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
                "Skipping {Channel} send; destination rejected ({Reason})",
                channel, reason);
            return false;
        }

        return await send();
    }

    private async Task<bool> SendSmsCoreAsync(string phone, string code)
    {
        var accountSid = _configuration["Twilio:AccountSid"];
        if (string.IsNullOrWhiteSpace(accountSid) || accountSid.StartsWith('<'))
        {
            _logger.LogWarning("Twilio not configured; skipping SMS");
            return true;
        }

        var authToken = _configuration["Twilio:AuthToken"];
        var fromNumber = _configuration["Twilio:FromNumber"];

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var pairs = new List<KeyValuePair<string, string>>
        {
            new("To", phone),
            new("From", fromNumber ?? ""),
            new("Body", $"Your TallyJ voting code is: {code}\n\nThis code expires in 15 minutes.")
        };
        AddStatusCallback(pairs);

        var response = await client.PostAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json",
            new FormUrlEncodedContent(pairs));

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Twilio SMS failed: {Status}", response.StatusCode);
            return false;
        }

        var (sid, status) = await ReadTwilioSidAndStatusAsync(response);
        await TryPersistSmsLogAsync(phone, sid, status);
        _logger.LogInformation("SMS verification code sent");
        return true;
    }

    private async Task<bool> SendVoiceCoreAsync(string phone, string code)
    {
        var accountSid = _configuration["Twilio:AccountSid"];
        if (string.IsNullOrWhiteSpace(accountSid) || accountSid.StartsWith('<'))
        {
            _logger.LogWarning("Twilio not configured; skipping voice call");
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

        var pairs = new List<KeyValuePair<string, string>>
        {
            new("To", phone),
            new("From", fromNumber ?? ""),
            new("Twiml", twiml)
        };
        AddStatusCallback(pairs);

        var response = await client.PostAsync(
            $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Calls.json",
            new FormUrlEncodedContent(pairs));

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Twilio voice call failed: {Status}", response.StatusCode);
            return false;
        }

        var (sid, status) = await ReadTwilioSidAndStatusAsync(response);
        await TryPersistSmsLogAsync(phone, sid, status);
        _logger.LogInformation("Voice verification code sent");
        return true;
    }

    private async Task<bool> SendWhatsAppCoreAsync(string phone, string code)
    {
        var idInstance = _configuration["GreenApi:IdInstance"];
        var apiToken = _configuration["GreenApi:ApiToken"];
        var baseUrl = _configuration["GreenApi:BaseUrl"] ?? "https://api.green-api.com";

        if (string.IsNullOrWhiteSpace(idInstance) || idInstance.StartsWith('<'))
        {
            _logger.LogWarning("GreenAPI not configured; skipping WhatsApp");
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
            _logger.LogError("GreenAPI WhatsApp failed: {Status}", response.StatusCode);
            return false;
        }

        var sid = await ReadGreenApiMessageIdAsync(response);
        await TryPersistSmsLogAsync(phone, sid, SmsLogSendHelper.DefaultLastStatus);
        _logger.LogInformation("WhatsApp verification code sent");
        return true;
    }

    private void AddStatusCallback(List<KeyValuePair<string, string>> pairs)
    {
        var callback = TwilioStatusCallbackUrl.TryResolve(_configuration);
        if (callback != null)
        {
            pairs.Add(new KeyValuePair<string, string>("StatusCallback", callback));
        }
    }

    /// <summary>
    /// Persist SmsLog after a successful provider send. Missing SID skips the insert
    /// (callback cannot match a row). A failed insert must not fail the voter send.
    /// </summary>
    private async Task TryPersistSmsLogAsync(string phone, string? sid, string? lastStatus)
    {
        var log = SmsLogSendHelper.TryCreate(sid, phone, lastStatus);
        if (log == null)
        {
            _logger.LogWarning("Paid send succeeded but SmsLog not written (missing SID)");
            return;
        }

        try
        {
            _context.SmsLogs.Add(log);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SmsLog insert failed after paid send");
        }
    }

    private static async Task<(string? Sid, string? Status)> ReadTwilioSidAndStatusAsync(
        HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            var sid = ReadJsonString(doc.RootElement, "sid");
            var status = ReadJsonString(doc.RootElement, "status");
            return (sid, status);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static async Task<string?> ReadGreenApiMessageIdAsync(HttpResponseMessage response)
    {
        try
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            return ReadJsonString(doc.RootElement, "idMessage");
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? ReadJsonString(JsonElement root, string name) =>
        root.TryGetProperty(name, out var el) ? el.GetString() : null;

    private static string NormalizePhoneForWhatsApp(string phone)
    {
        return new string(phone.Where(char.IsDigit).ToArray());
    }
}
