using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Backend.Context;
using Backend.Entities;
using Backend.Helpers;
using Backend.Identity;
using Backend.Middleware;
using Backend.Services.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

public class PasswordResetFlowTests : IntegrationTestBase
{
    private const string OldPassword = "OldPassword123!";
    private const string NewPassword = "NewPassword123!";

    public PasswordResetFlowTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task LocalReset_LinkIsSingleUse_LoginRequiresTheNewPassword_AndOldRefreshDies()
    {
        ResetRateLimit();
        var email = UniqueEmail();
        await CreateTestUserAsync(email, OldPassword, "Reset User");
        var firstLogin = await PostLoginAsync(email, OldPassword);
        firstLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        var oldRefresh = ReadCookie(firstLogin, "refresh_token");
        oldRefresh.Should().NotBeNullOrEmpty();

        var forgot = await PostForgotAsync(email, host: "evil.example");
        forgot.StatusCode.Should().Be(HttpStatusCode.OK);
        (await forgot.Content.ReadAsStringAsync()).Should().Contain(PasswordResetService.AcceptedKey);

        var link = LinkFor(email);
        link.Should().StartWith("https://localhost:8095/reset-password?");
        link.Should().NotContain("evil.example");
        var token = QueryHelpers.ParseQuery(new Uri(link).Query)["token"].ToString();
        token.Should().NotBeNullOrEmpty();
        link.Should().Contain("token=" + Uri.EscapeDataString(token));

        var weak = await PostResetAsync(email, token, "Ab1!xyz");
        weak.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await weak.Content.ReadAsStringAsync()).Should().Contain("auth.passwordMinLength8");
        (await weak.Content.ReadAsStringAsync()).Should().NotContain("Ab1!xyz");

        var reset = await PostResetAsync(email, token, NewPassword);
        reset.StatusCode.Should().Be(HttpStatusCode.OK);
        (await reset.Content.ReadAsStringAsync()).Should().Contain(PasswordResetService.CompletedKey);
        reset.Headers.Contains("Set-Cookie").Should().BeFalse();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync(email);
            var tokens = await db.RefreshTokens.Where(row => row.UserId == user!.Id).ToListAsync();
            tokens.Should().NotBeEmpty();
            tokens.Should().OnlyContain(row => row.IsRevoked && row.RevokedReason == "Password reset");
        }

        var bare = Factory.CreateClient();
        var refresh = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refreshToken");
        refresh.Headers.TryAddWithoutValidation("Cookie", $"refresh_token={oldRefresh}");
        var refreshed = await bare.SendAsync(refresh);
        refreshed.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var reused = await PostResetAsync(email, token, "AnotherPassword123!");
        reused.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await reused.Content.ReadAsStringAsync()).Should().Contain(PasswordResetService.InvalidTokenKey);

        var oldPassword = await PostLoginAsync(email, OldPassword);
        oldPassword.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var signedIn = await PostLoginAsync(email, NewPassword);
        signedIn.StatusCode.Should().Be(HttpStatusCode.OK);
        var signedInBody = await signedIn.Content.ReadFromJsonAsync<JsonElement>();
        signedInBody.GetProperty("requires2FA").GetBoolean().Should().BeFalse();
        ReadCookie(signedIn, SecureCookieMiddleware.AccessTokenCookieName).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task ForgotPassword_PerEmailCap_MatchesUnknownAndKnown_AcrossIps()
    {
        ResetRateLimit();
        var email = UniqueEmail();
        for (var i = 1; i <= ForgotPasswordMax(); i++)
        {
            var response = await PostForgotFromIp(email, $"203.0.113.{i}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var blocked = await PostForgotFromIp(email.ToUpperInvariant(), "203.0.113.20");
        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await blocked.Content.ReadAsStringAsync()).Should().Contain("error.tooManyRequests");

        var other = await PostForgotFromIp(UniqueEmail(), "203.0.113.21");
        other.StatusCode.Should().Be(HttpStatusCode.OK);

        var local = UniqueEmail();
        await CreateTestUserAsync(local, OldPassword);
        var known = await PostForgotFromIp(local, "198.51.100.8");
        known.StatusCode.Should().Be(HttpStatusCode.OK);
        (await known.Content.ReadAsStringAsync()).Should().Be(await (await PostForgotFromIp(UniqueEmail(), "198.51.100.9")).Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task VerifyEmail_IsCoveredByTheIpRateLimit()
    {
        ResetRateLimit();
        HttpResponseMessage? last = null;
        for (var i = 0; i < 11; i++)
        {
            last = await Client.PostAsJsonAsync("/api/auth/verifyEmail", new { email = "person@example.com", token = "not-a-token" });
        }

        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await last.Content.ReadAsStringAsync()).Should().Contain("error.tooManyRequests");
    }

    [Fact]
    public async Task UnknownAndOAuth_MatchTheLocalReply_AndDoNotSendMail()
    {
        ResetRateLimit();
        var local = UniqueEmail();
        var oauth = UniqueEmail();
        var missing = UniqueEmail();
        await CreateTestUserAsync(local, OldPassword);
        await CreateExternalUserAsync(oauth);

        var missingReply = await PostForgotAsync(missing);
        var oauthReply = await PostForgotAsync(oauth);
        var localReply = await PostForgotAsync(local);

        missingReply.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await missingReply.Content.ReadAsStringAsync();
        body.Should().Be(await oauthReply.Content.ReadAsStringAsync());
        body.Should().Be(await localReply.Content.ReadAsStringAsync());
        FindMessage(missing).Should().BeNull();
        FindMessage(oauth).Should().BeNull();
        FindMessage(local).Should().NotBeNull();
    }

    [Fact]
    public async Task Reset_PreservesAuthenticator_AndDoesNotSignIn()
    {
        ResetRateLimit();
        var email = UniqueEmail();
        var secret = await EnableTwoFactorAsync(email, OldPassword);
        var token = await RequestTokenAsync(email);

        var reset = await PostResetAsync(email, token, NewPassword);
        reset.StatusCode.Should().Be(HttpStatusCode.OK);
        reset.Headers.Contains("Set-Cookie").Should().BeFalse();

        using (var scope = Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync(email);
            user!.TwoFactorEnabled.Should().BeTrue();
        }

        var prompt = await PostLoginAsync(email, NewPassword);
        prompt.StatusCode.Should().Be(HttpStatusCode.OK);
        (await prompt.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requires2FA").GetBoolean().Should().BeTrue();
        CookieHeader(prompt).Should().NotContain(SecureCookieMiddleware.AccessTokenCookieName + "=");

        var code = new OtpNet.Totp(OtpNet.Base32Encoding.ToBytes(secret)).ComputeTotp();
        var finished = await PostLoginAsync(email, NewPassword, code);
        finished.StatusCode.Should().Be(HttpStatusCode.OK);
        (await finished.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("requires2FA").GetBoolean().Should().BeFalse();
        ReadCookie(finished, SecureCookieMiddleware.AccessTokenCookieName).Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Reset_DoesNotClearLockout()
    {
        ResetRateLimit();
        var email = UniqueEmail();
        await CreateTestUserAsync(email, OldPassword);
        DateTimeOffset lockoutEnd;
        using (var scope = Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync(email);
            lockoutEnd = DateTimeOffset.UtcNow.AddMinutes(15);
            (await users.SetLockoutEndDateAsync(user!, lockoutEnd)).Succeeded.Should().BeTrue();
        }

        var token = await RequestTokenAsync(email);
        (await PostResetAsync(email, token, NewPassword)).StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync(email);
            user!.LockoutEnd.Should().NotBeNull();
            user.LockoutEnd!.Value.Should().BeCloseTo(lockoutEnd, TimeSpan.FromSeconds(1));
        }

        var locked = await PostLoginAsync(email, NewPassword);
        locked.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await locked.Content.ReadAsStringAsync()).ToLowerInvariant().Should().Contain("locked");
    }

    [Fact]
    public async Task Reset_RevocationFailure_RollsBackPassword_AndSuccessfulResetRevokesRefresh()
    {
        ResetRateLimit();
        var email = UniqueEmail();
        await CreateTestUserAsync(email, OldPassword);
        var login = await PostLoginAsync(email, OldPassword);
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var originalRefresh = ReadCookie(login, "refresh_token");
        originalRefresh.Should().NotBeNullOrEmpty();
        var token = await RequestTokenAsync(email);

        PasswordResetService.ForceRefreshRevocationFailure = true;
        try
        {
            var failed = await PostResetAsync(email, token, NewPassword);
            failed.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await failed.Content.ReadAsStringAsync()).Should().Contain(PasswordResetService.FailedKey);
        }
        finally
        {
            PasswordResetService.ForceRefreshRevocationFailure = false;
        }

        using (var scope = Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync(email);
            (await users.CheckPasswordAsync(user!, OldPassword)).Should().BeTrue();
            (await users.CheckPasswordAsync(user!, NewPassword)).Should().BeFalse();
        }

        var rotatedResponse = await PostRefreshAsync(originalRefresh!);
        rotatedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rotatedRefresh = ReadCookie(rotatedResponse, "refresh_token");
        rotatedRefresh.Should().NotBeNullOrEmpty();

        var reset = await PostResetAsync(email, token, NewPassword);
        reset.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync(email);
            (await users.CheckPasswordAsync(user!, NewPassword)).Should().BeTrue();
            var live = await db.RefreshTokens.CountAsync(row => row.UserId == user!.Id && !row.IsRevoked);
            live.Should().Be(0);
        }

        (await PostRefreshAsync(originalRefresh!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await PostRefreshAsync(rotatedRefresh!)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ForgotPassword_DuplicateEmailFields_DoNotSendOrSpendTheTargetCap()
    {
        ResetRateLimit();
        var target = UniqueEmail();
        var decoy = UniqueEmail();
        await CreateTestUserAsync(target, OldPassword);

        var duplicates = new[]
        {
            $"{{\"email\":\"{decoy}\",\"Email\":\"{target}\"}}",
            $"{{\"Email\":\"{target}\",\"email\":\"{decoy}\"}}",
            $"{{\"note\":\"x\",\"email\":\"{target}\",\"Email\":\"{decoy}\"}}"
        };
        for (var i = 0; i < duplicates.Length; i++)
        {
            var response = await PostRawForgot(duplicates[i], $"203.0.113.{40 + i}");
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await response.Content.ReadAsStringAsync()).Should().Contain(RateLimitingMiddleware.DuplicateEmailFieldKey);
        }

        FindMessage(target).Should().BeNull();
        FindMessage(decoy).Should().BeNull();

        var accepted = new[]
        {
            $"{{\"email\":\"{target}\"}}",
            $"{{\"Email\":\"{target.ToUpperInvariant()}\"}}",
            $"{{\"unused\":false,\"email\":\"{target}\"}}"
        };
        for (var i = 0; i < accepted.Length; i++)
        {
            var response = await PostRawForgot(accepted[i], $"198.51.100.{40 + i}");
            response.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        var blocked = await PostRawForgot($"{{\"Email\":\"{target}\"}}", "198.51.100.49");
        blocked.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        (await blocked.Content.ReadAsStringAsync()).Should().Contain("error.tooManyRequests");

        CustomWebApplicationFactory.MockEmailSender.Sent.Count(message =>
            message.To.Mailboxes.Any(box =>
                string.Equals(box.Address, target, StringComparison.OrdinalIgnoreCase)))
            .Should().Be(3);
        FindMessage(decoy).Should().BeNull();
    }

    private async Task<string> RequestTokenAsync(string email)
    {
        var forgot = await PostForgotAsync(email);
        forgot.StatusCode.Should().Be(HttpStatusCode.OK);
        return QueryHelpers.ParseQuery(new Uri(LinkFor(email)).Query)["token"].ToString();
    }

    private static int ForgotPasswordMax() => RateLimitingMiddleware.ForgotPasswordMaxRequests;

    private async Task<HttpResponseMessage> PostRefreshAsync(string refreshToken)
    {
        var bare = Factory.CreateClient();
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/auth/refreshToken");
        message.Headers.TryAddWithoutValidation("Cookie", $"refresh_token={refreshToken}");
        return await bare.SendAsync(message);
    }

    private Task<HttpResponseMessage> PostRawForgot(string json, string forwardedFor)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/auth/forgotPassword")
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        message.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        return Client.SendAsync(message);
    }

    private Task<HttpResponseMessage> PostForgotFromIp(string email, string forwardedFor)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/auth/forgotPassword")
        {
            Content = JsonContent.Create(new { email })
        };
        message.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        return Client.SendAsync(message);
    }

    private async Task<HttpResponseMessage> PostForgotAsync(string email, string? host = null)
    {
        var message = new HttpRequestMessage(HttpMethod.Post, "/api/auth/forgotPassword")
        {
            Content = JsonContent.Create(new
            {
                email,
                frontendUrl = "https://evil.example/phish",
                returnUrl = "https://evil.example/steal"
            })
        };
        if (host != null)
        {
            message.Headers.TryAddWithoutValidation("X-Forwarded-Host", host);
            message.Headers.TryAddWithoutValidation("X-Forwarded-For", "203.0.113.8");
        }

        return await Client.SendAsync(message);
    }

    private Task<HttpResponseMessage> PostResetAsync(string email, string token, string password)
    {
        return Client.PostAsJsonAsync("/api/auth/resetPassword", new
        {
            email,
            token,
            newPassword = password,
            confirmPassword = password
        });
    }

    private Task<HttpResponseMessage> PostLoginAsync(string email, string password, string? code = null)
    {
        return Client.PostAsJsonAsync("/api/auth/login", new
        {
            email,
            password,
            twoFactorCode = code
        });
    }

    private async Task CreateExternalUserAsync(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = new AppUser
        {
            UserName = email,
            Email = email,
            EmailConfirmed = true,
            AuthMethod = "Google"
        };
        var created = await users.CreateAsync(user);
        created.Succeeded.Should().BeTrue(string.Join(", ", created.Errors.Select(error => error.Description)));
    }

    private async Task<string> EnableTwoFactorAsync(string email, string password)
    {
        await CreateTestUserAsync(email, password, email);
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var encryption = scope.ServiceProvider.GetRequiredService<EncryptionService>();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var user = await users.FindByEmailAsync(email);
        var secretBytes = new byte[20];
        RandomNumberGenerator.Fill(secretBytes);
        var secret = OtpNet.Base32Encoding.ToString(secretBytes);
        (await users.SetTwoFactorEnabledAsync(user!, true)).Succeeded.Should().BeTrue();
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

    private static string LinkFor(string email)
    {
        var message = FindMessage(email);
        message.Should().NotBeNull();
        var match = Regex.Match(message!.TextBody ?? "", @"https://localhost:8095/reset-password\?\S+");
        match.Success.Should().BeTrue();
        return match.Value;
    }

    private static MimeKit.MimeMessage? FindMessage(string email)
    {
        return CustomWebApplicationFactory.MockEmailSender.Sent.LastOrDefault(message =>
            message.To.Mailboxes.Any(box => string.Equals(box.Address, email, StringComparison.OrdinalIgnoreCase)));
    }

    private static string? ReadCookie(HttpResponseMessage response, string name)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var cookies))
        {
            return null;
        }

        var prefix = name + "=";
        var cookie = cookies.FirstOrDefault(value => value.StartsWith(prefix, StringComparison.Ordinal));
        if (cookie == null)
        {
            return null;
        }

        var raw = cookie.Split(';', 2)[0][prefix.Length..];
        return Uri.UnescapeDataString(raw);
    }

    private static string CookieHeader(HttpResponseMessage response)
    {
        return response.Headers.TryGetValues("Set-Cookie", out var cookies)
            ? string.Join("\n", cookies)
            : "";
    }

    private static string UniqueEmail() => $"reset-{Guid.NewGuid():N}@example.com";
}

public class ExpiredPasswordResetTests : IClassFixture<ShortPasswordResetFactory>
{
    private readonly ShortPasswordResetFactory _factory;
    private readonly HttpClient _client;

    public ExpiredPasswordResetTests(ShortPasswordResetFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    [Fact]
    public async Task ExpiredIdentityToken_IsRejected()
    {
        _factory.Services.GetRequiredService<RateLimitStore>().Reset();
        var email = $"expired-{Guid.NewGuid():N}@example.com";
        const string password = "OldPassword123!";
        using (var scope = _factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var created = await users.CreateAsync(new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                AuthMethod = "Local"
            }, password);
            created.Succeeded.Should().BeTrue(string.Join(", ", created.Errors.Select(error => error.Description)));
        }

        var forgot = await _client.PostAsJsonAsync("/api/auth/forgotPassword", new { email });
        forgot.StatusCode.Should().Be(HttpStatusCode.OK);
        await Task.Delay(50);

        var message = CustomWebApplicationFactory.MockEmailSender.Sent.Last(sent =>
            sent.To.Mailboxes.Any(box => string.Equals(box.Address, email, StringComparison.OrdinalIgnoreCase)));
        var link = Regex.Match(message.TextBody ?? "", @"https://\S+").Value;
        var token = QueryHelpers.ParseQuery(new Uri(link).Query)["token"].ToString();

        var reset = await _client.PostAsJsonAsync("/api/auth/resetPassword", new
        {
            email,
            token,
            newPassword = "NewPassword123!",
            confirmPassword = "NewPassword123!"
        });

        reset.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await reset.Content.ReadAsStringAsync()).Should().Contain(PasswordResetService.InvalidTokenKey);

        using var check = _factory.Services.CreateScope();
        var still = check.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        (await still.CheckPasswordAsync((await still.FindByEmailAsync(email))!, password)).Should().BeTrue();
    }
}

public class ShortPasswordResetFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureServices(services =>
        {
            services.PostConfigure<PasswordResetTokenProviderOptions>(options =>
            {
                options.TokenLifespan = TimeSpan.FromMilliseconds(1);
            });
        });
    }
}
