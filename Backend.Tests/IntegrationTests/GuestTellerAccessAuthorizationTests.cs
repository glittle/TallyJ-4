using System.Net;
using Backend.Context;
using Backend.DTOs.Elections;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Identity;
using Backend.Models;
using Backend.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Guest teller access follows officer membership, not election stage or
/// the online-voting flag. The shared passcode is not an authorization check.
/// </summary>
public class GuestTellerAccessAuthorizationTests : IntegrationTestBase
{
    public GuestTellerAccessAuthorizationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GatheringBallots_WithOnlineVoting_AllowsOwnerAndAdmin_AndDeniesEveryoneElse()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var adminEmail = $"guest-access-admin-{suffix}@tallyj.test";
        var ownerEmail = $"guest-access-owner-{suffix}@tallyj.test";
        var tellerEmail = $"guest-access-teller-{suffix}@tallyj.test";
        var strangerEmail = $"guest-access-stranger-{suffix}@tallyj.test";
        const string password = "TestPass123!";

        var adminToken = await GetAuthTokenAsync(adminEmail, password);
        SetAuthToken(adminToken);
        var electionGuid = await CreateElectionAsync($"Guest access {suffix}");
        await ConfigureGatheringOnlineElectionAsync(electionGuid);

        var open = await ToggleAsync(electionGuid, true);
        Assert.Equal(HttpStatusCode.OK, open.StatusCode);
        var opened = await ReadElectionAsync(electionGuid);
        Assert.NotNull(opened.ListedForPublicAsOf);
        Assert.Equal(ElectionStage.GatheringBallots, opened.ElectionStage);
        Assert.True(opened.UseOnlineVoting);
        Assert.Equal("gather-1", opened.ElectionPasscode);

        await AddJoinAsync(electionGuid, await UserIdAsync(ownerEmail, password), "Owner");
        SetAuthToken(await GetAuthTokenAsync(ownerEmail, password));
        var close = await ToggleAsync(electionGuid, false);
        Assert.Equal(HttpStatusCode.OK, close.StatusCode);
        var closed = await ReadElectionAsync(electionGuid);
        Assert.Null(closed.ListedForPublicAsOf);
        Assert.Equal(ElectionStage.GatheringBallots, closed.ElectionStage);
        Assert.True(closed.UseOnlineVoting);

        SetAuthToken(adminToken);
        Assert.Equal(HttpStatusCode.OK, (await ToggleAsync(electionGuid, true)).StatusCode);
        var listedAt = (await ReadElectionAsync(electionGuid)).ListedForPublicAsOf;
        Assert.NotNull(listedAt);

        await AddJoinAsync(electionGuid, await UserIdAsync(tellerEmail, password), "Teller");
        SetAuthToken(await GetAuthTokenAsync(tellerEmail, password));
        Assert.Equal(HttpStatusCode.Forbidden, (await ToggleAsync(electionGuid, false)).StatusCode);

        SetAuthToken(await GetAuthTokenAsync(strangerEmail, password));
        Assert.Equal(HttpStatusCode.Forbidden, (await ToggleAsync(electionGuid, false)).StatusCode);

        using (var scope = Factory.Services.CreateScope())
        {
            var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
            SetAuthToken(tokens.GenerateTellerToken(electionGuid));
        }

        Assert.Equal(HttpStatusCode.Forbidden, (await ToggleAsync(electionGuid, false)).StatusCode);

        var stillOpen = await ReadElectionAsync(electionGuid);
        Assert.Equal(listedAt, stillOpen.ListedForPublicAsOf);
        Assert.Equal(ElectionStage.GatheringBallots, stillOpen.ElectionStage);
        Assert.True(stillOpen.UseOnlineVoting);
        Assert.Equal("gather-1", stillOpen.ElectionPasscode);
    }

    private async Task<Guid> CreateElectionAsync(string name)
    {
        var response = await PostJsonAsync("/api/elections/createElection", new CreateElectionDto
        {
            Name = name,
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3,
            ShowAsTest = true
        });
        response.EnsureSuccessStatusCode();
        var created = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(response);
        return created!.Data!.ElectionGuid;
    }

    private Task<HttpResponseMessage> ToggleAsync(Guid electionGuid, bool isOpen)
    {
        return PutJsonAsync(
            $"/api/Elections/{electionGuid}/teller-access",
            new ToggleTellerAccessDto { IsOpen = isOpen });
    }

    private async Task ConfigureGatheringOnlineElectionAsync(Guid electionGuid)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var election = await db.Elections.SingleAsync(e => e.ElectionGuid == electionGuid);
        election.ElectionStage = ElectionStage.GatheringBallots;
        election.UseOnlineVoting = true;
        election.ElectionPasscode = "gather-1";
        election.ListedForPublicAsOf = null;
        await db.SaveChangesAsync();
    }

    private async Task<Guid> UserIdAsync(string email, string password)
    {
        await CreateTestUserAsync(email, password);
        using var scope = Factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await users.FindByEmailAsync(email);
        Assert.NotNull(user);
        return Guid.Parse(user!.Id);
    }

    private async Task AddJoinAsync(Guid electionGuid, Guid userId, string role)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        db.JoinElectionUsers.Add(new JoinElectionUser
        {
            ElectionGuid = electionGuid,
            UserId = userId,
            Role = role
        });
        await db.SaveChangesAsync();
    }

    private async Task<Election> ReadElectionAsync(Guid electionGuid)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        return await db.Elections.AsNoTracking().SingleAsync(e => e.ElectionGuid == electionGuid);
    }
}
