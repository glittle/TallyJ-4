using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Backend.Context;
using Backend.Controllers;
using Backend.DTOs.Auth;
using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Helpers;
using Backend.Identity;
using Backend.Middleware;
using Backend.Services.Auth;
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

        // Act - one more bad-credential attempt than the per-IP failure cap
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < RateLimitingMiddleware.LoginIpMaxRequests + 1; i++)
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
        for (int i = 0; i < RateLimitingMiddleware.LoginIpMaxRequests + 1; i++)
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
    public async Task Login_SpoofedLeftmostXForwardedFor_DoesNotEvadeLimit()
    {
        var loginRequest = new LoginRequest
        {
            Email = "rate-limit-spoof@example.com",
            Password = "WrongPassword"
        };

        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < RateLimitingMiddleware.LoginIpMaxRequests + 1; i++)
        {
            lastResponse = await PostJsonWithForwardedFor(
                "/api/auth/login",
                loginRequest,
                $"198.51.100.{i + 1}, 203.0.113.70, 10.0.0.4");
            await Task.Delay(50);
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await lastResponse.Content.ReadAsStringAsync();
        body.Should().Contain(RateLimitingMiddleware.TooManyRequestsKey);
    }

    [Fact]
    public async Task Login_SameIp_ThirtySuccesses_AllSucceed()
    {
        var loginRequest = new LoginRequest
        {
            Email = "admin@tallyj.test",
            Password = "TestPass123!"
        };

        for (var i = 0; i < 30; i++)
        {
            var response = await PostJsonWithForwardedFor(
                "/api/auth/login",
                loginRequest,
                "192.0.2.20");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }
    }

    [Fact]
    public async Task Login_SameIp_SuccessAfterFailures_DoesNotConsumeTheBucket()
    {
        var bad = new LoginRequest
        {
            Email = "rate-limit-mixed@example.com",
            Password = "WrongPassword"
        };
        var good = new LoginRequest
        {
            Email = "admin@tallyj.test",
            Password = "TestPass123!"
        };
        const string ip = "192.0.2.21";

        for (var i = 0; i < 5; i++)
        {
            var failed = await PostJsonWithForwardedFor("/api/auth/login", bad, ip);
            failed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        var success = await PostJsonWithForwardedFor("/api/auth/login", good, ip);
        success.StatusCode.Should().Be(HttpStatusCode.OK);

        for (var i = 0; i < RateLimitingMiddleware.LoginIpMaxRequests - 5; i++)
        {
            var failed = await PostJsonWithForwardedFor("/api/auth/login", bad, ip);
            failed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        var limited = await PostJsonWithForwardedFor("/api/auth/login", bad, ip);
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await limited.Content.ReadAsStringAsync()).Should().Contain(RateLimitingMiddleware.TooManyRequestsKey);
    }

    [Fact]
    public async Task Login_SameIp_RepeatedBadTwoFactorCodes_Reach429()
    {
        const string ip = "192.0.2.40";
        const string password = "TestPass123!";
        // One account locks at 5. The locking reply does not consume the IP bucket,
        // so each account contributes 4 counted invalid-code replies.
        var accounts = new List<(string Email, string WrongCode)>();
        for (var i = 0; i < 5; i++)
        {
            var email = $"2fa-ip-{i}-{Guid.NewGuid():N}@example.com";
            var secret = await EnableTwoFactorAsync(email, password);
            accounts.Add((email, WrongTotp(secret)));
        }

        foreach (var (email, wrongCode) in accounts)
        {
            for (var attempt = 0; attempt < 4; attempt++)
            {
                var failed = await PostLoginAsync(email, password, wrongCode, ip);
                failed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                (await failed.Content.ReadAsStringAsync()).Should().Contain("auth.errors.invalid2FACode");
            }
        }

        var limited = await PostLoginAsync(accounts[0].Email, password, accounts[0].WrongCode, ip);
        limited.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await limited.Content.ReadAsStringAsync()).Should().Contain(RateLimitingMiddleware.TooManyRequestsKey);
    }

    [Fact]
    public async Task Login_BadTwoFactorCode_LocksTheAccount()
    {
        const string ip = "192.0.2.41";
        const string password = "TestPass123!";
        var email = $"2fa-lock-{Guid.NewGuid():N}@example.com";
        var secret = await EnableTwoFactorAsync(email, password);
        var wrongCode = WrongTotp(secret);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var failed = await PostLoginAsync(email, password, wrongCode, ip);
            failed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await failed.Content.ReadAsStringAsync();
            body.Should().Contain("auth.errors.invalid2FACode");
            body.Should().NotContain("auth.errors.accountLocked");
        }

        var locking = await PostLoginAsync(email, password, wrongCode, ip);
        locking.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await locking.Content.ReadAsStringAsync()).Should().Contain("auth.errors.accountLocked");

        var correctCode = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secret)).ComputeTotp();
        var stillLocked = await PostLoginAsync(email, password, correctCode, ip);
        stillLocked.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await stillLocked.Content.ReadAsStringAsync()).Should().Contain("auth.errors.accountLocked");
    }

    [Fact]
    public async Task Login_EmptyTwoFactorPrompt_DoesNotResetTheAccessFailedCount()
    {
        const string ip = "192.0.2.43";
        const string password = "TestPass123!";
        var email = $"2fa-prompt-{Guid.NewGuid():N}@example.com";
        var secret = await EnableTwoFactorAsync(email, password);
        var wrongCode = WrongTotp(secret);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var failed = await PostLoginAsync(email, password, wrongCode, ip);
            failed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await failed.Content.ReadAsStringAsync()).Should().Contain("auth.errors.invalid2FACode");
        }

        var prompt = await PostLoginAsync(email, password, "", ip);
        prompt.StatusCode.Should().Be(HttpStatusCode.OK);
        var promptBody = await prompt.Content.ReadFromJsonAsync<AuthResponse>(JsonOptions);
        promptBody!.Requires2FA.Should().BeTrue();

        var locking = await PostLoginAsync(email, password, wrongCode, ip);
        locking.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await locking.Content.ReadAsStringAsync()).Should().Contain("auth.errors.accountLocked");
    }

    [Fact]
    public async Task Login_WithoutTwoFactor_SuccessfulLogin_ResetsTheAccessFailedCount()
    {
        const string ip = "192.0.2.42";
        const string password = "TestPass123!";
        const string wrongPassword = "WrongPassword123!";
        var email = $"lock-reset-{Guid.NewGuid():N}@example.com";
        await CreateTestUserAsync(email, password, email);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var failed = await PostLoginAsync(email, wrongPassword, null, ip);
            failed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await failed.Content.ReadAsStringAsync();
            body.Should().NotContain("auth.errors.accountLocked");
        }

        var success = await PostLoginAsync(email, password, null, ip);
        success.StatusCode.Should().Be(HttpStatusCode.OK);

        for (var attempt = 0; attempt < 4; attempt++)
        {
            var failed = await PostLoginAsync(email, wrongPassword, null, ip);
            failed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await failed.Content.ReadAsStringAsync()).Should().NotContain("auth.errors.accountLocked");
        }
    }

    [Fact]
    public async Task RequestCode_SameVoterId_ExceedsIdentifierLimit_Returns429()
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
    public void VoterCodeIpCeiling_IsLooseComparedWithTheIdentifierBucket()
    {
        Assert.Equal(5, RateLimitingMiddleware.VoterIdentifierMaxRequests);
        Assert.True(RateLimitingMiddleware.VoterVenueIpMaxRequests >= 60);
    }

    [Fact]
    public async Task RequestCode_AndVerifyCode_SameVenueIp_ManyVoters_StayUnderTheLooseCeiling()
    {
        const string venueIp = "203.0.113.77";
        for (var i = 0; i < 24; i++)
        {
            var requestCode = await PostJsonWithForwardedFor(
                "/api/online-voting/requestCode",
                new RequestCodeDto
                {
                    VoterId = $"loose-ceiling-{i}@example.com",
                    VoterIdType = "E",
                    DeliveryMethod = "email"
                },
                $"198.51.100.{i + 1}, {venueIp}");
            requestCode.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);

            var verifyCode = await PostJsonWithForwardedFor(
                "/api/online-voting/verifyCode",
                new VerifyCodeDto
                {
                    VoterId = $"loose-verify-{i}@example.com",
                    VerifyCode = "XXXXXX"
                },
                $"198.51.100.{i + 1}, {venueIp}");
            verifyCode.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }
    }

    [Fact]
    public async Task RequestCode_SameVenueIp_DifferentVoterIds_DoesNotRateLimitAtSixth()
    {
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await PostJsonWithForwardedFor(
                "/api/online-voting/requestCode",
                new RequestCodeDto
                {
                    VoterId = $"venue-voter-{i}@example.com",
                    VoterIdType = "E",
                    DeliveryMethod = "email"
                },
                "203.0.113.31");
            await Task.Delay(50);
        }

        lastResponse!.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task RequestCode_SpoofedLeftmostXForwardedFor_SameVoterId_DoesNotEvade()
    {
        var request = new RequestCodeDto
        {
            VoterId = "spoof-voter@example.com",
            VoterIdType = "E",
            DeliveryMethod = "email"
        };

        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await PostJsonWithForwardedFor(
                "/api/online-voting/requestCode",
                request,
                $"198.51.100.{i + 1}, 203.0.113.71, 10.0.0.4");
            await Task.Delay(50);
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await lastResponse.Content.ReadAsStringAsync();
        body.Should().Contain(RateLimitingMiddleware.TooManyRequestsKey);
    }

    [Fact]
    public async Task RequestCode_PaddedBodyWithRealVoterId_Returns413()
    {
        var response = await PostRawJsonWithForwardedFor(
            "/api/online-voting/requestCode",
            PaddedRequestCodeJson("padded-evade@example.com"),
            "203.0.113.80");

        response.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(RateLimitingMiddleware.PayloadTooLargeKey);
    }

    [Fact]
    public async Task RequestCode_PaddedBody_DoesNotEvadeIdentifierLimit()
    {
        var voterId = "padded-same-id@example.com";
        var request = new RequestCodeDto
        {
            VoterId = voterId,
            VoterIdType = "E",
            DeliveryMethod = "email"
        };

        HttpResponseMessage? lastNormal = null;
        for (int i = 0; i < 6; i++)
        {
            lastNormal = await PostJsonWithForwardedFor(
                "/api/online-voting/requestCode",
                request,
                "203.0.113.81");
            await Task.Delay(50);
        }

        lastNormal!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);

        var padded = await PostRawJsonWithForwardedFor(
            "/api/online-voting/requestCode",
            PaddedRequestCodeJson(voterId),
            "203.0.113.99");

        padded.StatusCode.Should().Be(HttpStatusCode.RequestEntityTooLarge);
        padded.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task VerifyCode_SameVoterId_ExceedsIdentifierLimit_Returns429()
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
    public async Task VerifyCode_SameVenueIp_DifferentVoterIds_DoesNotRateLimitAtSixth()
    {
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await PostJsonWithForwardedFor(
                "/api/online-voting/verifyCode",
                new VerifyCodeDto
                {
                    VoterId = $"venue-verify-{i}@example.com",
                    VerifyCode = "XXXXXX"
                },
                "203.0.113.41");
            await Task.Delay(50);
        }

        lastResponse!.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task GoogleAuth_SameVenueIp_SixthCall_DoesNotRateLimit()
    {
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await PostJsonWithForwardedFor(
                "/api/online-voting/googleAuth",
                new GoogleAuthForVoterDto { Credential = $"not-a-real-token-{i}" },
                "203.0.113.42");
            await Task.Delay(50);
        }

        lastResponse!.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Register_WithinRateLimit_IsRejectedNotRateLimited()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Email = $"test{Guid.NewGuid()}@example.com",
            Password = "TestPass123!",
            ConfirmPassword = "TestPass123!",
            DisplayName = "Test User"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(registerRequest),
            Encoding.UTF8,
            "application/json");

        // Act
        var response = await Client.PostAsync("/api/auth/registerAccount", content);

        // Open register is disabled (400 + i18n key). Rate limiting must not 429 the first call.
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(AuthController.OpenRegisterDisabledKey);
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

    private async Task<string> EnableTwoFactorAsync(string email, string password)
    {
        await CreateTestUserAsync(email, password, email);

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var encryption = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();

        var secretBytes = new byte[20];
        RandomNumberGenerator.Fill(secretBytes);
        var secret = OtpNet.Base32Encoding.ToString(secretBytes);

        var enabled = await userManager.SetTwoFactorEnabledAsync(user!, true);
        enabled.Succeeded.Should().BeTrue();

        db.TwoFactorTokens.Add(new TwoFactorToken
        {
            TokenGuid = Guid.NewGuid(),
            UserId = user!.Id,
            Secret = encryption.Encrypt(secret),
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow,
            RowVersion = new byte[8]
        });
        await db.SaveChangesAsync();
        return secret;
    }

    private static string WrongTotp(string secret)
    {
        var valid = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secret)).ComputeTotp();
        return valid == "000000" ? "111111" : "000000";
    }

    private Task<HttpResponseMessage> PostLoginAsync(string email, string password, string? twoFactorCode, string forwardedFor)
    {
        return PostJsonWithForwardedFor(
            "/api/auth/login",
            new LoginRequest
            {
                Email = email,
                Password = password,
                TwoFactorCode = twoFactorCode
            },
            forwardedFor);
    }

    private async Task<HttpResponseMessage> PostJsonWithForwardedFor<T>(
        string path,
        T body,
        string forwardedFor)
    {
        return await PostRawJsonWithForwardedFor(
            path,
            JsonSerializer.Serialize(body),
            forwardedFor);
    }

    private async Task<HttpResponseMessage> PostRawJsonWithForwardedFor(
        string path,
        string json,
        string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, path)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Forwarded-For", forwardedFor);
        return await Client.SendAsync(request);
    }

    private static string PaddedRequestCodeJson(string voterId)
    {
        var prefix =
            $"{{\"voterId\":\"{voterId}\",\"voterIdType\":\"E\",\"deliveryMethod\":\"email\",\"pad\":\"";
        var padLength = JsonRequestVoterId.MaxBodyBytes - prefix.Length + 32;
        return prefix + new string('x', padLength) + "\"}";
    }
}


