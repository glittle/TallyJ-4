using System.Net;
using System.Security.Cryptography;
using System.Text;
using Backend;
using Backend.DTOs.Security;
using Backend.Helpers;
using Backend.Services;

namespace Backend.Middleware;

/// <summary>
/// In-memory rate limiting for anonymous teller and voter authentication endpoints.
/// Account login and guest teller login count failed attempts only, 20 per minute
/// per trusted-ingress IP. Venues often put many tellers behind one public NAT, so
/// a successful join must not fill that bucket; the per-election lockout is the
/// guessing guard. Other teller routes stay tight per IP and count every request.
/// Voter code routes use a per-VoterId bucket (capped JSON peek) plus a loose per-IP
/// venue ceiling so a hall behind one public NAT is not locked after a few people.
/// Oversized bodies are rejected (413); they are not demoted to missing:{ip}.
/// Leftmost XFF is never used to split venue clients (see GetClientIpAddress).
/// </summary>
public class RateLimitingMiddleware
{
    public const string TooManyRequestsKey = "error.tooManyRequests";
    public const string PayloadTooLargeKey = "error.payloadTooLarge";

    public const int VoterIdentifierMaxRequests = 5;
    public const int VoterVenueIpMaxRequests = 60;

    /// <summary>
    /// Failed guest-teller attempts (unknown election or wrong passcode) allowed per
    /// trusted-ingress IP per minute. Successes do not consume the bucket, and neither
    /// do not-open, no-main-teller, or locked replies. Venues behind one NAT share
    /// this IP; the per-election lockout is the guessing guard.
    /// </summary>
    public const int TellerLoginIpMaxRequests = 20;

    /// <summary>
    /// Failed account-login attempts (bad credentials) allowed per trusted-ingress IP
    /// per minute. Successful logins do not consume the bucket. Account-locked,
    /// unverified-email, and invalid two-factor replies do not count. Venues behind
    /// one NAT share this IP.
    /// </summary>
    public const int LoginIpMaxRequests = 20;

    internal const string IpFailureItemKey = "RateLimitCountFailure";

    private static readonly TimeSpan OneMinute = TimeSpan.FromMinutes(1);

    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;
    private readonly RateLimitStore _store;

    private static readonly Dictionary<string, (int MaxRequests, TimeSpan Window)> TellerIpLimits =
        new(StringComparer.OrdinalIgnoreCase)
        {
            { "/api/auth/login", (LoginIpMaxRequests, OneMinute) },
            { "/api/auth/teller-login", (TellerLoginIpMaxRequests, OneMinute) },
            // Endpoint is disabled (400 + i18n key); keep a tight bucket for leftover callers.
            { "/api/auth/registerAccount", (3, TimeSpan.FromHours(1)) },
            { "/api/auth/registerWithInvite", (3, TimeSpan.FromHours(1)) },
            { "/api/auth/account-invite", (10, TimeSpan.FromMinutes(1)) },
            { "/api/auth/verify2fa", (10, TimeSpan.FromMinutes(1)) },
            { "/api/auth/forgotPassword", (3, TimeSpan.FromHours(1)) },
            { "/api/auth/resetPassword", (3, TimeSpan.FromHours(1)) },
            { "/api/auth/google/one-tap", (5, TimeSpan.FromMinutes(1)) },
            { "/api/auth/facebook", (5, TimeSpan.FromMinutes(1)) },
            { "/api/auth/kakao", (5, TimeSpan.FromMinutes(1)) },
            { "/api/auth/telegram", (5, TimeSpan.FromMinutes(1)) }
        };

    /// <summary>
    /// These routes reject up front when the bucket is full, then record a timestamp
    /// only after the endpoint runs and only when it reports a counted failure.
    /// </summary>
    private static readonly HashSet<string> FailureOnlyIpPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/auth/login",
        "/api/auth/teller-login"
    };

    private static readonly HashSet<string> VoterCodePaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/online-voting/requestCode",
        "/api/online-voting/verifyCode"
    };

    private static readonly HashSet<string> VoterOauthPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/api/online-voting/googleAuth",
        "/api/online-voting/facebookAuth",
        "/api/online-voting/kakaoAuth",
        "/api/online-voting/telegramAuth"
    };

    /// <summary>
    /// Initializes a new instance of the RateLimitingMiddleware.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    /// <param name="store">The rate limit store for persisting request counts.</param>
    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, RateLimitStore store)
    {
        _next = next;
        _logger = logger;
        _store = store;
    }

    /// <summary>
    /// Processes the HTTP request and applies rate limiting if configured for the endpoint.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="securityAuditService">The security audit service for logging rate limit violations.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    public async Task InvokeAsync(HttpContext context, ISecurityAuditService securityAuditService)
    {
        var path = NormalizePath(context.Request.Path.Value);
        if (path == null)
        {
            await _next(context);
            return;
        }

        List<(string Key, int MaxRequests, TimeSpan Window, string Bucket)>? buckets = null;
        string? clientIp = null;

        if (VoterCodePaths.Contains(path))
        {
            clientIp = context.GetClientIpAddress();
            var peek = await JsonRequestVoterId.TryReadAsync(context.Request, context.RequestAborted);
            if (peek.Status == JsonRequestVoterIdStatus.TooLarge)
            {
                await WritePayloadTooLargeAsync(context, path, clientIp);
                return;
            }

            var identifier = peek.Status == JsonRequestVoterIdStatus.Found && peek.VoterId != null
                ? JsonRequestVoterId.NormalizeForRateLimit(peek.VoterId)
                : $"missing:{clientIp}";

            buckets =
            [
                (GetIdentifierKey(path, identifier), VoterIdentifierMaxRequests, OneMinute, "voter-id"),
                (GetClientKey(path, clientIp), VoterVenueIpMaxRequests, OneMinute, "voter-venue-ip")
            ];
        }
        else if (VoterOauthPaths.Contains(path))
        {
            clientIp = context.GetClientIpAddress();
            buckets =
            [
                (GetClientKey(path, clientIp), VoterVenueIpMaxRequests, OneMinute, "voter-venue-ip")
            ];
        }
        else if (TellerIpLimits.TryGetValue(path, out var tellerLimit))
        {
            clientIp = context.GetClientIpAddress();
            buckets =
            [
                (GetClientKey(path, clientIp), tellerLimit.MaxRequests, tellerLimit.Window, "teller-ip")
            ];
        }

        var failureOnly = FailureOnlyIpPaths.Contains(path);
        if (buckets != null)
        {
            var userAgent = context.Request.Headers.UserAgent.ToString();
            if (await TryRejectIfLimitedAsync(
                    context,
                    securityAuditService,
                    path,
                    clientIp!,
                    userAgent,
                    buckets,
                    recordAttempts: !failureOnly))
            {
                return;
            }
        }

        await _next(context);

        if (failureOnly && buckets != null && context.Items.ContainsKey(IpFailureItemKey))
        {
            RecordAttempts(buckets);
        }
    }

    /// <summary>
    /// Marks this request as a counted per-IP failure. The middleware records it
    /// after the endpoint returns. A 429 that never reached the endpoint is not marked.
    /// </summary>
    public static void MarkIpFailure(HttpContext context)
    {
        context.Items[IpFailureItemKey] = true;
    }

    internal static string GetClientKey(string path, string clientIp)
    {
        return $"{path.ToLowerInvariant()}_{clientIp}";
    }

    /// <summary>
    /// Per-identifier bucket. The voterId is hashed so emails/phones are not stored in the in-memory key.
    /// </summary>
    internal static string GetIdentifierKey(string path, string normalizedVoterId)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalizedVoterId)));
        return $"{path.ToLowerInvariant()}_id_{hash}";
    }

    internal static string? NormalizePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        return path.TrimEnd('/');
    }

    private async Task<bool> TryRejectIfLimitedAsync(
        HttpContext context,
        ISecurityAuditService securityAuditService,
        string path,
        string clientIp,
        string userAgent,
        IReadOnlyList<(string Key, int MaxRequests, TimeSpan Window, string Bucket)> buckets,
        bool recordAttempts)
    {
        foreach (var (key, maxRequests, window, bucket) in buckets)
        {
            CleanupOldRequests(key, window);
            var requests = _store.RequestLog.GetOrAdd(key, _ => new List<DateTime>());
            if (requests.Count < maxRequests)
            {
                continue;
            }

            _logger.LogWarning("Rate limit exceeded for {Path} ({Bucket})", path, bucket);

            await securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.RateLimitExceeded,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Rate limit exceeded for {path} ({bucket}) - {requests.Count} requests in {window.TotalMinutes} minutes",
                IsSuspicious = true,
                Severity = SecurityEventSeverity.Warning
            });

            context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync($"{{\"error\":\"{TooManyRequestsKey}\"}}");
            return true;
        }

        if (recordAttempts)
        {
            RecordAttempts(buckets);
        }

        return false;
    }

    private void RecordAttempts(
        IReadOnlyList<(string Key, int MaxRequests, TimeSpan Window, string Bucket)> buckets)
    {
        var now = DateTime.UtcNow;
        foreach (var (key, _, _, _) in buckets)
        {
            _store.RequestLog.GetOrAdd(key, _ => new List<DateTime>()).Add(now);
        }
    }

    private async Task WritePayloadTooLargeAsync(HttpContext context, string path, string clientIp)
    {
        _logger.LogWarning("Rejected oversized voter auth body for {Path} from {ClientIp}", path, clientIp);

        context.Response.StatusCode = (int)HttpStatusCode.RequestEntityTooLarge;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync($"{{\"error\":\"{PayloadTooLargeKey}\"}}");
    }

    private void CleanupOldRequests(string clientKey, TimeSpan window)
    {
        if (_store.RequestLog.TryGetValue(clientKey, out var requests))
        {
            var cutoff = DateTime.UtcNow - window;
            requests.RemoveAll(r => r < cutoff);

            if (requests.Count == 0)
            {
                _store.RequestLog.TryRemove(clientKey, out _);
            }
        }
    }
}
