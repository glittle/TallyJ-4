using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.DTOs.OnlineVoting;

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
        var uniqueEmail = $"voter_{Guid.NewGuid():N}@example.com";
        await SetupOpenElectionWithVoter(uniqueEmail);

        var request = new RequestCodeDto
        {
            VoterId = uniqueEmail,
            VoterIdType = "E",
            DeliveryMethod = "email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);

        var content = await response.Content.ReadAsStringAsync();

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine($"Status: {response.StatusCode}, Content: {content}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("voting.auth.requestCode.", content);
    }

    [Fact]
    public async Task RequestCode_WithVoterNotInAnyElection_ShouldFail()
    {
        // Arrange
        await SetupOpenElection(); // Election exists but voter not registered
        var uniqueEmail = $"notregistered_{Guid.NewGuid():N}@example.com";

        var request = new RequestCodeDto
        {
            VoterId = uniqueEmail,
            VoterIdType = "E",
            DeliveryMethod = "email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);
        var content = await response.Content.ReadAsStringAsync();

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine($"Status: {response.StatusCode}, Content: {content}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("voting.auth.requestCode.notRegistered", content);
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
        var content = await response.Content.ReadAsStringAsync();

        // Assert - voter is not in any open election (other open elections may exist from prior tests)
        if (response.StatusCode != HttpStatusCode.OK)
        {
            Console.WriteLine($"Status: {response.StatusCode}, Content: {content}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(content.Contains("voting.auth.requestCode.noOpenElections") || content.Contains("voting.auth.requestCode.notRegistered"),
            $"Expected message about no open elections or voter not registered, got: {content}");
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
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected OK but got {response.StatusCode}. Response content: {content}");
        }

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
        if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected BadRequest but got {response.StatusCode}. Response content: {content}");
        }

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
        if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected BadRequest but got {response.StatusCode}. Response content: {content}");
        }

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region Available Elections Tests

    [Fact]
    public async Task GetAvailableElections_WithValidVoter_ReturnsElections()
    {
        // Arrange
        var email = $"voter_{Guid.NewGuid():N}@example.com";
        var electionGuid1 = await SetupOpenElectionWithVoter(email, electionName: "Election 1");
        var electionGuid2 = await SetupOpenElectionWithVoter(email, electionName: "Election 2");

        // Act
        var response = await Client.GetAsync($"/api/online-voting/availableElections?voterId={email}");

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected OK but got {response.StatusCode}. Response content: {content}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var elections = await response.Content.ReadFromJsonAsync<List<AvailableElectionDto>>();
        Assert.NotNull(elections);
        Assert.Equal(2, elections.Count);

        // Verify election 1
        var election1 = elections.FirstOrDefault(e => e.ElectionGuid == electionGuid1);
        Assert.NotNull(election1);
        Assert.Equal("Election 1", election1.Name);
        Assert.NotNull(election1.OnlineWhenOpen);
        Assert.NotNull(election1.OnlineWhenClose);
        Assert.False(election1.HasVoted);

        // Verify election 2
        var election2 = elections.FirstOrDefault(e => e.ElectionGuid == electionGuid2);
        Assert.NotNull(election2);
        Assert.Equal("Election 2", election2.Name);
        Assert.NotNull(election2.OnlineWhenOpen);
        Assert.NotNull(election2.OnlineWhenClose);
        Assert.False(election2.HasVoted);
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
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected OK but got {response.StatusCode}. Response content: {content}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var elections = await response.Content.ReadFromJsonAsync<List<AvailableElectionDto>>();
        Assert.NotNull(elections);
        Assert.Empty(elections);
    }

    [Fact]
    public async Task GetAvailableElections_OnlyReturnsOpenElections()
    {
        // Arrange
        var email = $"voter_{Guid.NewGuid():N}@example.com";
        var openElectionGuid = await SetupOpenElectionWithVoter(email, electionName: "Open Election");
        await SetupClosedElectionWithVoter(email, electionName: "Closed Election");

        // Act
        var response = await Client.GetAsync($"/api/online-voting/availableElections?voterId={email}");

        // Assert
        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected OK but got {response.StatusCode}. Response content: {content}");
        }

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
        if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected BadRequest but got {response.StatusCode}. Response content: {content}");
        }

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
