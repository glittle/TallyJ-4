using System.Net;
using System.Text;
using System.Text.Json;
using Backend.Context;
using Backend.Controllers;
using Backend.DTOs.Auth;
using Backend.Identity;
using Backend.Services.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Invite-only local signup after open register was disabled (#347 leftover).
/// </summary>
public class AccountInviteTests : IntegrationTestBase
{
    public AccountInviteTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RegisterAccount_OpenSelfServe_StillRejected_WhenInvitePathExists()
    {
        var testEmail = $"open-register-{Guid.NewGuid()}@tallyj.test";
        var response = await PostJsonAsync("/api/auth/registerAccount", new RegisterRequest
        {
            Email = testEmail,
            Password = "TestPass123!",
            ConfirmPassword = "TestPass123!",
            DisplayName = "Should Not Be Created"
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain(AuthController.OpenRegisterDisabledKey);

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        (await userManager.FindByEmailAsync(testEmail)).Should().BeNull();
    }

    [Fact]
    public async Task SuperAdmin_CanIssueInvite_AndAnonymousCanCreateOneLocalAccount()
    {
        var created = await IssueInviteAsSuperAdminAsync();
        created.Token.Should().NotBeNullOrWhiteSpace();
        created.InviteUrl.Should().Contain("/register");
        created.InviteUrl.Should().Contain("invite=");

        var peek = await Client.GetAsync($"/api/auth/account-invite?token={Uri.EscapeDataString(created.Token)}");
        peek.StatusCode.Should().Be(HttpStatusCode.OK);
        var peekDto = JsonSerializer.Deserialize<AccountInviteStatusDto>(
            await peek.Content.ReadAsStringAsync(), JsonOptions);
        peekDto!.Valid.Should().BeTrue();

        var email = $"invite-create-{Guid.NewGuid()}@tallyj.test";
        var register = await PostJsonAsync("/api/auth/registerWithInvite", new RegisterWithInviteRequest
        {
            Token = created.Token,
            Email = email,
            Password = "TestPass123!",
            ConfirmPassword = "TestPass123!",
            DisplayName = "Invited User"
        });

        register.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = JsonSerializer.Deserialize<AuthResponse>(
            await register.Content.ReadAsStringAsync(), JsonOptions);
        auth!.Email.Should().Be(email);
        auth.AuthMethod.Should().Be("Local");
        auth.RequiresEmailVerification.Should().BeTrue();

        using (var scope = Factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await userManager.FindByEmailAsync(email);
            user.Should().NotBeNull();
            user!.AuthMethod.Should().Be("Local");
            user.EmailConfirmed.Should().BeFalse();
        }

        await ConfirmEmailAsync(email);

        var login = await PostJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = "TestPass123!"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RegisterWithInvite_Reuse_Fails_AndDoesNotCreateSecondUser()
    {
        var created = await IssueInviteAsSuperAdminAsync();
        var firstEmail = $"invite-first-{Guid.NewGuid()}@tallyj.test";
        var first = await PostJsonAsync("/api/auth/registerWithInvite", new RegisterWithInviteRequest
        {
            Token = created.Token,
            Email = firstEmail,
            Password = "TestPass123!",
            ConfirmPassword = "TestPass123!",
            DisplayName = "First"
        });
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var secondEmail = $"invite-second-{Guid.NewGuid()}@tallyj.test";
        var reuse = await PostJsonAsync("/api/auth/registerWithInvite", new RegisterWithInviteRequest
        {
            Token = created.Token,
            Email = secondEmail,
            Password = "TestPass123!",
            ConfirmPassword = "TestPass123!",
            DisplayName = "Second"
        });

        reuse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await reuse.Content.ReadAsStringAsync()).Should().Contain(AccountInviteService.InvalidInviteKey);

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        (await userManager.FindByEmailAsync(secondEmail)).Should().BeNull();
        (await userManager.FindByEmailAsync(firstEmail)).Should().NotBeNull();
    }

    [Fact]
    public async Task RegisterWithInvite_Expired_Fails()
    {
        var created = await IssueInviteAsSuperAdminAsync();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var hash = AccountInviteService.HashToken(created.Token);
            var invite = db.AccountInvites.Single(i => i.TokenHash == hash);
            invite.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
            await db.SaveChangesAsync();
        }

        var peek = await Client.GetAsync($"/api/auth/account-invite?token={Uri.EscapeDataString(created.Token)}");
        var peekDto = JsonSerializer.Deserialize<AccountInviteStatusDto>(
            await peek.Content.ReadAsStringAsync(), JsonOptions);
        peekDto!.Valid.Should().BeFalse();

        var email = $"invite-expired-{Guid.NewGuid()}@tallyj.test";
        var register = await PostJsonAsync("/api/auth/registerWithInvite", new RegisterWithInviteRequest
        {
            Token = created.Token,
            Email = email,
            Password = "TestPass123!",
            ConfirmPassword = "TestPass123!",
            DisplayName = "Expired"
        });

        register.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await register.Content.ReadAsStringAsync()).Should().Contain(AccountInviteService.InvalidInviteKey);

        using var users = Factory.Services.CreateScope();
        var userManager = users.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        (await userManager.FindByEmailAsync(email)).Should().BeNull();
    }

    [Fact]
    public async Task GoogleOneTap_CreatePath_Unchanged_AfterInviteSlice()
    {
        var testEmail = $"google-after-invite-{Guid.NewGuid()}@tallyj.test";
        var response = await PostJsonAsync("/api/auth/google/one-tap", new GoogleOneTapRequest
        {
            Credential = $"dev-google:{testEmail}"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = JsonSerializer.Deserialize<AuthResponse>(
            await response.Content.ReadAsStringAsync(), JsonOptions);
        auth!.Email.Should().Be(testEmail);
        auth.AuthMethod.Should().Be("Google");

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByEmailAsync(testEmail);
        user.Should().NotBeNull();
        user!.AuthMethod.Should().Be("Google");
        user.GoogleId.Should().Be($"dev-google:{testEmail}");
    }

    [Fact]
    public async Task CreateAccountInvite_ReturnsUnauthorized_WhenAnonymous()
    {
        var response = await Client.PostAsync("/api/superadmin/account-invites", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private async Task<AccountInviteCreatedDto> IssueInviteAsSuperAdminAsync()
    {
        var login = await PostJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = "admin@tallyj.test",
            Password = "TestPass123!"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        var request = new HttpRequestMessage(HttpMethod.Post, "/api/superadmin/account-invites");
        foreach (var cookie in GetCookiesFromResponse(login))
        {
            request.Headers.Add("Cookie", $"{cookie.Key}={ExtractCookieValue(cookie.Value)}");
        }

        var response = await Client.SendAsync(request);
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var data = doc.RootElement.GetProperty("data");
        return new AccountInviteCreatedDto
        {
            Token = data.GetProperty("token").GetString()!,
            InviteUrl = data.GetProperty("inviteUrl").GetString()!,
            ExpiresAt = data.GetProperty("expiresAt").GetDateTimeOffset()
        };
    }

    private async Task<HttpResponseMessage> PostJsonAsync(string url, object body)
    {
        var content = new StringContent(
            JsonSerializer.Serialize(body),
            Encoding.UTF8,
            "application/json");
        return await Client.PostAsync(url, content);
    }

    private static Dictionary<string, string> GetCookiesFromResponse(HttpResponseMessage response)
    {
        var cookies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var cookie in setCookies)
            {
                var nameValue = cookie.Split(';', 2)[0].Split('=', 2);
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
