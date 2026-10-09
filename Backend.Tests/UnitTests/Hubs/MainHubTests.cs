using System.Security.Claims;
using Backend.Authorization;
using Backend.Entities;
using Backend.Hubs;
using Backend.Services;
using Backend.Tests.UnitTests;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.UnitTests.Hubs;

public class MainHubTests : ServiceTestBase
{
    private readonly Guid _userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly Guid _electionA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private readonly Guid _electionB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private readonly Guid _electionOther = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private (MainHub Hub, Mock<IGroupManager> Groups, Mock<IComputerAssignmentService> Assignment, Mock<IElectionAccessEvaluator> Access) CreateHub(
        ClaimsPrincipal user)
    {
        var assignment = new Mock<IComputerAssignmentService>();
        var access = new Mock<IElectionAccessEvaluator>();
        access
            .Setup(a => a.CanJoinElectionAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<Guid>()))
            .ReturnsAsync(true);
        var hub = new MainHub(
            NullLogger<MainHub>.Instance,
            assignment.Object,
            Context,
            access.Object);

        var context = new Mock<HubCallerContext>();
        context.Setup(c => c.ConnectionId).Returns("conn-1");
        context.Setup(c => c.User).Returns(user);

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

        return (hub, groups, assignment, access);
    }

    private ClaimsPrincipal KnownTellerPrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim("isTeller", "false"),
        ], "TestAuth"));
    }

    private ClaimsPrincipal GuestTellerPrincipal()
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, _userId.ToString()),
            new Claim("isTeller", "true"),
            new Claim("authMethod", "AccessCode"),
            new Claim("electionGuid", _electionA.ToString()),
        ], "TestAuth"));
    }

    private void SeedMembership()
    {
        Context.JoinElectionUsers.AddRange(
            new JoinElectionUser { ElectionGuid = _electionA, UserId = _userId },
            new JoinElectionUser { ElectionGuid = _electionB, UserId = _userId });
        Context.SaveChanges();
    }

    [Fact]
    public async Task JoinElections_known_teller_joins_base_and_Known_for_member_elections_only()
    {
        SeedMembership();
        var (hub, groups, assignment, _) = CreateHub(KnownTellerPrincipal());

        var joined = await hub.JoinElections([_electionA, _electionOther, _electionB]);

        Assert.Equal(2, joined.Count);
        Assert.Contains(_electionA, joined);
        Assert.Contains(_electionB, joined);
        Assert.DoesNotContain(_electionOther, joined);

        groups.Verify(
            g => g.AddToGroupAsync("conn-1", MainHub.GetGroupName(_electionA), It.IsAny<CancellationToken>()),
            Times.Once);
        groups.Verify(
            g => g.AddToGroupAsync("conn-1", MainHub.GetGroupName(_electionA) + "Known", It.IsAny<CancellationToken>()),
            Times.Once);
        groups.Verify(
            g => g.AddToGroupAsync("conn-1", MainHub.GetGroupName(_electionB), It.IsAny<CancellationToken>()),
            Times.Once);
        groups.Verify(
            g => g.AddToGroupAsync("conn-1", MainHub.GetGroupName(_electionB) + "Known", It.IsAny<CancellationToken>()),
            Times.Once);
        groups.Verify(
            g => g.AddToGroupAsync("conn-1", MainHub.GetGroupName(_electionOther), It.IsAny<CancellationToken>()),
            Times.Never);

        assignment.Verify(
            a => a.AssignCode(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task JoinElections_guest_teller_is_rejected()
    {
        SeedMembership();
        var (hub, groups, _, _) = CreateHub(GuestTellerPrincipal());

        await Assert.ThrowsAsync<HubException>(() => hub.JoinElections([_electionA]));

        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task LeaveElections_removes_groups_without_releasing_computer_assignment()
    {
        var (hub, groups, assignment, _) = CreateHub(KnownTellerPrincipal());

        await hub.LeaveElections([_electionA, _electionB]);

        groups.Verify(
            g => g.RemoveFromGroupAsync("conn-1", MainHub.GetGroupName(_electionA), It.IsAny<CancellationToken>()),
            Times.Once);
        groups.Verify(
            g => g.RemoveFromGroupAsync("conn-1", MainHub.GetGroupName(_electionA) + "Known", It.IsAny<CancellationToken>()),
            Times.Once);
        groups.Verify(
            g => g.RemoveFromGroupAsync("conn-1", MainHub.GetGroupName(_electionA) + "Guest", It.IsAny<CancellationToken>()),
            Times.Once);
        groups.Verify(
            g => g.RemoveFromGroupAsync("conn-1", MainHub.GetGroupName(_electionB), It.IsAny<CancellationToken>()),
            Times.Once);

        assignment.Verify(a => a.ReleaseConnection(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task JoinElection_without_access_does_not_join_the_group()
    {
        var (hub, groups, _, access) = CreateHub(KnownTellerPrincipal());
        access
            .Setup(a => a.CanJoinElectionAsync(It.IsAny<ClaimsPrincipal>(), _electionOther))
            .ReturnsAsync(false);

        await Assert.ThrowsAsync<HubException>(() => hub.JoinElection(_electionOther, "client-1"));

        groups.Verify(
            g => g.AddToGroupAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
}
