using Backend.DTOs.SignalR;
using Backend.Enumerations;
using Backend.Hubs;
using Backend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Contract tests for MainHub fan-out: event names and group targets must match the SPA.
/// </summary>
public class SignalRNotificationServiceTests
{
    private readonly Guid _electionGuid = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private (
        SignalRNotificationService Service,
        Mock<IHubClients> MainClients,
        Dictionary<string, Mock<IClientProxy>> GroupProxies) CreateService()
    {
        var mainHub = new Mock<IHubContext<MainHub>>();
        var analyzeHub = new Mock<IHubContext<AnalyzeHub>>();
        var ballotImportHub = new Mock<IHubContext<BallotImportHub>>();
        var frontDeskHub = new Mock<IHubContext<FrontDeskHub>>();
        var publicHub = new Mock<IHubContext<PublicHub>>();
        var mainClients = new Mock<IHubClients>();
        var groupProxies = new Dictionary<string, Mock<IClientProxy>>();

        mainHub.Setup(h => h.Clients).Returns(mainClients.Object);
        mainClients
            .Setup(c => c.Group(It.IsAny<string>()))
            .Returns((string groupName) =>
            {
                if (!groupProxies.TryGetValue(groupName, out var proxy))
                {
                    proxy = new Mock<IClientProxy>();
                    proxy
                        .Setup(p => p.SendCoreAsync(
                            It.IsAny<string>(),
                            It.IsAny<object?[]>(),
                            It.IsAny<CancellationToken>()))
                        .Returns(Task.CompletedTask);
                    groupProxies[groupName] = proxy;
                }

                return proxy.Object;
            });

        var service = new SignalRNotificationService(
            mainHub.Object,
            analyzeHub.Object,
            ballotImportHub.Object,
            frontDeskHub.Object,
            publicHub.Object,
            NullLogger<SignalRNotificationService>.Instance);

        return (service, mainClients, groupProxies);
    }

    [Fact]
    public async Task SendElectionUpdateAsync_sends_statusChanged_to_base_Main_group()
    {
        var (service, mainClients, groupProxies) = CreateService();
        var update = new ElectionUpdateDto
        {
            ElectionGuid = _electionGuid,
            Name = "Test Election",
            ElectionStage = ElectionStage.GatheringBallots,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        var expectedGroup = MainHub.GetGroupName(_electionGuid);

        await service.SendElectionUpdateAsync(update);

        mainClients.Verify(c => c.Group(expectedGroup), Times.Once);
        mainClients.Verify(c => c.Group(expectedGroup + "Known"), Times.Never);
        mainClients.Verify(c => c.Group(expectedGroup + "Guest"), Times.Never);

        Assert.True(groupProxies.TryGetValue(expectedGroup, out var proxy));
        proxy!.Verify(
            p => p.SendCoreAsync(
                "statusChanged",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var invocation = proxy.Invocations.Single(i =>
            i.Method.Name == nameof(IClientProxy.SendCoreAsync)
            && Equals(i.Arguments[0], "statusChanged"));
        var capturedArgs = Assert.IsType<object?[]>(invocation.Arguments[1]);
        Assert.Single(capturedArgs);
        var dto = Assert.IsType<ElectionUpdateDto>(capturedArgs[0]);
        Assert.Equal(_electionGuid, dto.ElectionGuid);
        Assert.Equal("Test Election", dto.Name);
        Assert.Equal(ElectionStage.GatheringBallots, dto.ElectionStage);
    }

    [Fact]
    public async Task CloseOutGuestTellersAsync_sends_electionClosed_to_Guest_group_only()
    {
        var (service, mainClients, groupProxies) = CreateService();
        var baseGroup = MainHub.GetGroupName(_electionGuid);
        var guestGroup = baseGroup + "Guest";

        await service.CloseOutGuestTellersAsync(_electionGuid);

        mainClients.Verify(c => c.Group(guestGroup), Times.Once);
        mainClients.Verify(c => c.Group(baseGroup), Times.Never);
        mainClients.Verify(c => c.Group(baseGroup + "Known"), Times.Never);

        Assert.True(groupProxies.TryGetValue(guestGroup, out var proxy));
        proxy!.Verify(
            p => p.SendCoreAsync(
                "electionClosed",
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
