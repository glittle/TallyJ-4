using System.Text;
using System.Text.Json;
using Backend.Helpers;

namespace Backend.Services.Auth;

/// <summary>
/// GreenAPI <c>checkWhatsapp</c> HTTP helper. Config keys:
/// <c>GreenApi:IdInstance</c>, <c>GreenApi:ApiToken</c>, <c>GreenApi:BaseUrl</c>.
/// </summary>
public class GreenApiWhatsAppClient : IGreenApiWhatsAppClient
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GreenApiWhatsAppClient> _logger;

    public GreenApiWhatsAppClient(
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        ILogger<GreenApiWhatsAppClient> logger)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<GreenApiWhatsAppCheckResult> CheckWhatsAppAsync(
        string phone,
        CancellationToken cancellationToken = default)
    {
        var idInstance = _configuration["GreenApi:IdInstance"];
        var apiToken = _configuration["GreenApi:ApiToken"];
        var baseUrl = _configuration["GreenApi:BaseUrl"] ?? "https://api.green-api.com";

        if (!GreenApiWhatsAppHelper.IsConfigured(idInstance, apiToken))
        {
            _logger.LogWarning("{Method}: GreenAPI not configured; skip check", nameof(CheckWhatsAppAsync));
            return GreenApiWhatsAppCheckResult.NotConfigured();
        }

        var digits = GreenApiWhatsAppHelper.NormalizePhone(phone);
        if (digits.Length == 0 || !long.TryParse(digits, out var phoneNumber))
        {
            return GreenApiWhatsAppCheckResult.FromProvider(OnlineVoterWhatsAppStatus.CheckFailed);
        }

        var client = _httpClientFactory.CreateClient("GreenApi");
        var url = GreenApiWhatsAppHelper.BuildCheckUrl(baseUrl, idInstance!, apiToken!);
        var json = JsonSerializer.Serialize(new { phoneNumber });
        using var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync(url, content, cancellationToken);
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var status = GreenApiWhatsAppHelper.MapCheckResponse(body, response.IsSuccessStatusCode);
            _logger.LogInformation(
                "{Method}: GreenAPI checkWhatsapp ({Status})",
                nameof(CheckWhatsAppAsync),
                status);
            return GreenApiWhatsAppCheckResult.FromProvider(status);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _logger.LogWarning(ex, "{Method}: GreenAPI checkWhatsapp failed", nameof(CheckWhatsAppAsync));
            return GreenApiWhatsAppCheckResult.FromProvider(OnlineVoterWhatsAppStatus.CheckFailed);
        }
    }
}
