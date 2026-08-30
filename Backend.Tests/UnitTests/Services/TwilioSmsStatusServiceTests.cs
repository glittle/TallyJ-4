using Backend.Entities;
using Backend.Helpers;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Twilio status-callback auto-learn: selected terminal failures stamp SmsStatus
/// on an existing phone OnlineVoter row; transients and non-P rows are left alone.
/// </summary>
public class TwilioSmsStatusServiceTests : ServiceTestBase
{
    private const string StoredPhone = "+14168972671";
    private const string TwilioTo = "+14168972671";

    private readonly TwilioSmsStatusService _service;

    public TwilioSmsStatusServiceTests()
    {
        _service = new TwilioSmsStatusService(Context, Mock.Of<ILogger<TwilioSmsStatusService>>());
    }

    [Fact]
    public async Task Undelivered_30003_OnPRowWithNullSmsStatus_BecomesTwilio30003()
    {
        await SeedPhoneVoter(StoredPhone, smsStatus: null);

        await _service.ProcessCallbackAsync("SMtest", "undelivered", TwilioTo, 30003);

        var row = await PhoneRow(StoredPhone);
        Assert.Equal("twilio-30003", row.SmsStatus);
    }

    [Theory]
    [InlineData(30005, "twilio-30005")]
    [InlineData(30006, "twilio-30006")]
    [InlineData(30004, "twilio-30004")]
    [InlineData(21211, "twilio-21211")]
    [InlineData(21614, "twilio-21614")]
    public async Task Failed_SelectedCode_OnPRowWithNullSmsStatus_BecomesTwilioCode(
        int errorCode, string expected)
    {
        await SeedPhoneVoter(StoredPhone, smsStatus: null);

        await _service.ProcessCallbackAsync("SMtest", "failed", TwilioTo, errorCode);

        var row = await PhoneRow(StoredPhone);
        Assert.Equal(expected, row.SmsStatus);
    }

    [Fact]
    public async Task Undelivered_NoErrorCode_SmsStatusUnchanged()
    {
        await SeedPhoneVoter(StoredPhone, smsStatus: null);

        await _service.ProcessCallbackAsync("SMtest", "undelivered", TwilioTo, errorCode: null);

        var row = await PhoneRow(StoredPhone);
        Assert.Null(row.SmsStatus);
    }

    [Fact]
    public async Task Undelivered_UnlistedErrorCode_SmsStatusUnchanged()
    {
        await SeedPhoneVoter(StoredPhone, smsStatus: null);

        await _service.ProcessCallbackAsync("SMtest", "undelivered", TwilioTo, 30007);

        var row = await PhoneRow(StoredPhone);
        Assert.Null(row.SmsStatus);
    }

    [Theory]
    [InlineData("delivered")]
    [InlineData("sent")]
    [InlineData("queued")]
    [InlineData("sending")]
    public async Task NonTerminalStatus_SmsStatusUnchanged(string status)
    {
        await SeedPhoneVoter(StoredPhone, smsStatus: null);

        await _service.ProcessCallbackAsync("SMtest", status, TwilioTo, 30003);

        var row = await PhoneRow(StoredPhone);
        Assert.Null(row.SmsStatus);
    }

    [Fact]
    public async Task ExistingBlockReason_LeftAlone()
    {
        await SeedPhoneVoter(StoredPhone, smsStatus: "admin");

        await _service.ProcessCallbackAsync("SMtest", "undelivered", TwilioTo, 30003);

        var row = await PhoneRow(StoredPhone);
        Assert.Equal("admin", row.SmsStatus);
    }

    [Fact]
    public async Task ExistingOk_OverwrittenBySelectedFailureCode()
    {
        await SeedPhoneVoter(StoredPhone, smsStatus: OnlineVoterSmsStatus.Ok);

        await _service.ProcessCallbackAsync("SMtest", "undelivered", TwilioTo, 30003);

        var row = await PhoneRow(StoredPhone);
        Assert.Equal("twilio-30003", row.SmsStatus);
    }

    [Theory]
    [InlineData("E")]
    [InlineData("C")]
    [InlineData("T")]
    public async Task NonPOccupantOfSameVoterId_QueryIsPhoneTypeScoped_NotUpdated(string voterIdType)
    {
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = StoredPhone,
            VoterIdType = voterIdType,
            SmsStatus = null
        });
        await Context.SaveChangesAsync();

        await _service.ProcessCallbackAsync("SMtest", "undelivered", TwilioTo, 30003);

        var row = Assert.Single(await Context.OnlineVoters.ToListAsync());
        Assert.Equal(voterIdType, row.VoterIdType);
        Assert.Null(row.SmsStatus);
    }

    [Fact]
    public async Task NoPRow_DoesNotInsert()
    {
        await _service.ProcessCallbackAsync("SMtest", "undelivered", TwilioTo, 30003);

        Assert.Empty(await Context.OnlineVoters.ToListAsync());
    }

    [Fact]
    public async Task TwilioToWithPlus_MatchesStoredPhoneWithoutPlus()
    {
        await SeedPhoneVoter("14168972671", smsStatus: null);

        await _service.ProcessCallbackAsync("SMtest", "undelivered", "+14168972671", 30003);

        var row = await PhoneRow("14168972671");
        Assert.Equal("twilio-30003", row.SmsStatus);
    }

    [Fact]
    public async Task Delivered_DoesNotSetSmsStatusToOk()
    {
        await SeedPhoneVoter(StoredPhone, smsStatus: null);

        await _service.ProcessCallbackAsync("SMtest", "delivered", TwilioTo, errorCode: null);

        var row = await PhoneRow(StoredPhone);
        Assert.Null(row.SmsStatus);
    }

    [Fact]
    public async Task ExistingSmsLog_Updated_OnlineVoterLearned()
    {
        await SeedPhoneVoter(StoredPhone, smsStatus: null);
        Context.SmsLogs.Add(new SmsLog
        {
            SmsSid = "SMexisting",
            Phone = StoredPhone,
            SentDate = DateTimeOffset.Parse("2026-08-01T00:00:00Z"),
            LastStatus = "sent"
        });
        await Context.SaveChangesAsync();

        await _service.ProcessCallbackAsync("SMexisting", "undelivered", TwilioTo, 30003);

        var log = Assert.Single(await Context.SmsLogs.ToListAsync());
        Assert.Equal("undelivered", log.LastStatus);
        Assert.Equal(30003, log.ErrorCode);
        Assert.NotNull(log.LastDate);
        var row = await PhoneRow(StoredPhone);
        Assert.Equal("twilio-30003", row.SmsStatus);
    }

    [Fact]
    public async Task NoSmsLogRow_DoesNotInsertLog_StillLearnsSmsStatus()
    {
        await SeedPhoneVoter(StoredPhone, smsStatus: null);

        await _service.ProcessCallbackAsync("SMmissing", "undelivered", TwilioTo, 30003);

        Assert.Empty(await Context.SmsLogs.ToListAsync());
        var row = await PhoneRow(StoredPhone);
        Assert.Equal("twilio-30003", row.SmsStatus);
    }

    private async Task SeedPhoneVoter(string voterId, string? smsStatus)
    {
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = voterId,
            VoterIdType = OnlineVoterPhoneHelper.PhoneVoterIdType,
            SmsStatus = smsStatus
        });
        await Context.SaveChangesAsync();
    }

    private Task<OnlineVoter> PhoneRow(string voterId) =>
        Context.OnlineVoters.SingleAsync(ov => ov.VoterId == voterId);
}
