using Backend.Entities;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.UnitTests.Helpers;

public class OnlineVoterPhoneHelperTests : ServiceTestBase
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task EnsureOnlineVoterForPhoneAsync_NoPhone_DoesNotAddRow(string? phone)
    {
        await OnlineVoterPhoneHelper.EnsureOnlineVoterForPhoneAsync(Context, phone);
        await Context.SaveChangesAsync();

        Assert.Empty(Context.OnlineVoters);
    }

    [Fact]
    public async Task EnsureOnlineVoterForPhoneAsync_NewPhone_AddsPhoneRowWithNullAuthFields()
    {
        const string phone = "+14168972671";

        await OnlineVoterPhoneHelper.EnsureOnlineVoterForPhoneAsync(Context, phone);
        await Context.SaveChangesAsync();

        var row = Assert.Single(Context.OnlineVoters);
        Assert.Equal(phone, row.VoterId);
        Assert.Equal(OnlineVoterPhoneHelper.PhoneVoterIdType, row.VoterIdType);
        Assert.Null(row.WhenRegistered);
        Assert.Null(row.WhenLastLogin);
        Assert.Null(row.SmsStatus);
    }

    [Fact]
    public async Task EnsureOnlineVoterForPhoneAsync_ExistingRow_DoesNotDuplicateOrWipeFields()
    {
        const string phone = "+14168972671";
        var registered = DateTimeOffset.Parse("2026-01-01T00:00:00Z");
        var lastLogin = DateTimeOffset.Parse("2026-01-02T00:00:00Z");
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = "P",
            SmsStatus = "landline",
            WhenRegistered = registered,
            WhenLastLogin = lastLogin
        });
        await Context.SaveChangesAsync();

        await OnlineVoterPhoneHelper.EnsureOnlineVoterForPhoneAsync(Context, phone);
        await Context.SaveChangesAsync();

        var row = Assert.Single(await Context.OnlineVoters.Where(ov => ov.VoterId == phone).ToListAsync());
        Assert.Equal("P", row.VoterIdType);
        Assert.Equal("landline", row.SmsStatus);
        Assert.Equal(registered, row.WhenRegistered);
        Assert.Equal(lastLogin, row.WhenLastLogin);
    }

    [Theory]
    [InlineData("E")]
    [InlineData("C")]
    [InlineData("T")]
    public async Task EnsureOnlineVoterForPhoneAsync_VoterIdOccupiedByOtherType_SkipsWithoutWipeOrThrow(
        string existingType)
    {
        const string phone = "+14168972671";
        var registered = DateTimeOffset.Parse("2026-06-01T00:00:00Z");
        var lastLogin = DateTimeOffset.Parse("2026-06-02T00:00:00Z");
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = existingType,
            SmsStatus = "admin",
            WhenRegistered = registered,
            WhenLastLogin = lastLogin
        });
        await Context.SaveChangesAsync();

        var thrown = await Record.ExceptionAsync(async () =>
        {
            await OnlineVoterPhoneHelper.EnsureOnlineVoterForPhoneAsync(Context, phone);
            await Context.SaveChangesAsync();
        });

        Assert.Null(thrown);
        var row = Assert.Single(await Context.OnlineVoters.Where(ov => ov.VoterId == phone).ToListAsync());
        Assert.Equal(existingType, row.VoterIdType);
        Assert.Equal("admin", row.SmsStatus);
        Assert.Equal(registered, row.WhenRegistered);
        Assert.Equal(lastLogin, row.WhenLastLogin);
    }

    [Fact]
    public async Task EnsureOnlineVotersForPhonesAsync_Batch_AddsMissingOnly()
    {
        const string existing = "+14168972671";
        const string added = "+14168972672";
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = existing,
            VoterIdType = "P",
            SmsStatus = "OK",
            WhenRegistered = DateTimeOffset.Parse("2026-04-01T00:00:00Z")
        });
        await Context.SaveChangesAsync();

        await OnlineVoterPhoneHelper.EnsureOnlineVotersForPhonesAsync(
            Context,
            [existing, added, added, null, ""]);
        await Context.SaveChangesAsync();

        Assert.Equal(2, await Context.OnlineVoters.CountAsync());
        var existingRow = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == existing);
        Assert.Equal("OK", existingRow.SmsStatus);
        Assert.NotNull(existingRow.WhenRegistered);
        var newRow = await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == added);
        Assert.Equal("P", newRow.VoterIdType);
        Assert.Null(newRow.WhenRegistered);
    }
}
