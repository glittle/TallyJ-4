using System.Net;
using System.Net.Http.Json;
using Backend.Context;
using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Middleware;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Cookie transport for online-voter JWT (issue #250): set on auth, used without Bearer,
/// accepted on voter hubs, cleared on logout, distinct from teller auth_token.
/// </summary>
public class VoterCookieAuthTests : IntegrationTestBase
{
    public VoterCookieAuthTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task VerifyCode_SetsHttpOnlyVoterCookie_AndOmitsTokenFromBody()
    {
        var email = $"cookie_otp_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);

        var requestResponse = await Client.PostAsJsonAsync("/api/online-voting/requestCode", new RequestCodeDto
        {
            VoterId = email,
            VoterIdType = "E",
            DeliveryMethod = "email"
        });
        var requestBody = await requestResponse.Content.ReadFromJsonAsync<RequestCodeResponseDto>();
        Assert.False(string.IsNullOrWhiteSpace(requestBody?.DevVerificationCode));

        var verifyResponse = await Client.PostAsJsonAsync("/api/online-voting/verifyCode", new VerifyCodeDto
        {
            VoterId = email,
            VerifyCode = requestBody!.DevVerificationCode!
        });

        Assert.Equal(HttpStatusCode.OK, verifyResponse.StatusCode);
        var auth = await verifyResponse.Content.ReadFromJsonAsync<OnlineVoterAuthResponse>();
        Assert.NotNull(auth);
        Assert.Equal(email, auth.VoterId);
        Assert.True(string.IsNullOrEmpty(auth.Token));

        var tokenCookie = GetSetCookieHeader(verifyResponse, SecureCookieMiddleware.VoterTokenCookieName);
        Assert.False(string.IsNullOrWhiteSpace(tokenCookie));
        Assert.Contains("httponly", tokenCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", tokenCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", tokenCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", tokenCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(SecureCookieMiddleware.AccessTokenCookieName + "=", tokenCookie, StringComparison.OrdinalIgnoreCase);

        var sessionCookie = GetSetCookieHeader(verifyResponse, SecureCookieMiddleware.VoterSessionCookieName);
        Assert.False(string.IsNullOrWhiteSpace(sessionCookie));
        Assert.DoesNotContain("httponly", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", sessionCookie, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", sessionCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AvailableElections_WithVoterCookieOnly_Succeeds()
    {
        var email = $"cookie_elections_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email, electionName: "Cookie Election");
        var token = await AuthenticateVoterAndGetCookieAsync(email);

        SetVoterCookie(token);
        var response = await Client.GetAsync("/api/online-voting/availableElections");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var elections = await response.Content.ReadFromJsonAsync<List<AvailableElectionDto>>();
        Assert.NotNull(elections);
        Assert.Contains(elections, e => e.Name == "Cookie Election");
    }

    [Fact]
    public async Task Me_WithVoterCookieOnly_ReturnsVoterIdentity()
    {
        var email = $"cookie_me_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);
        var token = await AuthenticateVoterAndGetCookieAsync(email);

        SetVoterCookie(token);
        var response = await Client.GetAsync("/api/online-voting/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var session = await response.Content.ReadFromJsonAsync<OnlineVoterSessionDto>();
        Assert.NotNull(session);
        Assert.Equal(email, session.VoterId);
        Assert.Equal("E", session.VoterIdType);
    }

    [Fact]
    public async Task Logout_ClearsVoterCookies_ThenAvailableElectionsUnauthorized()
    {
        var email = $"cookie_logout_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);
        var token = await AuthenticateVoterAndGetCookieAsync(email);

        SetVoterCookie(token);
        var logout = await Client.PostAsync("/api/online-voting/logout", null);
        Assert.Equal(HttpStatusCode.OK, logout.StatusCode);

        var clearedToken = GetSetCookieHeader(logout, SecureCookieMiddleware.VoterTokenCookieName);
        Assert.False(string.IsNullOrWhiteSpace(clearedToken));
        Assert.Contains("max-age=0", clearedToken, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", clearedToken, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", clearedToken, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("domain=", clearedToken, StringComparison.OrdinalIgnoreCase);

        var tellerCookie = GetSetCookieHeader(logout, SecureCookieMiddleware.AccessTokenCookieName);
        Assert.Null(tellerCookie);

        Client.DefaultRequestHeaders.Remove("Cookie");
        var noCookie = await Client.GetAsync("/api/online-voting/availableElections");
        Assert.Equal(HttpStatusCode.Unauthorized, noCookie.StatusCode);
    }

    [Fact]
    public async Task VoterAndTellerCookies_DoNotMixUpByPath()
    {
        var email = $"cookie_both_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);
        var voterToken = await AuthenticateVoterAndGetCookieAsync(email);
        var tellerToken = await GetAuthTokenAsync();

        SetCookies(
            (SecureCookieMiddleware.VoterTokenCookieName, voterToken),
            (SecureCookieMiddleware.AccessTokenCookieName, tellerToken));

        var voterResponse = await Client.GetAsync("/api/online-voting/availableElections");
        Assert.Equal(HttpStatusCode.OK, voterResponse.StatusCode);

        var tellerResponse = await Client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.OK, tellerResponse.StatusCode);
    }

    [Fact]
    public async Task AllVotersAndVoterPersonal_JoinWithCookieOnly()
    {
        var email = $"cookie_hubs_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(email);
        var token = await AuthenticateVoterAndGetCookieAsync(email);
        var cookieHeader = $"{SecureCookieMiddleware.VoterTokenCookieName}={token}";

        await using var allVoters = CreateVoterHubConnection("/hubs/all-voters", cookieHeader);
        await allVoters.StartAsync();
        await allVoters.InvokeAsync("Join");

        await using var personal = CreateVoterHubConnection("/hubs/voter-personal", cookieHeader);
        await personal.StartAsync();
        await personal.InvokeAsync("Join");
    }

    private HubConnection CreateVoterHubConnection(string hubPath, string cookieHeader)
    {
        var handler = Factory.Server.CreateHandler();
        return new HubConnectionBuilder()
            .WithUrl(new Uri(Client.BaseAddress!, hubPath), options =>
            {
                options.HttpMessageHandlerFactory = _ => handler;
                options.Headers.Add("Cookie", cookieHeader);
            })
            .Build();
    }

    private async Task<string> AuthenticateVoterAndGetCookieAsync(string email)
    {
        var response = await Client.PostAsJsonAsync("/api/online-voting/googleAuth", new GoogleAuthForVoterDto
        {
            Credential = $"dev-google:{email}"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var token = GetSetCookieValue(response, SecureCookieMiddleware.VoterTokenCookieName);
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token!;
    }

    private async Task SetupOpenElectionWithVoter(string email, string? electionName = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var electionGuid = Guid.NewGuid();
        context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = electionName ?? "Cookie Auth Election",
            UseOnlineVoting = true,
            OnlineWhenOpen = DateTime.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTime.UtcNow.AddHours(1),
            ElectionStage = ElectionStage.GatheringBallots,
            RowVersion = new byte[8]
        });

        context.People.Add(new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            FirstName = "Cookie",
            LastName = "Voter",
            Email = email,
            CanVote = true,
            RowVersion = new byte[8]
        });

        await context.SaveChangesAsync();
    }
}
