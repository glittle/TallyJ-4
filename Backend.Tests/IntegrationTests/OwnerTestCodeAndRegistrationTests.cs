using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Backend.Controllers;
using Backend.DTOs.Auth;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Identity;
using Backend.Services;
using Backend.Services.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// The owner test code is a real HTTP send, and a failed restriction row does not leave a user.
/// </summary>
public class OwnerTestCodeAndRegistrationTests : IntegrationTestBase
{
    public OwnerTestCodeAndRegistrationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RestrictedOwner_CanEmailThemselvesATestCode_WithoutOpeningOnlineVoting()
    {
        var email = $"test-code-{Guid.NewGuid()}@tallyj.test";
        var password = "TestPass123!";
        var register = await PostJsonAsync("/api/auth/registerAccount", new RegisterRequest
        {
            Email = email,
            Password = password,
            ConfirmPassword = password,
            DisplayName = "Restricted Owner"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        Guid userId;
        Guid electionId;
        using (var scope = Factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByEmailAsync(email);
            user.Should().NotBeNull();
            userId = Guid.Parse(user!.Id);
            var token = await users.GenerateEmailConfirmationTokenAsync(user);
            (await users.ConfirmEmailAsync(user, token)).Succeeded.Should().BeTrue();

            var db = scope.ServiceProvider.GetRequiredService<Backend.Context.MainDbContext>();
            electionId = Guid.NewGuid();
            db.Elections.Add(new Election
            {
                ElectionGuid = electionId,
                Name = "Still closed",
                UseOnlineVoting = false,
                ElectionStage = ElectionStage.SettingUp,
                RowVersion = new byte[8]
            });
            db.JoinElectionUsers.Add(new JoinElectionUser
            {
                ElectionGuid = electionId,
                UserId = userId,
                Role = "Admin"
            });
            db.People.Add(new Person
            {
                PersonGuid = Guid.NewGuid(),
                ElectionGuid = electionId,
                FirstName = "Ada",
                LastName = "Voter",
                Email = $"voter-{Guid.NewGuid()}@example.com",
                CanVote = true,
                RowVersion = new byte[8]
            });
            await db.SaveChangesAsync();
        }

        var login = await PostJsonAsync("/api/auth/login", new LoginRequest
        {
            Email = email,
            Password = password
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var auth = GetSetCookieValue(login, "auth_token");
        auth.Should().NotBeNullOrWhiteSpace();
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth);

        var before = CustomWebApplicationFactory.MockEmailSender.Sent.Count;
        var sent = await Client.PostAsync("/api/account/owner-test-code", new StringContent(""));
        sent.StatusCode.Should().Be(HttpStatusCode.OK);

        var messages = CustomWebApplicationFactory.MockEmailSender.Sent
            .Skip(before)
            .Where(message => message.To.Mailboxes.Any(box =>
                string.Equals(box.Address, email, StringComparison.OrdinalIgnoreCase)))
            .ToList();
        messages.Should().ContainSingle();
        messages[0].Subject.Should().Be(OwnerTestCodeService.Subject);
        messages[0].TextBody.Should().Contain(email);
        messages[0].TextBody.Should().Contain("does not open online voting");

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Backend.Context.MainDbContext>();
            var election = await db.Elections.SingleAsync(item => item.ElectionGuid == electionId);
            election.UseOnlineVoting.Should().BeFalse();
        }

        Client.DefaultRequestHeaders.Authorization = null;
        var requestCode = await PostJsonAsync("/api/online-voting/requestCode", new
        {
            voterId = email,
            voterIdType = "E",
            deliveryMethod = "email"
        });
        requestCode.StatusCode.Should().Be(HttpStatusCode.OK);
        CustomWebApplicationFactory.MockEmailSender.Sent
            .Skip(before)
            .Count(message => message.Subject == "Your TallyJ Voting Code"
                && message.To.Mailboxes.Any(box =>
                    string.Equals(box.Address, email, StringComparison.OrdinalIgnoreCase)))
            .Should().Be(0);
    }

    [Fact]
    public async Task Register_WhenRestrictionRowFails_ReturnsGenericError_AndCreatesNoUser()
    {
        var email = $"restrict-fail-{Guid.NewGuid()}@tallyj.test";
        OwnerAccountGate.FailRestrictionSaveForEmail = email;
        try
        {
            var response = await PostJsonAsync("/api/auth/registerAccount", new RegisterRequest
            {
                Email = email,
                Password = "TestPass123!",
                ConfirmPassword = "TestPass123!",
                DisplayName = "Should Roll Back"
            });

            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var body = await response.Content.ReadAsStringAsync();
            body.Should().Contain(LocalAuthService.RegisterFailedKey);
            body.Should().NotContain("UNIQUE");
            body.Should().NotContain("constraint");

            using var scope = Factory.Services.CreateScope();
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            (await users.FindByEmailAsync(email)).Should().BeNull();
        }
        finally
        {
            OwnerAccountGate.FailRestrictionSaveForEmail = null;
        }
    }
}
