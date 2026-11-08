using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Backend.DTOs.Auth;
using Backend.Identity;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

public class PasswordLengthPolicyTests : IntegrationTestBase
{
    private const string Eight = "Ab1!wxyz";
    private const string Seven = "Ab1!xyz";
    private const string LegacySix = "Ab1!xy";

    public PasswordLengthPolicyTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Register_EightAccepted_SevenRejected()
    {
        ResetRateLimit();
        var accepted = await RegisterAsync(UniqueEmail(), Eight);
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);

        var rejected = await RegisterAsync(UniqueEmail(), Seven);
        rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await rejected.Content.ReadAsStringAsync();
        body.Should().Contain("at least 8 characters");
        body.Should().NotContain(Seven);
    }

    [Fact]
    public async Task Reset_EightAccepted_SevenRejected_AndLoginUsesTheNewPassword()
    {
        ResetRateLimit();
        var email = UniqueEmail();
        await CreateTestUserAsync(email, "OldPassword123!");
        var token = await RequestTokenAsync(email);

        var rejected = await ResetAsync(email, token, Seven);
        rejected.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await rejected.Content.ReadAsStringAsync()).Should().Contain("auth.passwordMinLength8");

        var accepted = await ResetAsync(email, token, Eight);
        accepted.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await Client.PostAsJsonAsync("/api/auth/login", new { email, password = Eight });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_ExistingShorterPassword_IsNotForcedOntoTheNewMinimum()
    {
        var email = UniqueEmail();
        using (var scope = Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                AuthMethod = "Local"
            };
            (await users.CreateAsync(user)).Succeeded.Should().BeTrue();
            user.PasswordHash = users.PasswordHasher.HashPassword(user, LegacySix);
            (await users.UpdateAsync(user)).Succeeded.Should().BeTrue();
        }

        var login = await Client.PostAsJsonAsync("/api/auth/login", new { email, password = LegacySix });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private Task<HttpResponseMessage> RegisterAsync(string email, string password)
    {
        return Client.PostAsJsonAsync("/api/auth/registerAccount", new RegisterRequest
        {
            Email = email,
            DisplayName = "Length Policy",
            Password = password,
            ConfirmPassword = password
        });
    }

    private Task<HttpResponseMessage> ResetAsync(string email, string token, string password)
    {
        return Client.PostAsJsonAsync("/api/auth/resetPassword", new
        {
            email,
            token,
            newPassword = password,
            confirmPassword = password
        });
    }

    private async Task<string> RequestTokenAsync(string email)
    {
        var forgot = await Client.PostAsJsonAsync("/api/auth/forgotPassword", new { email });
        forgot.StatusCode.Should().Be(HttpStatusCode.OK);
        var message = CustomWebApplicationFactory.MockEmailSender.Sent.Last(sent =>
            sent.To.Mailboxes.Any(box => string.Equals(box.Address, email, StringComparison.OrdinalIgnoreCase)));
        var link = Regex.Match(message.TextBody ?? "", @"https://\S+").Value;
        return QueryHelpers.ParseQuery(new Uri(link).Query)["token"].ToString();
    }

    private static string UniqueEmail() => $"len-{Guid.NewGuid():N}@example.com";
}
