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
/// Integration tests for online voting security features, particularly SMS pumping prevention.
/// </summary>
public class OnlineVotingSecurityTests : IntegrationTestBase
{
    public OnlineVotingSecurityTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task RequestCode_WithValidVoterInOpenElection_ShouldSucceed()
    {
        // Arrange
        var (electionGuid, voterId) = await SetupOpenElectionWithVoter("test@example.com");

        var request = new RequestCodeDto
        {
            ElectionGuid = electionGuid,
            VoterId = voterId,
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
    public async Task RequestCode_WithVoterNotInElection_ShouldFail()
    {
        // Arrange
        var electionGuid = await SetupOpenElection();

        var request = new RequestCodeDto
        {
            ElectionGuid = electionGuid,
            VoterId = "notinelection@example.com",
            VoterIdType = "E",
            DeliveryMethod = "email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not registered to vote in this election", content);
    }

    [Fact]
    public async Task RequestCode_WithClosedElection_ShouldFail()
    {
        // Arrange
        var (electionGuid, voterId) = await SetupClosedElectionWithVoter("test@example.com");

        var request = new RequestCodeDto
        {
            ElectionGuid = electionGuid,
            VoterId = voterId,
            VoterIdType = "E",
            DeliveryMethod = "email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not currently open", content);
    }

    [Fact]
    public async Task RequestCode_WithPhoneNumber_ValidVoterInElection_ShouldSucceed()
    {
        // Arrange
        var phoneNumber = "+15551234567";
        var (electionGuid, _) = await SetupOpenElectionWithVoter(null, phoneNumber);

        var request = new RequestCodeDto
        {
            ElectionGuid = electionGuid,
            VoterId = phoneNumber,
            VoterIdType = "P",
            DeliveryMethod = "sms"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RequestCode_WithPhoneNumber_NotInElection_ShouldFail()
    {
        // Arrange
        var electionGuid = await SetupOpenElection();

        var request = new RequestCodeDto
        {
            ElectionGuid = electionGuid,
            VoterId = "+15559999999",
            VoterIdType = "P",
            DeliveryMethod = "sms"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("not registered to vote in this election", content);
    }

    [Fact]
    public async Task RequestCode_WithKioskCode_ValidVoterInElection_ShouldSucceed()
    {
        // Arrange
        var kioskCode = "KIOSK123";
        var (electionGuid, _) = await SetupOpenElectionWithVoter(null, null, kioskCode);

        var request = new RequestCodeDto
        {
            ElectionGuid = electionGuid,
            VoterId = kioskCode,
            VoterIdType = "C",
            DeliveryMethod = "email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RequestCode_WithNonExistentElection_ShouldFail()
    {
        // Arrange
        var request = new RequestCodeDto
        {
            ElectionGuid = Guid.NewGuid(),
            VoterId = "test@example.com",
            VoterIdType = "E",
            DeliveryMethod = "email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Election not found", content);
    }

    [Fact]
    public async Task RequestCode_WithoutElectionGuid_ShouldFail()
    {
        // Arrange
        var request = new RequestCodeDto
        {
            ElectionGuid = Guid.Empty,
            VoterId = "test@example.com",
            VoterIdType = "E",
            DeliveryMethod = "email"
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/online-voting/requestCode", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Election GUID is required", content);
    }

    // Helper methods

    private async Task<Guid> SetupOpenElection()
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Test Open Election",
            OnlineWhenOpen = DateTime.UtcNow.AddHours(-1),
            OnlineWhenClose = DateTime.UtcNow.AddHours(1),
            TallyStatus = "Open",
            RowVersion = new byte[8]
        };

        context.Elections.Add(election);
        await context.SaveChangesAsync();

        return election.ElectionGuid;
    }

    private async Task<(Guid electionGuid, string voterId)> SetupOpenElectionWithVoter(
        string? email = null,
        string? phone = null,
        string? kioskCode = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var electionGuid = Guid.NewGuid();

        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = "Test Open Election with Voter",
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

        var voterId = email ?? phone ?? kioskCode 
            ?? throw new InvalidOperationException("At least one of email, phone, or kioskCode must be provided");
        return (electionGuid, voterId);
    }

    private async Task<(Guid electionGuid, string voterId)> SetupClosedElectionWithVoter(
        string? email = null,
        string? phone = null)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var electionGuid = Guid.NewGuid();

        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = "Test Closed Election",
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

        var voterId = email ?? phone 
            ?? throw new InvalidOperationException("At least one of email or phone must be provided");
        return (electionGuid, voterId);
    }
}
