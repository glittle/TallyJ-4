using System.Security.Claims;
using Backend.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.UnitTests.Hubs;

/// <summary>
/// Ensures personal groups are server-derived from the JWT voterId claim
/// (voter A cannot join voter B's group).
/// </summary>
public class VoterPersonalHubTests
{
    private (VoterPersonalHub Hub, Mock<IGroupManager> Groups) CreateHub(ClaimsPrincipal? user)
    {
        var hub = new VoterPersonalHub(NullLogger<VoterPersonalHub>.Instance);
        var context = new Mock<HubCallerContext>();
        context.Setup(c => c.ConnectionId).Returns("conn-voter");
        context.Setup(c => c.User).Returns(user!);

        var groups = new Mock<IGroupManager>();
        groups
            .Setup(g => g.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        groups
            .Setup(g => g.RemoveFromGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        hub.Context = context.Object;
        hub.Groups = groups.Object;
        return (hub, groups);
    }

    private static ClaimsPrincipal OnlineVoterPrincipal(string voterId)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("voterId", voterId),
            new Claim("voterIdType", "E"),
            new Claim("voterType", "online"),
        ], "TestAuth"));
    }

    [Fact]
    public async Task Join_uses_server_voterId_claim_for_group()
    {
        const string voterId = "alice@example.com";
        var (hub, groups) = CreateHub(OnlineVoterPrincipal(voterId));

        await hub.Join();

        groups.Verify(
            g => g.AddToGroupAsync(
                "conn-voter",
                VoterPersonalHub.GetGroupName(voterId),
                It.IsAny<CancellationToken>()),
            Times.Once);
        groups.Verify(
            g => g.AddToGroupAsync(
                "conn-voter",
                VoterPersonalHub.GetGroupName("bob@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Join_without_voterId_throws()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("voterType", "online"),
        ], "TestAuth"));
        var (hub, _) = CreateHub(user);

        await Assert.ThrowsAsync<HubException>(() => hub.Join());
    }

    [Fact]
    public void GetGroupName_prefixes_voter_id()
    {
        Assert.Equal("Voteralice@example.com", VoterPersonalHub.GetGroupName("alice@example.com"));
    }
}
