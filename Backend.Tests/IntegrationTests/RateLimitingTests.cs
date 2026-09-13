using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Backend.DTOs.Auth;
using Backend.DTOs.OnlineVoting;
using Backend.Middleware;
using Xunit;

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
            Email = "rate-limit-login@example.com",
            Password = "WrongPassword"
        };

        // Act - Make 6 login attempts (exceeds 5 per minute limit)
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await PostJsonWithForwardedFor(
                "/api/auth/login",
                loginRequest,
                "203.0.113.50");
            await Task.Delay(50);
        }

        // Assert - Last request should be rate limited
        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await lastResponse.Content.ReadAsStringAsync();
        body.Should().Contain(RateLimitingMiddleware.TooManyRequestsKey);
        body.Should().NotContain("Too many requests. Please try again later.");
    }

    [Fact]
    public async Task Login_TwoClientsBehindOneProxy_AreNotOneBucket()
    {
        var loginRequest = new LoginRequest
        {
            Email = "rate-limit-proxy@example.com",
            Password = "WrongPassword"
        };

        HttpResponseMessage? lastForFirstClient = null;
        for (int i = 0; i < 6; i++)
        {
            lastForFirstClient = await PostJsonWithForwardedFor(
                "/api/auth/login",
                loginRequest,
                "203.0.113.10, 10.0.0.4");
            await Task.Delay(50);
        }

        lastForFirstClient!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var otherClient = await PostJsonWithForwardedFor(
            "/api/auth/login",
            loginRequest,
            "203.0.113.20, 10.0.0.4");

        otherClient.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RequestCode_ExceedsRateLimit_Returns429()
    {
        var request = new RequestCodeDto
        {
            VoterId = "rate-limit@example.com",
            VoterIdType = "E",
            DeliveryMethod = "email"
        };

        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await PostJsonWithForwardedFor(
                "/api/online-voting/requestCode",
                request,
                "203.0.113.30");
            await Task.Delay(50);
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await lastResponse.Content.ReadAsStringAsync();
        body.Should().Contain(RateLimitingMiddleware.TooManyRequestsKey);
    }

    [Fact]
    public async Task VerifyCode_ExceedsRateLimit_Returns429()
    {
        var request = new VerifyCodeDto
        {
            VoterId = "rate-limit@example.com",
            VerifyCode = "XXXXXX"
        };

        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await PostJsonWithForwardedFor(
                "/api/online-voting/verifyCode",
                request,
                "203.0.113.40");
            await Task.Delay(50);
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await lastResponse.Content.ReadAsStringAsync();
        body.Should().Contain(RateLimitingMiddleware.TooManyRequestsKey);
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

    private async Task<HttpResponseMessage> PostJsonWithForwardedFor<T>(
        string path,
        T body,
        string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(
                JsonSerializer.Serialize(body),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        return await Client.SendAsync(request);
    }
}


