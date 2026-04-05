using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

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
    private readonly UserManager<AppUser> _userManager;

    /// <summary>
    /// Initializes a new instance of the RemoteLogService.
    /// </summary>
    /// <param name="httpClientFactory">Factory for creating HTTP clients.</param>
    /// <param name="configuration">Application configuration for retrieving webhook URLs.</param>
    /// <param name="logger">Logger for recording remote log operations.</param>
    /// <param name="httpContextAccessor">HTTP context accessor for retrieving current request information.</param>
    /// <param name="userManager">User manager for retrieving user information.</param>
    public RemoteLogService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<RemoteLogService> logger, IHttpContextAccessor httpContextAccessor, UserManager<AppUser> userManager)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
    }

    /// <summary>
    /// Computes the host and version string for logging.
    /// </summary>
    /// <returns>A string containing ComputerName / Host URL / Version [/ UserName] if user is authenticated.</returns>
    private async Task<string> GetHostAndVersionAsync()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return $"{Environment.MachineName} / Unknown / {Assembly.GetExecutingAssembly().GetName().Version}";
        }

        var hostUrl = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                      ?? httpContext.Request.Host.ToString();

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

        var userIdString = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? httpContext.User.FindFirst("sub")?.Value;

        string userName = null;
        if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());
            userName = user?.UserName;
        }

        var result = $"{Environment.MachineName} / {hostUrl} / {version}";
        if (!string.IsNullOrEmpty(userName))
        {
            result += $" / {userName}";
        }

        return result;
    }

    /// <summary>
    /// Sends a log message to the configured remote logging endpoint.
    /// </summary>
    /// <param name="message">The log message to send.</param>
    /// <param name="level">The log level (e.g., "Error", "Warning", "Info").</param>
    /// <param name="details">Additional details about the log event.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task SendLogAsync(string message, string level, string? details = null)
    {
        var webhookUrl = _configuration["ifttt:ReportToGlen"];
        if (string.IsNullOrEmpty(webhookUrl))
        {
            _logger.LogWarning("Remote logging webhook URL is not configured.");
            return;
        }

        try
        {
            var client = _httpClientFactory.CreateClient();
            var hostAndVersion = await GetHostAndVersionAsync();
            var payload = new
            {
                value1 = hostAndVersion,
                value2 = message,
                value3 = details ?? string.Empty
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var response = await client.PostAsync(webhookUrl, content);
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