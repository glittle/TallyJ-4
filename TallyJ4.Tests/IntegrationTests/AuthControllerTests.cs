using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TallyJ4.Application.DTOs.Auth;
using TallyJ4.Middleware;
using Xunit;

namespace TallyJ4.Tests.IntegrationTests;

public class AuthControllerTests : IntegrationTestBase
{
    public AuthControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_SetsSecureCookies_AndReturnsUserInfo()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "admin@tallyj.test",
            Password = "TestPass123!"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, JsonOptions);

        authResponse.Should().NotBeNull();
        authResponse!.Email.Should().Be("admin@tallyj.test");
        authResponse.AuthMethod.Should().Be("Local");
        authResponse.Requires2FA.Should().BeFalse();

        // Check that secure cookies are set
        var cookies = GetCookiesFromResponse(response);
        cookies.Should().ContainKey(SecureCookieMiddleware.AccessTokenCookieName);
        cookies.Should().ContainKey(SecureCookieMiddleware.RefreshTokenCookieName);
        cookies.Should().ContainKey(SecureCookieMiddleware.UserEmailCookieName);
        cookies.Should().ContainKey(SecureCookieMiddleware.AuthMethodCookieName);

        // Verify cookie attributes
        var accessTokenCookie = cookies[SecureCookieMiddleware.AccessTokenCookieName];
        accessTokenCookie.Should().Contain("HttpOnly");
        accessTokenCookie.Should().Contain("Secure");
        accessTokenCookie.Should().Contain("SameSite=Strict");

        var refreshTokenCookie = cookies[SecureCookieMiddleware.RefreshTokenCookieName];
        refreshTokenCookie.Should().Contain("HttpOnly");
        refreshTokenCookie.Should().Contain("Secure");
        refreshTokenCookie.Should().Contain("SameSite=Strict");

        var userEmailCookie = cookies[SecureCookieMiddleware.UserEmailCookieName];
        userEmailCookie.Should().NotContain("HttpOnly"); // User info cookies are readable
        userEmailCookie.Should().Contain("Secure");
        userEmailCookie.Should().Contain("SameSite=Strict");
    }

    [Fact]
    public async Task RefreshToken_SetsNewSecureCookies()
    {
        // Arrange - First login to get initial tokens
        var loginRequest = new LoginRequest
        {
            Email = "admin@tallyj.test",
            Password = "TestPass123!"
        };

        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        var loginResponse = await Client.PostAsync("/api/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginCookies = GetCookiesFromResponse(loginResponse);
        var refreshToken = ExtractCookieValue(loginCookies[SecureCookieMiddleware.RefreshTokenCookieName]);

        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };

        var refreshContent = new StringContent(
            JsonSerializer.Serialize(refreshRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/refreshToken", refreshContent);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var responseContent = await response.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(responseContent, JsonOptions);

        authResponse.Should().NotBeNull();
        authResponse!.Email.Should().Be("admin@tallyj.test");

        // Check that new secure cookies are set
        var cookies = GetCookiesFromResponse(response);
        cookies.Should().ContainKey(SecureCookieMiddleware.AccessTokenCookieName);
        cookies.Should().ContainKey(SecureCookieMiddleware.RefreshTokenCookieName);
    }

    [Fact]
    public async Task Logout_ClearsAllAuthCookies()
    {
        // Arrange - First login to set cookies
        var loginRequest = new LoginRequest
        {
            Email = "admin@tallyj.test",
            Password = "TestPass123!"
        };

        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        var loginResponse = await Client.PostAsync("/api/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Logout
        var logoutResponse = await Client.PostAsync("/api/auth/logout", null);

        // Assert
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Check that cookies are cleared (expired)
        var cookies = GetCookiesFromResponse(logoutResponse);
        cookies.Should().ContainKey(SecureCookieMiddleware.AccessTokenCookieName);
        cookies.Should().ContainKey(SecureCookieMiddleware.RefreshTokenCookieName);
        cookies.Should().ContainKey(SecureCookieMiddleware.UserEmailCookieName);
        cookies.Should().ContainKey(SecureCookieMiddleware.UserNameCookieName);
        cookies.Should().ContainKey(SecureCookieMiddleware.AuthMethodCookieName);

        // All cookies should be expired (contain max-age=0 or expired date)
        foreach (var cookie in cookies.Values)
        {
            cookie.Should().MatchRegex("(max-age=0|expires=[^;]+;.*max-age=0)");
        }
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsBadRequest_NoCookiesSet()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "admin@tallyj.test",
            Password = "WrongPassword"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/login", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        // Check that no auth cookies are set
        var cookies = GetCookiesFromResponse(response);
        cookies.Should().NotContainKey(SecureCookieMiddleware.AccessTokenCookieName);
        cookies.Should().NotContainKey(SecureCookieMiddleware.RefreshTokenCookieName);
        cookies.Should().NotContainKey(SecureCookieMiddleware.UserEmailCookieName);
    }

    private Dictionary<string, string> GetCookiesFromResponse(HttpResponseMessage response)
    {
        var cookies = new Dictionary<string, string>();
        if (response.Headers.TryGetValues("Set-Cookie", out var cookieHeaders))
        {
            foreach (var cookieHeader in cookieHeaders)
            {
                var parts = cookieHeader.Split('=', 2);
                if (parts.Length == 2)
                {
                    var name = parts[0];
                    var value = parts[1].Split(';')[0]; // Get value before semicolon
                    cookies[name] = cookieHeader; // Store full cookie string for attribute checking
                }
            }
        }
        return cookies;
    }

    private string ExtractCookieValue(string cookieString)
    {
        var parts = cookieString.Split('=', 2);
        return parts.Length == 2 ? parts[1].Split(';')[0] : "";
    }
}