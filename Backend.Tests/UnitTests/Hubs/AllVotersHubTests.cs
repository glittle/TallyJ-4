using System.Security.Claims;
using Backend.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.UnitTests.Hubs;

public class AllVotersHubTests
{
    private (AllVotersHub Hub, Mock<IGroupManager> Groups) CreateHub()
    {
        var hub = new AllVotersHub(NullLogger<AllVotersHub>.Instance);
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
        return (hub, groups);
    }

    [Fact]
    public async Task Join_adds_to_global_AllVoters_group()
    {
        var (hub, groups) = CreateHub();

        await hub.Join();

        groups.Verify(
            g => g.AddToGroupAsync(
                "conn-all",
                AllVotersHub.GetGroupName(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public void GetGroupName_is_global_AllVoters()
    {
        Assert.Equal("AllVoters", AllVotersHub.GetGroupName());
    }
}
