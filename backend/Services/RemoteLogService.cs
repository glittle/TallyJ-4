using System.Text.Json;

namespace Backend.Services;

/// <summary>
/// Service implementation for sending log messages to remote logging endpoints via webhooks.
/// </summary>
public class RemoteLogService : IRemoteLogService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<RemoteLogService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly string _webhookUrl;

    /// <summary>
    /// Initializes a new instance of the RemoteLogService.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="configuration">Application configuration for retrieving webhook URLs.</param>
    /// <param name="logger">Logger for recording remote log operations.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for retrieving current request information.</param>
    public RemoteLogService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<RemoteLogService> logger, IHttpContextAccessor httpContextAccessor)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;

        _webhookUrl = _configuration["ifttt:webhook"] ?? string.Empty;
        if (string.IsNullOrEmpty(_webhookUrl))
        {
            _logger.LogWarning("Remote logging webhook URL is not configured.");
        }
        else
        {
            _logger.LogInformation("Remote logging webhook URL configured: {WebhookUrl}", _webhookUrl);
        }
    }

    /// <summary>
    /// Computes the host and version string for logging.
    /// </summary>
    /// <param name="userName">The username of the authenticated user.</param>
    /// <returns>A string containing ComputerName / Host URL / Version [/ UserName] if user is authenticated.</returns>
    private async Task<string> GetHostAndVersionAsync(string? userName)
    {
        var version = _configuration["version"] ?? "unknown";
        var result = $"{Environment.MachineName} / {version}";

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return result;
        }

        var hostUrl = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                      ?? httpContext.Request.Host.ToString();

        result += $" / {hostUrl}";

        if (!string.IsNullOrEmpty(userName))
        {
            result += $" / {userName}";
        }

        return result;
    }

    /// <summary>
    /// Sends a log message to the configured remote logging endpoint.
    /// </summary>
    /// <param name="message">The message for the log.</param>
    /// <param name="userName">The username of the authenticated user.</param>
    /// <param name="electionName">Election name</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendLogAsync(string message, string? userName = null, string? electionName = null)
    {
        if (string.IsNullOrEmpty(_webhookUrl))
        {
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var hostAndVersion = await GetHostAndVersionAsync(userName);
            var payload = new
            {
                value1 = hostAndVersion,
                value2 = electionName,
                value3 = message
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(_webhookUrl, content);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to send log to remote endpoint. Status: {StatusCode}", response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending log to remote endpoint.");
        }
    }

}