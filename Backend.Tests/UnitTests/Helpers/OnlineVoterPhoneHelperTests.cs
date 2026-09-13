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
        Assert.Null(row.WhatsAppStatus);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindPhoneOnlineVoterAsync_NoPhone_ReturnsNull(string? phone)
    {
        var row = await OnlineVoterPhoneHelper.FindPhoneOnlineVoterAsync(Context, phone);
        Assert.Null(row);
    }

    [Fact]
    public async Task FindPhoneOnlineVoterAsync_PRow_ReturnsRow()
    {
        const string phone = "+14168972671";
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = "P",
            SmsStatus = "OK"
        });
        await Context.SaveChangesAsync();

        var row = await OnlineVoterPhoneHelper.FindPhoneOnlineVoterAsync(Context, phone);

        Assert.NotNull(row);
        Assert.Equal("P", row.VoterIdType);
        Assert.Equal("OK", row.SmsStatus);
    }

    [Fact]
    public async Task FindTrackedPhoneOnlineVoterAsync_PRow_ReturnsTrackedRow()
    {
        const string phone = "+14168972671";
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = "P",
            SmsStatus = null
        });
        await Context.SaveChangesAsync();

        var row = await OnlineVoterPhoneHelper.FindTrackedPhoneOnlineVoterAsync(Context, phone);

        Assert.NotNull(row);
        Assert.Equal("P", row.VoterIdType);
        Assert.Equal(EntityState.Unchanged, Context.Entry(row).State);
        row.SmsStatus = "twilio-30003";
        await Context.SaveChangesAsync();
        Assert.Equal("twilio-30003", (await Context.OnlineVoters.SingleAsync(ov => ov.VoterId == phone)).SmsStatus);
    }

    [Fact]
    public async Task FindTrackedPhoneOnlineVoterAsync_AfterEnsure_FindsUnsavedAddedRow()
    {
        const string phone = "+14168972672";
        await OnlineVoterPhoneHelper.EnsureOnlineVoterForPhoneAsync(Context, phone);

        var row = await OnlineVoterPhoneHelper.FindTrackedPhoneOnlineVoterAsync(Context, phone);

        Assert.NotNull(row);
        Assert.Equal("P", row.VoterIdType);
        Assert.Equal(EntityState.Added, Context.Entry(row).State);
    }

    [Theory]
    [InlineData("E")]
    [InlineData("C")]
    [InlineData("T")]
    public async Task FindTrackedPhoneOnlineVoterAsync_NonPOccupancy_ReturnsNull(string existingType)
    {
        const string phone = "+14168972671";
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = existingType,
            SmsStatus = null
        });
        await Context.SaveChangesAsync();

        var row = await OnlineVoterPhoneHelper.FindTrackedPhoneOnlineVoterAsync(Context, phone);

        Assert.Null(row);
        var occupant = Assert.Single(await Context.OnlineVoters.Where(ov => ov.VoterId == phone).ToListAsync());
        Assert.Equal(existingType, occupant.VoterIdType);
        Assert.Null(occupant.SmsStatus);
    }

    [Theory]
    [InlineData("E")]
    [InlineData("C")]
    [InlineData("T")]
    public async Task FindPhoneOnlineVoterAsync_NonPOccupancy_ReturnsNull(string existingType)
    {
        const string phone = "+14168972671";
        Context.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = existingType,
            SmsStatus = "admin"
        });
        await Context.SaveChangesAsync();

        var row = await OnlineVoterPhoneHelper.FindPhoneOnlineVoterAsync(Context, phone);

        Assert.Null(row);
    }

    [Fact]
    public async Task FindPhoneOnlineVotersAsync_ReturnsOnlyPRowsKeyedByStoredPhone()
    {
        const string okPhone = "+14168972690";
        const string blockedPhone = "+14168972691";
        const string emailOccupant = "+14168972692";
        Context.OnlineVoters.AddRange(
            new OnlineVoter { VoterId = okPhone, VoterIdType = "P", SmsStatus = "OK" },
            new OnlineVoter { VoterId = blockedPhone, VoterIdType = "P", SmsStatus = "landline" },
            new OnlineVoter { VoterId = emailOccupant, VoterIdType = "E", SmsStatus = "admin" });
        await Context.SaveChangesAsync();

        var rows = await OnlineVoterPhoneHelper.FindPhoneOnlineVotersAsync(
            Context,
            [okPhone, blockedPhone, emailOccupant, "   ", null, okPhone]);

        Assert.Equal(2, rows.Count);
        Assert.Equal("OK", rows[okPhone].SmsStatus);
        Assert.Equal("landline", rows[blockedPhone].SmsStatus);
        Assert.False(rows.ContainsKey(emailOccupant));
    }

    [Fact]
    public void ToListHint_NoPhone_ReturnsNull()
    {
        Assert.Null(OnlineVoterPhoneHelper.ToListHint(null, phoneRow: null));
        Assert.Null(OnlineVoterPhoneHelper.ToListHint("  ", phoneRow: null));
    }

    [Fact]
    public void ToListHint_MissingOrNonPRow_IsNeverSeen()
    {
        var neverSeen = OnlineVoterPhoneHelper.ToListHint("+14168972693", phoneRow: null);
        Assert.NotNull(neverSeen);
        Assert.False(neverSeen.HasPhoneRow);
        Assert.Null(neverSeen.WhenRegistered);
        Assert.Null(neverSeen.SmsStatus);

        var occupant = new OnlineVoter
        {
            VoterId = "+14168972693",
            VoterIdType = "E",
            SmsStatus = "admin",
            WhenRegistered = DateTimeOffset.Parse("2026-01-01T00:00:00Z")
        };
        var ignored = OnlineVoterPhoneHelper.ToListHint(occupant.VoterId, occupant);
        Assert.NotNull(ignored);
        Assert.False(ignored.HasPhoneRow);
        Assert.Null(ignored.WhenRegistered);
        Assert.Null(ignored.SmsStatus);
    }

    [Fact]
    public void ToListHint_PRow_CopiesWhenRegisteredAndSmsStatus()
    {
        var registered = DateTimeOffset.Parse("2026-04-01T12:00:00Z");
        var row = new OnlineVoter
        {
            VoterId = "+14168972694",
            VoterIdType = "P",
            SmsStatus = "OK",
            WhenRegistered = registered
        };

        var hint = OnlineVoterPhoneHelper.ToListHint(row.VoterId, row);

        Assert.NotNull(hint);
        Assert.True(hint.HasPhoneRow);
        Assert.Equal(registered, hint.WhenRegistered);
        Assert.Equal("OK", hint.SmsStatus);
    }
}
