using Backend.DTOs.SignalR;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Twilio callbacks push delivery status on the requestCode channel when a SID is bound.
/// </summary>
public class TwilioSmsStatusServiceDeliveryChannelTests : ServiceTestBase
{
    [Fact]
    public async Task Delivered_Callback_PushesDeliveredAndFinal_WithoutOtp()
    {
        var channels = new VoterCodeDeliveryChannelService();
        var issued = channels.Issue();
        channels.BindProviderSid(issued.ChannelId, "SMchan");
        var pushed = new List<VoterCodeDeliveryStatusDto>();
        var signalR = new Mock<ISignalRNotificationService>();
        signalR
            .Setup(s => s.SendVoterCodeDeliveryStatusAsync(
                issued.ChannelId,
                It.IsAny<VoterCodeDeliveryStatusDto>()))
            .Callback<string, VoterCodeDeliveryStatusDto>((_, status) => pushed.Add(status))
            .Returns(Task.CompletedTask);

        var service = new TwilioSmsStatusService(
            Context,
            Mock.Of<ILogger<TwilioSmsStatusService>>(),
            channels,
            signalR.Object);

        await service.ProcessCallbackAsync("SMchan", "delivered", "+14168972671", null);

        Assert.Contains(pushed, s => s.Status == VoterCodeDeliveryStatuses.Delivered);
        Assert.Contains(pushed, s => s.Status == VoterCodeDeliveryStatuses.Final && s.Okay == true);
        Assert.All(pushed, status =>
        {
            Assert.Null(status.GetType().GetProperty("VerifyCode"));
            Assert.DoesNotContain("+14168972671", status.MessageKey ?? "");
            Assert.Equal("delivered", status.ProviderStatus);
        });
    }

    [Fact]
    public async Task UnknownSid_DoesNotPush()
    {
        var channels = new VoterCodeDeliveryChannelService();
        var signalR = new Mock<ISignalRNotificationService>();
        var service = new TwilioSmsStatusService(
            Context,
            Mock.Of<ILogger<TwilioSmsStatusService>>(),
            channels,
            signalR.Object);

        await service.ProcessCallbackAsync("SMunknown", "delivered", "+14168972671", null);

        signalR.Verify(
            s => s.SendVoterCodeDeliveryStatusAsync(
                It.IsAny<string>(),
                It.IsAny<VoterCodeDeliveryStatusDto>()),
            Times.Never);
    }
}
