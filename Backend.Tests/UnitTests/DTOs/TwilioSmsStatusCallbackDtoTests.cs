using Backend.DTOs.Public;

namespace Backend.Tests.UnitTests.DTOs;

public class TwilioSmsStatusCallbackDtoTests
{
    [Fact]
    public void Sid_PrefersMessageSidThenSmsSidThenCallSid()
    {
        Assert.Equal("SM1", new TwilioSmsStatusCallbackDto
        {
            MessageSid = "SM1",
            SmsSid = "SM2",
            CallSid = "CA1"
        }.Sid);
        Assert.Equal("SM2", new TwilioSmsStatusCallbackDto { SmsSid = "SM2", CallSid = "CA1" }.Sid);
        Assert.Equal("CA1", new TwilioSmsStatusCallbackDto { CallSid = "CA1" }.Sid);
    }

    [Fact]
    public void Status_PrefersMessageStatusThenSmsStatusThenCallStatus()
    {
        Assert.Equal("delivered", new TwilioSmsStatusCallbackDto
        {
            MessageStatus = "delivered",
            SmsStatus = "sent",
            CallStatus = "completed"
        }.Status);
        Assert.Equal("completed", new TwilioSmsStatusCallbackDto { CallStatus = "completed" }.Status);
    }
}
