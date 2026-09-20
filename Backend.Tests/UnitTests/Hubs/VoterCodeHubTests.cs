using Backend.DTOs.SignalR;
using Backend.Hubs;
using Backend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Backend.Tests.UnitTests.Hubs;

public class VoterCodeHubTests
{
    private static (VoterCodeHub Hub, Mock<IGroupManager> Groups, Mock<ISingleClientProxy> Caller)
        CreateHub(IVoterCodeDeliveryChannelService channels, string connectionId = "conn-code")
    {
        var hub = new VoterCodeHub(channels, NullLogger<VoterCodeHub>.Instance);
        var context = new Mock<HubCallerContext>();
        context.Setup(c => c.ConnectionId).Returns(connectionId);
        context.Setup(c => c.Items).Returns(new Dictionary<object, object?>());

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

        var caller = new Mock<ISingleClientProxy>();
        caller
            .Setup(c => c.SendCoreAsync(
                It.IsAny<string>(),
                It.IsAny<object?[]>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var clients = new Mock<IHubCallerClients>();
        clients.Setup(c => c.Caller).Returns(caller.Object);

        hub.Context = context.Object;
        hub.Groups = groups.Object;
        hub.Clients = clients.Object;
        return (hub, groups, caller);
    }

    [Fact]
    public async Task Join_rejects_unknown_token()
    {
        var channels = new VoterCodeDeliveryChannelService();
        var (hub, groups, _) = CreateHub(channels);

        var ex = await Assert.ThrowsAsync<HubException>(() => hub.Join("not-a-real-token"));
        Assert.Contains("Invalid or expired", ex.Message);
        groups.Verify(
            g => g.AddToGroupAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Join_rejects_expired_token()
    {
        var clock = new ShiftTimeProvider(DateTimeOffset.Parse("2026-09-20T12:00:00Z"));
        var channels = new VoterCodeDeliveryChannelService(clock);
        var issued = channels.Issue();
        clock.Advance(VoterCodeDeliveryChannelService.Lifetime.Add(TimeSpan.FromMinutes(1)));
        var (hub, _, _) = CreateHub(channels);

        await Assert.ThrowsAsync<HubException>(() => hub.Join(issued.RawToken));
    }

    [Fact]
    public async Task Join_adds_to_channel_group_and_replays_status_without_otp()
    {
        var channels = new VoterCodeDeliveryChannelService();
        var issued = channels.Issue();
        channels.RecordStatus(issued.ChannelId, VoterCodeDeliveryStatusDto.Sending());
        channels.RecordStatus(issued.ChannelId, VoterCodeDeliveryStatusDto.Sent());
        var (hub, groups, caller) = CreateHub(channels);

        await hub.Join(issued.RawToken);

        groups.Verify(
            g => g.AddToGroupAsync(
                "conn-code",
                VoterCodeHub.GetGroupName(issued.ChannelId),
                It.IsAny<CancellationToken>()),
            Times.Once);

        var statusSends = caller.Invocations
            .Where(i => i.Method.Name == nameof(IClientProxy.SendCoreAsync)
                        && Equals(i.Arguments[0], "codeDeliveryStatus"))
            .ToList();
        Assert.Equal(2, statusSends.Count);
        foreach (var send in statusSends)
        {
            var args = Assert.IsType<object?[]>(send.Arguments[1]);
            var payload = Assert.IsType<VoterCodeDeliveryStatusDto>(args[0]);
            Assert.DoesNotContain(
                "Code",
                payload.GetType().GetProperties().Select(p => p.Name));
            Assert.DoesNotContain(
                "VerifyCode",
                payload.GetType().GetProperties().Select(p => p.Name));
            Assert.False(string.Equals(payload.MessageKey, "ABC123", StringComparison.Ordinal));
        }
    }

    [Fact]
    public void GetGroupName_uses_VoterCode_prefix()
    {
        Assert.Equal("VoterCodeabc", VoterCodeHub.GetGroupName("abc"));
    }

    private sealed class ShiftTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ShiftTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan delta) => _utcNow += delta;
    }
}
