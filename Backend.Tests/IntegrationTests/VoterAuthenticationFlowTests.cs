using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.DTOs.OnlineVoting;
using Xunit;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Integration tests for revised voter authentication flow (no election GUID required upfront).
/// </summary>
public class VoterAuthenticationFlowTests : IntegrationTestBase
{
    public VoterAuthenticationFlowTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    #region Request Code Tests

    [Fact]
    public async Task RequestCode_WithValidVoterInOpenElection_ShouldSucceed()
    {
        // Arrange
        await SetupOpenElectionWithVoter("test@example.com");

        var request = new RequestCodeDto
        {
            VoterId = "test@example.com",
            VoterIdType = "E",
            DeliveryMethod = "email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Verification code sent successfully", content);
    }

    [Fact]
    public async Task RequestCode_WithVoterNotInAnyElection_ShouldFail()
    {
        // Arrange
        await SetupOpenElection(); // Election exists but voter not registered

        var request = new RequestCodeDto
        {
            VoterId = "notregistered@example.com",
            VoterIdType = "E",
            DeliveryMethod = "email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not registered to vote in any currently open election", content);
    }

    [Fact]
    public async Task RequestCode_WithNoOpenElections_ShouldFail()
    {
        // Arrange
        await SetupClosedElectionWithVoter("test@example.com");

        var request = new RequestCodeDto
        {
            VoterId = "test@example.com",
            VoterIdType = "E",
            DeliveryMethod = "email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("no elections currently open", content);
    }

    [Fact]
    public async Task RequestCode_WithPhoneNumber_ValidVoter_ShouldSucceed()
    {
        // Arrange
        var phoneNumber = "+15551234567";
        await SetupOpenElectionWithVoter(null, phoneNumber);

        var request = new RequestCodeDto
        {
            VoterId = phoneNumber,
            VoterIdType = "P",
            DeliveryMethod = "sms"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    #endregion

    #region Google OAuth Tests

    [Fact]
    public async Task GoogleAuth_WithoutGoogleClientId_ShouldReturnError()
    {
        // Arrange
        await SetupOpenElectionWithVoter("test@example.com");

        var request = new GoogleAuthForVoterDto
        {
            Credential = "fake-google-token"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/googleAuth", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Will fail because Google Client ID is not configured in test environment
    }

    [Fact]
    public async Task GoogleAuth_EndpointExists()
    {
        // Arrange
        var request = new GoogleAuthForVoterDto
        {
            Credential = "" // Empty credential
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/googleAuth", request);

        // Assert
        // Should return BadRequest due to validation or Google auth failure
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Available Elections Tests

    [Fact]
    public async Task GetAvailableElections_WithValidVoter_ReturnsElections()
    {
        // Arrange
        var email = "voter@example.com";
        var electionGuid1 = await SetupOpenElectionWithVoter(email, electionName: "Election 1");
        var electionGuid2 = await SetupOpenElectionWithVoter(email, electionName: "Election 2");

        // Act
        var response = await Client.GetAsync($"/api/online-voting/availableElections?voterId={email}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var elections = await response.Content.ReadFromJsonAsync<List<AvailableElectionDto>>();
        Assert.NotNull(elections);
        Assert.Equal(2, elections.Count);
        Assert.Contains(elections, e => e.ElectionGuid == electionGuid1);
        Assert.Contains(elections, e => e.ElectionGuid == electionGuid2);
    }

    [Fact]
    public async Task GetAvailableElections_WithVoterNotInAnyElection_ReturnsEmptyList()
    {
        // Arrange
        await SetupOpenElection(); // Election exists but voter not registered
        var email = "notregistered@example.com";

        // Act
        var response = await Client.GetAsync($"/api/online-voting/availableElections?voterId={email}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var elections = await response.Content.ReadFromJsonAsync<List<AvailableElectionDto>>();
        Assert.NotNull(elections);
        Assert.Empty(elections);
    }

    [Fact]
    public async Task GetAvailableElections_OnlyReturnsOpenElections()
    {
        // Arrange
        var email = "voter@example.com";
        var openElectionGuid = await SetupOpenElectionWithVoter(email, electionName: "Open Election");
        await SetupClosedElectionWithVoter(email, electionName: "Closed Election");

        // Act
        var response = await Client.GetAsync($"/api/online-voting/availableElections?voterId={email}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var elections = await response.Content.ReadFromJsonAsync<List<AvailableElectionDto>>();
        Assert.NotNull(elections);
        Assert.Single(elections);
        Assert.Equal(openElectionGuid, elections[0].ElectionGuid);
        Assert.Equal("Open Election", elections[0].Name);
    }

    [Fact]
    public async Task GetAvailableElections_WithoutVoterId_ShouldFail()
    {
        // Act
        var response = await Client.GetAsync("/api/online-voting/availableElections");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    // Helper methods

    private async Task<Guid> SetupOpenElection()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var electionGuid = Guid.NewGuid();

        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = "Test Open Election",
            OnlineWhenOpen = DateTime.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTime.UtcNow.AddHours(1),
            TallyStatus = "Open",
            RowVersion = new byte[8]
        };

        context.Elections.Add(election);
        await context.SaveChangesAsync();

        return electionGuid;
    }

    private async Task<Guid> SetupOpenElectionWithVoter(
        string? email = null,
        string? phone = null,
        string? kioskCode = null,
        string? electionName = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var electionGuid = Guid.NewGuid();

        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = electionName ?? "Test Open Election with Voter",
            OnlineWhenOpen = DateTime.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTime.UtcNow.AddHours(1),
            TallyStatus = "Open",
            RowVersion = new byte[8]
        };

        context.Elections.Add(election);

        var person = new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            LastName = "Test",
            FirstName = "Voter",
            Email = email,
            Phone = phone,
            KioskCode = kioskCode,
            CanVote = true,
            RowVersion = new byte[8]
        };

        context.People.Add(person);
        await context.SaveChangesAsync();

        return electionGuid;
    }

    private async Task<Guid> SetupClosedElectionWithVoter(
        string? email = null,
        string? phone = null,
        string? electionName = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var electionGuid = Guid.NewGuid();

        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = electionName ?? "Test Closed Election",
            OnlineWhenOpen = DateTime.UtcNow.AddHours(-3),
            OnlineWhenClose = DateTime.UtcNow.AddHours(-1),
            TallyStatus = "Closed",
            RowVersion = new byte[8]
        };

        context.Elections.Add(election);

        var person = new Person
        {
            ElectionGuid = electionGuid,
            PersonGuid = Guid.NewGuid(),
            LastName = "Test",
            FirstName = "Voter",
            Email = email,
            Phone = phone,
            CanVote = true,
            RowVersion = new byte[8]
        };

        context.People.Add(person);
        await context.SaveChangesAsync();

        return electionGuid;
    }
}
