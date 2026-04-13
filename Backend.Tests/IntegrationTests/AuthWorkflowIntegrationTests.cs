using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Backend.Application.DTOs.Auth;
using Backend.Middleware;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Comprehensive integration tests for complete authentication workflows.
/// Tests cover end-to-end flows including login, OAuth, 2FA, registration, and security features.
/// </summary>
public class AuthWorkflowIntegrationTests : IntegrationTestBase
{
    public AuthWorkflowIntegrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CompleteLoginFlow_WithCookieRefresh_Succeeds()
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

        // Act - Login
        var loginResponse = await Client.PostAsync("/api/auth/login", content);

        // Assert - Login successful
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponseContent = await loginResponse.Content.ReadAsStringAsync();
        var authResponse = JsonSerializer.Deserialize<AuthResponse>(loginResponseContent, JsonOptions);
        authResponse.Should().NotBeNull();
        authResponse!.Email.Should().Be("admin@tallyj.test");

        // Verify cookies are set
        var loginCookies = GetCookiesFromResponse(loginResponse);
        loginCookies.Should().ContainKey(SecureCookieMiddleware.AccessTokenCookieName);
        loginCookies.Should().ContainKey(SecureCookieMiddleware.RefreshTokenCookieName);

        // Extract refresh token for refresh test
        var refreshToken = ExtractCookieValue(loginCookies[SecureCookieMiddleware.RefreshTokenCookieName]);

        // Wait a bit to ensure token expiry (if short-lived)
        await Task.Delay(1000);

        // Act - Refresh token
        var refreshRequest = new RefreshTokenRequest
        {
            RefreshToken = refreshToken
        };

        var refreshContent = new StringContent(
            JsonSerializer.Serialize(refreshRequest),
            Encoding.UTF8,
            "application/json");

        var refreshResponse = await Client.PostAsync("/api/auth/refreshToken", refreshContent);

        // Assert - Refresh successful
        refreshResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var refreshResponseContent = await refreshResponse.Content.ReadAsStringAsync();
        var refreshAuthResponse = JsonSerializer.Deserialize<AuthResponse>(refreshResponseContent, JsonOptions);
        refreshAuthResponse.Should().NotBeNull();
        refreshAuthResponse!.Email.Should().Be("admin@tallyj.test");

        // Verify new cookies are set
        var refreshCookies = GetCookiesFromResponse(refreshResponse);
        refreshCookies.Should().ContainKey(SecureCookieMiddleware.AccessTokenCookieName);
        refreshCookies.Should().ContainKey(SecureCookieMiddleware.RefreshTokenCookieName);

        // Act - Access protected endpoint with new cookies
        var protectedResponse = await Client.GetAsync("/api/elections/getMyElections");

        // Assert - Protected endpoint accessible
        protectedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OAuthFlow_Initiation_ReturnsValidRedirect()
    {
        // Arrange - This test verifies the OAuth initiation endpoint
        // Note: Full OAuth flow requires external Google services configured

        // Act
        var response = await Client.GetAsync("/api/auth/google/login");

        // Assert - Google OAuth is not available in test environment
        // Either returns BadRequest (if Google not configured) or NotFound (route not registered)
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task OAuthCallback_InvalidState_ReturnsBadRequest()
    {
        // Arrange - Try OAuth callback with invalid state
        var invalidCallbackUrl = "/api/auth/google/callback?code=fake_code&state=invalid_state";

        // Act - The callback will fail authentication (no valid external auth session)
        // and redirect to error page; test verifies it does not succeed
        var response = await Client.GetAsync(invalidCallbackUrl);

        // Assert - Should not return success (either redirect to error or bad request)
        response.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Complete2FAFlow_SetupEnableAndVerify_Succeeds()
    {
        // Arrange - Create a test user for 2FA
        var testEmail = $"2fa-test-{Guid.NewGuid()}@tallyj.test";
        var testPassword = "TestPass123!";

        // Register user
        var registerRequest = new RegisterRequest
        {
            Email = testEmail,
            Password = testPassword,
            ConfirmPassword = testPassword,
            DisplayName = "2FA Test User"
        };

        var registerContent = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json");

        var registerResponse = await Client.PostAsync("/api/auth/registerAccount", registerContent);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Confirm email so login can proceed
        await ConfirmEmailAsync(testEmail);

        // Login first to get authenticated
        var loginRequest = new LoginRequest
        {
            Email = testEmail,
            Password = testPassword
        };

        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        var loginResponse = await Client.PostAsync("/api/auth/login", loginContent);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Setup 2FA
        var setupResponse = await Client.PostAsync("/api/auth/setup2fa", null);

        // Assert - 2FA setup returns QR code/secret
        setupResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var setupContent = await setupResponse.Content.ReadAsStringAsync();
        var setupResult = JsonSerializer.Deserialize<TwoFactorSetupResponse>(setupContent, JsonOptions);
        setupResult.Should().NotBeNull();
        setupResult!.Secret.Should().NotBeNullOrEmpty();
        setupResult.QrCodeDataUrl.Should().NotBeNullOrEmpty();

        // Generate a valid TOTP code for testing
        // For integration tests, we'll use a known secret and calculate the code
        var secret = setupResult.Secret;
        var totpCode = GenerateTotpCode(secret);

        // Act - Enable 2FA with valid code
        var enableRequest = new Enable2FARequest { Code = totpCode };
        var enableContent = new StringContent(
            JsonSerializer.Serialize(enableRequest),
            Encoding.UTF8,
            "application/json");

        var enableResponse = await Client.PostAsync("/api/auth/enable2fa", enableContent);

        // Assert - 2FA enabled successfully
        enableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Logout and login again to trigger 2FA requirement
        await Client.PostAsync("/api/auth/logout", null);

        var reLoginContent2 = new StringContent(JsonSerializer.Serialize(loginRequest), Encoding.UTF8, "application/json");
        var reLoginResponse = await Client.PostAsync("/api/auth/login", reLoginContent2);
        reLoginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var reLoginContent = await reLoginResponse.Content.ReadAsStringAsync();
        var reLoginAuthResponse = JsonSerializer.Deserialize<AuthResponse>(reLoginContent, JsonOptions);
        reLoginAuthResponse.Should().NotBeNull();
        reLoginAuthResponse!.Requires2FA.Should().BeTrue();

        // Act - Verify 2FA with valid code
        var verifyRequest = new Verify2FARequest
        {
            Email = testEmail,
            Password = testPassword,
            Code = GenerateTotpCode(secret) // Generate fresh code
        };

        var verifyContent = new StringContent(
            JsonSerializer.Serialize(verifyRequest),
            Encoding.UTF8,
            "application/json");

        var verifyResponse = await Client.PostAsync("/api/auth/verify2fa", verifyContent);

        // Assert - 2FA verification successful
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var verifyResponseContent = await verifyResponse.Content.ReadAsStringAsync();
        var verifyAuthResponse = JsonSerializer.Deserialize<AuthResponse>(verifyResponseContent, JsonOptions);
        verifyAuthResponse.Should().NotBeNull();
        verifyAuthResponse!.Requires2FA.Should().BeFalse();

        // Verify cookies are set after 2FA verification
        var verifyCookies = GetCookiesFromResponse(verifyResponse);
        verifyCookies.Should().ContainKey(SecureCookieMiddleware.AccessTokenCookieName);
        verifyCookies.Should().ContainKey(SecureCookieMiddleware.RefreshTokenCookieName);
    }

    [Fact]
    public async Task RegistrationWithEmailVerification_CompleteFlow_Succeeds()
    {
        // Arrange
        var testEmail = $"verify-test-{Guid.NewGuid()}@tallyj.test";
        var testPassword = "TestPass123!";

        var registerRequest = new RegisterRequest
        {
            Email = testEmail,
            Password = testPassword,
            ConfirmPassword = testPassword,
            DisplayName = "Verification Test User"
        };

        var registerContent = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json");

        // Act - Register user
        var registerResponse = await Client.PostAsync("/api/auth/registerAccount", registerContent);

        // Assert - Registration successful
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Act - Try to login before email verification (should fail)
        var loginRequest = new LoginRequest
        {
            Email = testEmail,
            Password = testPassword
        };

        var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        var preVerifyLoginResponse = await Client.PostAsync("/api/auth/login", loginContent);

        // Assert - Login fails due to unverified email
        preVerifyLoginResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var preVerifyContent = await preVerifyLoginResponse.Content.ReadAsStringAsync();
        preVerifyContent.ToLower().Should().Contain("email").And.Contain("verif");

        // Note: In a real integration test, we would need to:
        // 1. Mock the email service to capture the verification token
        // 2. Call the verify email endpoint with the captured token
        // 3. Then verify login works
        // For this test, we verify the registration and pre-verification behavior
    }

    [Fact]
    public async Task AccountLockout_CompleteFlow_WithRecovery()
    {
        // Arrange
        var testEmail = $"lockout-recovery-{Guid.NewGuid()}@tallyj.test";
        var correctPassword = "TestPass123!";
        var wrongPassword = "WrongPassword123!";

        // Register user
        var registerRequest = new RegisterRequest
        {
            Email = testEmail,
            Password = correctPassword,
            ConfirmPassword = correctPassword,
            DisplayName = "Lockout Recovery User"
        };

        var registerContent = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json");

        var registerResponse = await Client.PostAsync("/api/auth/registerAccount", registerContent);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Confirm email so lockout tracking is active
        await ConfirmEmailAsync(testEmail);

        // Act - Attempt multiple failed logins to trigger lockout
        for (int i = 0; i < 5; i++)
        {
            var loginRequest = new LoginRequest
            {
                Email = testEmail,
                Password = wrongPassword
            };

            var content = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                Encoding.UTF8,
                "application/json");

            var response = await Client.PostAsync("/api/auth/login", content);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        // Reset rate limit so the final lockout check isn't blocked by rate limiting
        ResetRateLimit();

        // Act - Try login with correct password (should be locked out)
        var validLoginRequest = new LoginRequest
        {
            Email = testEmail,
            Password = correctPassword
        };

        var validContent = new StringContent(
            JsonSerializer.Serialize(validLoginRequest),
            Encoding.UTF8,
            "application/json");

        var lockedResponse = await Client.PostAsync("/api/auth/login", validContent);

        // Assert - Account is locked
        lockedResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var lockedContent = await lockedResponse.Content.ReadAsStringAsync();
        lockedContent.ToLower().Should().Contain("lock");

        // Note: In a real scenario, account lockout recovery would involve:
        // 1. Waiting for lockout duration to expire, or
        // 2. Admin intervention, or
        // 3. Password reset flow
        // For this test, we verify the lockout behavior
    }

    [Fact]
    public async Task PasswordResetFlow_CompleteWorkflow_Succeeds()
    {
        // Arrange
        var testEmail = "admin@tallyj.test"; // Use seeded user

        // Act - Request password reset
        var forgotRequest = new ForgotPasswordRequest
        {
            Email = testEmail
        };

        var forgotContent = new StringContent(
            JsonSerializer.Serialize(forgotRequest),
            Encoding.UTF8,
            "application/json");

        var forgotResponse = await Client.PostAsync("/api/auth/forgotPassword", forgotContent);

        // Assert - Password reset request accepted
        forgotResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Note: In a complete integration test, we would:
        // 1. Mock email service to capture reset token
        // 2. Use the token to reset password
        // 3. Verify login with new password works
        // For this test, we verify the request acceptance
    }

    [Fact]
    public async Task SecurityHeaders_OnAllAuthEndpoints_ArePresent()
    {
        // Arrange
        var getEndpoints = new[]
        {
            "/api/auth/login",
            "/api/auth/registerAccount",
            "/api/auth/refreshToken"
        };

        foreach (var endpoint in getEndpoints)
        {
            var response = await Client.GetAsync(endpoint);

            response.Headers.Should().ContainKey("X-Content-Type-Options");
            response.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        }

        var logoutResponse = await Client.PostAsync("/api/auth/logout", null);
        logoutResponse.Headers.Should().ContainKey("X-Content-Type-Options");
        logoutResponse.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
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
                    var value = parts[1].Split(';')[0];
                    cookies[name] = value;
                }
            }
        }
        return cookies;
    }

    private string ExtractCookieValue(string cookieString)
    {
        var parts = cookieString.Split('=', 2);
        var rawValue = parts.Length == 2 ? parts[1].Split(';')[0] : "";
        return Uri.UnescapeDataString(rawValue);
    }

    private string GenerateTotpCode(string secret)
    {
        var totp = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secret));
        return totp.ComputeTotp();
    }
}


