using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Backend.Context;
using Backend.Helpers;

namespace Backend.Services;

/// <summary>
/// Twilio SMS for an announcement body. Same account and status callback as verification SMS.
/// Writes <c>SmsLog</c> only after Twilio accepts the message.
/// </summary>
public class TwilioAnnouncementSmsSender : IAnnouncementSmsSender
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly MainDbContext _context;
    private readonly ILogger<TwilioAnnouncementSmsSender> _logger;

    public TwilioAnnouncementSmsSender(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        MainDbContext context,
        ILogger<TwilioAnnouncementSmsSender> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsConfigured()
    {
        return !IsPlaceholder(_configuration["Twilio:AccountSid"])
            && !IsPlaceholder(_configuration["Twilio:AuthToken"])
            && !IsPlaceholder(_configuration["Twilio:FromNumber"]);
    }

    /// <inheritdoc />
    public async Task<AnnouncementProviderResult> SendAsync(
        string phone,
        string message,
        Guid electionGuid,
        Guid personGuid,
        CancellationToken cancellationToken = default)
    {
        if (!PaidDestinationPhone.TryExplain(phone, out var reason))
        {
            return AnnouncementProviderResult.Failed(reason);
        }

        if (!IsConfigured())
        {
            return AnnouncementProviderResult.NotConfigured();
        }

        var accountSid = _configuration["Twilio:AccountSid"]!;
        var authToken = _configuration["Twilio:AuthToken"];
        var fromNumber = _configuration["Twilio:FromNumber"];

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{accountSid}:{authToken}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var pairs = new List<KeyValuePair<string, string>>
        {
            new("To", phone),
            new("From", fromNumber ?? ""),
            new("Body", message)
        };
        var callback = TwilioStatusCallbackUrl.TryResolve(_configuration);
        if (callback != null)
        {
            pairs.Add(new KeyValuePair<string, string>("StatusCallback", callback));
        }

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsync(
                $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json",
                new FormUrlEncodedContent(pairs),
                cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Announcement SMS failed before Twilio returned a status");
            return AnnouncementProviderResult.Failed(ex.Message);
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Announcement SMS failed: HTTP {Status}", (int)response.StatusCode);
            return AnnouncementProviderResult.Failed($"HTTP {(int)response.StatusCode}");
        }

        var (sid, status) = await ReadSidAsync(response, cancellationToken);
        await TryPersistSmsLogAsync(sid, phone, status, electionGuid, personGuid);
        return AnnouncementProviderResult.Succeeded();
    }

    private async Task TryPersistSmsLogAsync(
        string? sid,
        string phone,
        string? status,
        Guid electionGuid,
        Guid personGuid)
    {
        var log = SmsLogSendHelper.TryCreate(sid, phone, status, electionGuid, personGuid);
        if (log == null)
        {
            _logger.LogWarning("Announcement SMS succeeded but SmsLog not written (missing SID)");
            return;
        }

        try
        {
            _context.SmsLogs.Add(log);
            await _context.SaveChangesAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SmsLog insert failed after announcement SMS");
        }
    }

    private static async Task<(string? Sid, string? Status)> ReadSidAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);
            var sid = doc.RootElement.TryGetProperty("sid", out var sidEl) ? sidEl.GetString() : null;
            var status = doc.RootElement.TryGetProperty("status", out var statusEl) ? statusEl.GetString() : null;
            return (sid, status);
        }
        catch (JsonException)
        {
            return (null, null);
        }
    }

    private static bool IsPlaceholder(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.TrimStart().StartsWith('<');
}
