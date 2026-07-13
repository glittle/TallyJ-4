using System.Net;
using System.Text;
using System.Text.Json;
using Backend.DTOs.Auth;
using FluentAssertions;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Ensures SuperAdmin APIs are enforced server-side. Client flags (e.g. pinia isSuperAdmin)
/// are irrelevant — only JWT email vs SuperAdmin:Emails grants access.
/// </summary>
public class SuperAdminAuthorizationTests : IntegrationTestBase
{
    public SuperAdminAuthorizationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Theory]
    [InlineData("/api/superadmin/dashboard/summary")]
    [InlineData("/api/superadmin/dashboard/elections")]
    [InlineData("/api/superadmin/users")]
    public async Task SuperAdminEndpoints_ReturnForbidden_ForAuthenticatedNonSuperAdmin(string path)
    {
        var cookies = await LoginAndGetCookiesAsync("test@tallyj.com", "Tester1234!X");

        var request = new HttpRequestMessage(HttpMethod.Get, path);
        AttachCookies(request, cookies);

        var response = await Client.SendAsync(request);

        // Authenticated but fails SuperAdmin policy → 403 (not 401)
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SuperAdminSummary_IsAuthorized_ForConfiguredSuperAdmin()
    {
        // admin@tallyj.test is in SuperAdmin:Emails (appsettings)
        var cookies = await LoginAndGetCookiesAsync("admin@tallyj.test", "TestPass123!");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/superadmin/dashboard/summary");
        AttachCookies(request, cookies);

        var response = await Client.SendAsync(request);

        // Policy must succeed for configured super admins (not 401/403).
        // Other status codes (e.g. 500 from test DB) still prove authz was granted.
        response.StatusCode.Should().NotBe(HttpStatusCode.Unauthorized);
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SuperAdminEndpoints_ReturnUnauthorized_WhenAnonymous()
    {
        var response = await Client.GetAsync("/api/superadmin/dashboard/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SuperAdminEndpoints_IgnoreClientSuperAdminClaimInJwt_WhenEmailNotInList()
    {
        // Even if a client could somehow attach an isSuperAdmin claim, authorization uses
        // email claim vs server config only — not a role/claim named isSuperAdmin.
        var cookies = await LoginAndGetCookiesAsync("test@tallyj.com", "Tester1234!X");

        var request = new HttpRequestMessage(HttpMethod.Get, "/api/superadmin/dashboard/summary");
        AttachCookies(request, cookies);
        // Spurious client headers must not elevate privileges
        request.Headers.TryAddWithoutValidation("X-Is-SuperAdmin", "true");
        request.Headers.TryAddWithoutValidation("isSuperAdmin", "true");

        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<Dictionary<string, string>> LoginAndGetCookiesAsync(string email, string password)
    {
        var loginRequest = new LoginRequest { Email = email, Password = password };
        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        var loginResponse = await Client.PostAsync("/api/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return GetCookiesFromResponse(loginResponse);
    }

    private static void AttachCookies(HttpRequestMessage request, Dictionary<string, string> cookies)
    {
        foreach (var cookie in cookies)
        {
            var cookieValue = ExtractCookieValue(cookie.Value);
            request.Headers.Add("Cookie", $"{cookie.Key}={cookieValue}");
        }
    }

    private static Dictionary<string, string> GetCookiesFromResponse(HttpResponseMessage response)
    {
        var cookies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var cookie in setCookies)
            {
                var parts = cookie.Split(';', 2);
                var nameValue = parts[0].Split('=', 2);
                if (nameValue.Length == 2)
                {
                    cookies[nameValue[0]] = cookie;
                }
            }
        }

        return cookies;
    }

    private static string ExtractCookieValue(string cookieString)
    {
        var parts = cookieString.Split('=', 2);
        var rawValue = parts.Length == 2 ? parts[1].Split(';')[0] : "";
        return Uri.UnescapeDataString(rawValue);
    }
}
