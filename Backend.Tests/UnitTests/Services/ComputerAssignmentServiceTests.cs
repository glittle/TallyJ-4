using Backend.Hubs;
using Backend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.UnitTests.Services;

public class ComputerAssignmentServiceTests
{
    private readonly Guid _electionGuid = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private ComputerAssignmentService CreateService(TimeSpan? guestCloseoutDelay = null)
    {
        var (service, _) = CreateServiceWithGuestProxy(guestCloseoutDelay);
        return service;
    }

    private (ComputerAssignmentService Service, Mock<IClientProxy> GuestProxy) CreateServiceWithGuestProxy(
        TimeSpan? guestCloseoutDelay = null,
        ISignalRNotificationService? signalRNotificationService = null)
    {
        var hubContext = new Mock<IHubContext<MainHub>>();
        var clients = new Mock<IHubClients>();
        var guestProxy = new Mock<IClientProxy>();
        var otherProxy = new Mock<IClientProxy>();

        guestProxy
            .Setup(p => p.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        hubContext.Setup(h => h.Clients).Returns(clients.Object);
        clients.Setup(c => c.Group(It.Is<string>(g => g.EndsWith("Guest")))).Returns(guestProxy.Object);
        clients.Setup(c => c.Group(It.Is<string>(g => !g.EndsWith("Guest")))).Returns(otherProxy.Object);

        var delay = guestCloseoutDelay ?? ComputerAssignmentService.DefaultGuestCloseoutDelay;
        var service = new ComputerAssignmentService(
            hubContext.Object,
            NullLogger<ComputerAssignmentService>.Instance,
            signalRNotificationService,
            delay);

        return (service, guestProxy);
    }

    [Fact]
    public void AssignCode_assigns_sequential_codes_for_new_clients()
    {
        var service = CreateService();

        var first = service.AssignCode(_electionGuid, "client-1", "conn-1", isMainTeller: true);
        var second = service.AssignCode(_electionGuid, "client-2", "conn-2", isMainTeller: true);

        Assert.Equal("A", first);
        Assert.Equal("B", second);
    }

    [Fact]
    public void AssignCode_reassigns_prior_code_when_available()
    {
        var service = CreateService();

        var initial = service.AssignCode(_electionGuid, "client-a", "conn-a", isMainTeller: true);
        service.AssignCode(_electionGuid, "client-b", "conn-b", isMainTeller: true);

        service.ReleaseConnection("conn-a");

        var reassigned = service.AssignCode(_electionGuid, "client-a", "conn-a2", isMainTeller: true);

        Assert.Equal("A", initial);
        Assert.Equal("A", reassigned);
    }

    [Fact]
    public void AssignCode_skips_taken_code_and_assigns_next_after_max_active()
    {
        var service = CreateService();

        service.AssignCode(_electionGuid, "client-a", "conn-a", isMainTeller: true);
        service.AssignCode(_electionGuid, "client-b", "conn-b", isMainTeller: true);
        service.AssignCode(_electionGuid, "client-c", "conn-c", isMainTeller: true);
        service.ReleaseConnection("conn-c");
        service.AssignCode(_electionGuid, "client-d", "conn-d", isMainTeller: true);

        var next = service.AssignCode(_electionGuid, "client-e", "conn-e", isMainTeller: true);

        Assert.Equal("E", next);
    }

    [Fact]
    public void ReleaseConnection_resets_sequence_when_last_workstation_disconnects()
    {
        var service = CreateService();

        service.AssignCode(_electionGuid, "client-1", "conn-1", isMainTeller: true);
        service.AssignCode(_electionGuid, "client-2", "conn-2", isMainTeller: true);
        service.ReleaseConnection("conn-1");
        service.ReleaseConnection("conn-2");

        var restarted = service.AssignCode(_electionGuid, "client-3", "conn-3", isMainTeller: true);

        Assert.Equal("A", restarted);
    }

    [Fact]
    public void AssignCode_firstMainTeller_notifiesGuestJoinListRefresh()
    {
        var signalR = new Mock<ISignalRNotificationService>();
        signalR.Setup(s => s.SendPublicElectionListUpdateAsync()).Returns(Task.CompletedTask);
        var service = CreateServiceWithGuestProxy(signalRNotificationService: signalR.Object).Service;

        service.AssignCode(_electionGuid, "main-1", "conn-main", isMainTeller: true);

        signalR.Verify(s => s.SendPublicElectionListUpdateAsync(), Times.Once);
    }

    [Fact]
    public void AssignCode_secondMainTeller_doesNotNotifyGuestJoinList()
    {
        var signalR = new Mock<ISignalRNotificationService>();
        signalR.Setup(s => s.SendPublicElectionListUpdateAsync()).Returns(Task.CompletedTask);
        var service = CreateServiceWithGuestProxy(signalRNotificationService: signalR.Object).Service;

        service.AssignCode(_electionGuid, "main-1", "conn-main-1", isMainTeller: true);
        service.AssignCode(_electionGuid, "main-2", "conn-main-2", isMainTeller: true);

        signalR.Verify(s => s.SendPublicElectionListUpdateAsync(), Times.Once);
    }

    [Fact]
    public void ReleaseConnection_lastMainTeller_notifiesGuestJoinListRefresh()
    {
        var signalR = new Mock<ISignalRNotificationService>();
        signalR.Setup(s => s.SendPublicElectionListUpdateAsync()).Returns(Task.CompletedTask);
        var service = CreateServiceWithGuestProxy(signalRNotificationService: signalR.Object).Service;

        service.AssignCode(_electionGuid, "main-1", "conn-main", isMainTeller: true);
        service.ReleaseConnection("conn-main");

        signalR.Verify(s => s.SendPublicElectionListUpdateAsync(), Times.Exactly(2));
    }

    [Fact]
    public void AssignCode_releases_connection_from_other_election_when_switching()
    {
        var electionA = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var electionB = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var service = CreateService();

        service.AssignCode(electionA, "client-1", "conn-1", isMainTeller: true);
        service.AssignCode(electionB, "client-1", "conn-1", isMainTeller: true);

        Assert.False(service.HasActiveMainTeller(electionA));
        Assert.True(service.HasActiveMainTeller(electionB));
    }

    [Fact]
    public void HasActiveMainTeller_and_CanGuestJoin_reflect_connection_state()
    {
        var service = CreateService();

        Assert.False(service.HasActiveMainTeller(_electionGuid));
        Assert.False(service.CanGuestJoin(_electionGuid));

        service.AssignCode(_electionGuid, "main-1", "conn-main", isMainTeller: true);

        Assert.True(service.HasActiveMainTeller(_electionGuid));
        Assert.True(service.CanGuestJoin(_electionGuid));
        Assert.False(service.IsGuestGracePeriodActive(_electionGuid));
    }

    [Fact]
    public async Task CanGuestJoin_true_during_grace_after_last_main_disconnect()
    {
        var gracePeriod = TimeSpan.FromMilliseconds(200);
        var (service, _) = CreateServiceWithGuestProxy(gracePeriod);

        service.AssignCode(_electionGuid, "main-1", "conn-main", isMainTeller: true);
        service.AssignCode(_electionGuid, "guest-1", "conn-guest", isMainTeller: false);
        service.ReleaseConnection("conn-main");

        Assert.False(service.HasActiveMainTeller(_electionGuid));
        Assert.True(service.IsGuestGracePeriodActive(_electionGuid));
        Assert.True(service.CanGuestJoin(_electionGuid));

        await WaitUntilAsync(
            () => !service.CanGuestJoin(_electionGuid),
            gracePeriod + TimeSpan.FromSeconds(2),
            "Guest join should be blocked after the close-out grace period expires.");
    }

    [Fact]
    public void GetActiveComputers_lists_current_connections()
    {
        var service = CreateService();

        service.AssignCode(_electionGuid, "client-1", "conn-1", isMainTeller: true);
        service.AssignCode(_electionGuid, "client-2", "conn-2", isMainTeller: false);

        var active = service.GetActiveComputers(_electionGuid);

        Assert.Equal(2, active.Count);
        Assert.Contains(active, c => c.ComputerCode == "A" && c.IsMainTeller);
        Assert.Contains(active, c => c.ComputerCode == "B" && !c.IsMainTeller);
    }

    [Fact]
    public async Task ReleaseConnection_lastMainTeller_triggersGuestCloseoutAfterDelay()
    {
        var (service, guestProxy) = CreateServiceWithGuestProxy(TimeSpan.FromMilliseconds(80));

        service.AssignCode(_electionGuid, "main-1", "conn-main", isMainTeller: true);
        service.AssignCode(_electionGuid, "guest-1", "conn-guest", isMainTeller: false);
        service.ReleaseConnection("conn-main");

        await Task.Delay(200);

        guestProxy.Verify(
            p => p.SendCoreAsync(
                "electionClosed",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AssignCode_guestRejoin_afterLastMainDisconnect_doesNotCancelGuestCloseout()
    {
        var (service, guestProxy) = CreateServiceWithGuestProxy(TimeSpan.FromMilliseconds(150));

        service.AssignCode(_electionGuid, "main-1", "conn-main", isMainTeller: true);
        service.AssignCode(_electionGuid, "guest-1", "conn-guest", isMainTeller: false);
        service.ReleaseConnection("conn-main");

        // Guest reconnect must not cancel the close-out timer started when the last main left.
        service.AssignCode(_electionGuid, "guest-1", "conn-guest-2", isMainTeller: false);

        await Task.Delay(500);

        guestProxy.Verify(
            p => p.SendCoreAsync(
                "electionClosed",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AssignCode_mainTellerRejoin_beforeDelay_cancelsGuestCloseout()
    {
        var (service, guestProxy) = CreateServiceWithGuestProxy(TimeSpan.FromMilliseconds(200));

        service.AssignCode(_electionGuid, "main-1", "conn-main", isMainTeller: true);
        service.AssignCode(_electionGuid, "guest-1", "conn-guest", isMainTeller: false);
        service.ReleaseConnection("conn-main");

        service.AssignCode(_electionGuid, "main-1", "conn-main-2", isMainTeller: true);

        await Task.Delay(350);

        guestProxy.Verify(
            p => p.SendCoreAsync(
                "electionClosed",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ReleaseConnection_guestChurn_afterLastMainDisconnect_doesNotRestartCloseoutTimer()
    {
        var (service, guestProxy) = CreateServiceWithGuestProxy(TimeSpan.FromMilliseconds(80));

        service.AssignCode(_electionGuid, "main-1", "conn-main", isMainTeller: true);
        service.AssignCode(_electionGuid, "guest-1", "conn-guest-1", isMainTeller: false);
        service.AssignCode(_electionGuid, "guest-2", "conn-guest-2", isMainTeller: false);
        service.ReleaseConnection("conn-main");

        await Task.Delay(15);
        service.ReleaseConnection("conn-guest-1");

        await Task.Delay(120);

        guestProxy.Verify(
            p => p.SendCoreAsync(
                "electionClosed",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    private static async Task WaitUntilAsync(
        Func<bool> condition,
        TimeSpan timeout,
        string because)
    {
        var pollInterval = TimeSpan.FromMilliseconds(10);
        var deadline = DateTime.UtcNow + timeout;

        while (DateTime.UtcNow < deadline)
        {
            if (condition())
            {
                return;
            }

            await Task.Delay(pollInterval);
        }

        Assert.Fail(because);
    }

    /// <summary>
    /// Verification plan step 4: run assignment flow twice and assert identical success behavior.
    /// </summary>
    [Fact]
    public void AssignmentFlow_runTwice_producesIdenticalSequentialOutcomes()
    {
        for (var run = 0; run < 2; run++)
        {
            var service = CreateService();

            Assert.Equal("A", service.AssignCode(_electionGuid, $"run{run}-a", "conn-a", true));
            Assert.Equal("B", service.AssignCode(_electionGuid, $"run{run}-b", "conn-b", true));
            service.ReleaseConnection("conn-a");
            Assert.Equal("A", service.AssignCode(_electionGuid, $"run{run}-a", "conn-a2", true));
            service.ReleaseConnection("conn-a2");
            service.ReleaseConnection("conn-b");
            Assert.Equal("A", service.AssignCode(_electionGuid, $"run{run}-fresh", "conn-fresh", true));
        }
    }
}