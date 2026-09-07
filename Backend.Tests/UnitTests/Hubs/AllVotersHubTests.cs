using System.Security.Claims;
using Backend.Hubs;
using Backend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.UnitTests.Hubs;

public class AllVotersHubTests
{
    private (AllVotersHub Hub, Mock<IGroupManager> Groups, Mock<IOnlineVoterPresenceService> Presence)
        CreateHub()
    {
        var presence = new Mock<IOnlineVoterPresenceService>();
        var hub = new AllVotersHub(NullLogger<AllVotersHub>.Instance, presence.Object);
        var context = new Mock<HubCallerContext>();
        context.Setup(c => c.ConnectionId).Returns("conn-all");
        context.Setup(c => c.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("voterId", "alice@example.com"),
            new Claim("voterType", "online"),
        ], "TestAuth")));

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
        return (hub, groups, presence);
    }

    [Fact]
    public async Task Join_adds_to_global_AllVoters_group()
    {
        var (hub, groups, presence) = CreateHub();

        await hub.Join();

        groups.Verify(
            g => g.AddToGroupAsync(
                "conn-all",
                AllVotersHub.GetGroupName(),
                It.IsAny<CancellationToken>()),
            Times.Once);
        presence.Verify(
            p => p.AddSession(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task JoinElection_records_anonymous_session_for_election()
    {
        var (hub, _, presence) = CreateHub();
        var electionGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

        await hub.JoinElection(electionGuid);

        presence.Verify(p => p.AddSession(electionGuid, "conn-all"), Times.Once);
    }

    [Fact]
    public async Task JoinElection_rejects_empty_election()
    {
        var (hub, _, presence) = CreateHub();

        await Assert.ThrowsAsync<HubException>(() => hub.JoinElection(Guid.Empty));
        presence.Verify(
            p => p.AddSession(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
    }

    [Fact]
    public async Task LeaveElection_removes_session()
    {
        var (hub, _, presence) = CreateHub();

        await hub.LeaveElection();

        presence.Verify(p => p.RemoveSession("conn-all"), Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_removes_session()
    {
        var (hub, _, presence) = CreateHub();

        await hub.OnDisconnectedAsync(null);

        presence.Verify(p => p.RemoveSession("conn-all"), Times.Once);
    }

    [Fact]
    public void GetGroupName_is_global_AllVoters()
    {
        Assert.Equal("AllVoters", AllVotersHub.GetGroupName());
    }
}
