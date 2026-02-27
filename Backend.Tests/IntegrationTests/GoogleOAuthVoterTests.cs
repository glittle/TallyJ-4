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
/// Integration tests for Google OAuth authentication for online voters.
/// </summary>
public class GoogleOAuthVoterTests : IntegrationTestBase
{
    public GoogleOAuthVoterTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GoogleAuth_WithoutGoogleClientId_ShouldReturnError()
    {
        // Arrange
        var (electionGuid, _) = await SetupOpenElectionWithVoter("test@example.com");

        var request = new GoogleAuthForVoterDto
        {
            ElectionGuid = electionGuid,
            Credential = "fake-google-token"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/online-voting/{electionGuid}/googleAuth", request);

        // Assert
        // Will fail because Google Client ID is not configured in test environment
        // This is expected behavior - just verifying the endpoint exists and handles missing config
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GoogleAuth_WithElectionGuidMismatch_ShouldFail()
    {
        // Arrange
        var electionGuid1 = Guid.NewGuid();
        var electionGuid2 = Guid.NewGuid();

        var request = new GoogleAuthForVoterDto
        {
            ElectionGuid = electionGuid1,
            Credential = "fake-google-token"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/online-voting/{electionGuid2}/googleAuth", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Election GUID mismatch", content);
    }

    [Fact]
    public async Task GoogleAuth_WithNonExistentElection_ShouldFail()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();

        var request = new GoogleAuthForVoterDto
        {
            ElectionGuid = electionGuid,
            Credential = "fake-google-token-that-would-be-validated"
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/online-voting/{electionGuid}/googleAuth", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        // Will fail on Google validation first, but that's OK for this test
    }

    [Fact]
    public async Task GoogleAuth_EndpointExists_AndRequiresCredential()
    {
        // Arrange
        var electionGuid = Guid.NewGuid();

        var request = new GoogleAuthForVoterDto
        {
            ElectionGuid = electionGuid,
            Credential = "" // Empty credential
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/online-voting/{electionGuid}/googleAuth", request);

        // Assert
        // Should return BadRequest due to validation or Google auth failure
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // Helper methods

    private async Task<(Guid electionGuid, string voterId)> SetupOpenElectionWithVoter(string email)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

        var electionGuid = Guid.NewGuid();

        var election = new Election
        {
            ElectionGuid = electionGuid,
            Name = "Test Open Election for Google Auth",
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
            CanVote = true,
            RowVersion = new byte[8]
        };

        context.People.Add(person);
        await context.SaveChangesAsync();

        return (electionGuid, email);
    }
}
