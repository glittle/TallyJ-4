using System.Net;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Backend.Application.DTOs.Auth;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Integration tests for rate limiting functionality on authentication endpoints.
/// </summary>
public class RateLimitingTests : IntegrationTestBase
{
    public RateLimitingTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Login_WithinRateLimit_Succeeds()
    {
        // Arrange - ensure test user exists
        await GetAuthTokenAsync("admin@tallyj.test", "TestPass123!");

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
    }

    [Fact]
    public async Task Login_ExceedsRateLimit_Returns429()
    {
        // Arrange
        var loginRequest = new LoginRequest
        {
            Email = "admin@tallyj.test",
            Password = "WrongPassword"
        };

        // Act - Make 6 login attempts (exceeds 5 per minute limit)
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                Encoding.UTF8,
                "application/json");

            lastResponse = await Client.PostAsync("/api/auth/login", content);

            // Small delay to ensure requests are processed
            await Task.Delay(200);
        }

        // Assert - Last request should be rate limited
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Register_WithinRateLimit_Succeeds()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "TestPass123!",
            DisplayName = "Test User"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/registerAccount", content);

        // Assert - Should succeed (even if user already exists, rate limiting should allow the request)
        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Register_ExceedsRateLimit_Returns429()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "TestPass123!",
            DisplayName = "Test User"
        };

        // Act - Make 4 registration attempts (exceeds 3 per hour limit)
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 4; i++)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(registerRequest),
                Encoding.UTF8,
                "application/json");

            lastResponse = await Client.PostAsync("/api/auth/registerAccount", content);

            // Small delay to ensure requests are processed
            await Task.Delay(200);
        }

        // Assert - Last request should be rate limited
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Verify2FA_WithinRateLimit_Succeeds()
    {
        // Arrange
        var verifyRequest = new Verify2FARequest
        {
            Email = "admin@tallyj.test",
            Code = "123456" // Invalid code, but should not be rate limited initially
        };

        var content = new StringContent(
            JsonSerializer.Serialize(verifyRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/verify2fa", content);

        // Assert - Should return BadRequest for invalid code, but not rate limited
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Verify2FA_ExceedsRateLimit_Returns429()
    {
        // Arrange
        var verifyRequest = new Verify2FARequest
        {
            Email = "admin@tallyj.test",
            Code = "123456"
        };

        // Act - Make 11 verification attempts (exceeds 10 per minute limit)
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 11; i++)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(verifyRequest),
                Encoding.UTF8,
                "application/json");

            lastResponse = await Client.PostAsync("/api/auth/verify2fa", content);

            // Small delay to ensure requests are processed
            await Task.Delay(200);
        }

        // Assert - Last request should be rate limited
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ForgotPassword_WithinRateLimit_Succeeds()
    {
        // Arrange
        var forgotRequest = new ForgotPasswordRequest
        {
            Email = "admin@tallyj.test"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(forgotRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/forgotPassword", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_ExceedsRateLimit_Returns429()
    {
        // Arrange
        var forgotRequest = new ForgotPasswordRequest
        {
            Email = "admin@tallyj.test"
        };

        // Act - Make 4 forgot password attempts (exceeds 3 per hour limit)
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 4; i++)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(forgotRequest),
                Encoding.UTF8,
                "application/json");

            lastResponse = await Client.PostAsync("/api/auth/forgotPassword", content);

            // Small delay to ensure requests are processed
            await Task.Delay(200);
        }

        // Assert - Last request should be rate limited
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task ResetPassword_WithinRateLimit_Succeeds()
    {
        // Arrange
        var resetRequest = new ResetPasswordRequest
        {
            Email = "admin@tallyj.test",
            Token = "invalid-token",
            NewPassword = "NewPass123!"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(resetRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/resetPassword", content);

        // Assert - Should return BadRequest for invalid token, but not rate limited
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ResetPassword_ExceedsRateLimit_Returns429()
    {
        // Arrange
        var resetRequest = new ResetPasswordRequest
        {
            Email = "admin@tallyj.test",
            Token = "invalid-token",
            NewPassword = "NewPass123!"
        };

        // Act - Make 4 reset password attempts (exceeds 3 per hour limit)
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 4; i++)
        {
            var content = new StringContent(
                JsonSerializer.Serialize(resetRequest),
                Encoding.UTF8,
                "application/json");

            lastResponse = await Client.PostAsync("/api/auth/resetPassword", content);

            // Small delay to ensure requests are processed
            await Task.Delay(200);
        }

        // Assert - Last request should be rate limited
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}


