using Backend.Entities;
using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class SmsLogPhoneHelperTests : ServiceTestBase
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task FindRecentForPhoneAsync_NoPhone_Empty(string? phone)
    {
        Context.SmsLogs.Add(SeedLog("+14168972671", "SM-keep"));
        await Context.SaveChangesAsync();

        var rows = await SmsLogPhoneHelper.FindRecentForPhoneAsync(Context, phone);

        Assert.Empty(rows);
    }

    [Fact]
    public async Task FindRecentForPhoneAsync_ExactPhone_ReturnsNewestFirst()
    {
        const string phone = "+14168972690";
        Context.SmsLogs.AddRange(
            SeedLog(phone, "SM-old", sent: DateTimeOffset.Parse("2026-04-01T12:00:00Z")),
            SeedLog(phone, "SM-new", sent: DateTimeOffset.Parse("2026-04-03T12:00:00Z")),
            SeedLog("+14165550100", "SM-other", sent: DateTimeOffset.Parse("2026-04-04T12:00:00Z")));
        await Context.SaveChangesAsync();

        var rows = await SmsLogPhoneHelper.FindRecentForPhoneAsync(Context, phone);

        Assert.Equal(["SM-new", "SM-old"], rows.Select(r => r.SmsSid));
    }

    [Theory]
    [InlineData("+14168972691", "14168972691")]
    [InlineData("14168972691", "+14168972691")]
    public async Task FindRecentForPhoneAsync_PlusMinusVariant_Matches(
        string storedPhone,
        string lookupPhone)
    {
        Context.SmsLogs.Add(SeedLog(storedPhone, "SM-variant"));
        await Context.SaveChangesAsync();

        var rows = await SmsLogPhoneHelper.FindRecentForPhoneAsync(Context, lookupPhone);

        var row = Assert.Single(rows);
        Assert.Equal("SM-variant", row.SmsSid);
    }

    [Fact]
    public async Task FindRecentForPhoneAsync_MoreThanLimit_TakesNewestFive()
    {
        const string phone = "+14168972692";
        for (var i = 1; i <= 7; i++)
        {
            Context.SmsLogs.Add(SeedLog(
                phone,
                $"SM-{i}",
                sent: DateTimeOffset.Parse("2026-04-01T00:00:00Z").AddHours(i)));
        }
        await Context.SaveChangesAsync();

        var rows = await SmsLogPhoneHelper.FindRecentForPhoneAsync(Context, phone);

        Assert.Equal(SmsLogPhoneHelper.RecentLimit, rows.Count);
        Assert.Equal(
            ["SM-7", "SM-6", "SM-5", "SM-4", "SM-3"],
            rows.Select(r => r.SmsSid));
    }

    [Fact]
    public async Task FindRecentForPhoneAsync_DoesNotUsePersonGuidOrElection()
    {
        const string phone = "+14168972693";
        var otherElection = Guid.NewGuid();
        var otherPerson = Guid.NewGuid();
        Context.SmsLogs.Add(SeedLog(
            phone,
            "SM-global",
            electionGuid: otherElection,
            personGuid: otherPerson));
        await Context.SaveChangesAsync();

        var rows = await SmsLogPhoneHelper.FindRecentForPhoneAsync(Context, phone);

        var row = Assert.Single(rows);
        Assert.Equal("SM-global", row.SmsSid);
        Assert.Equal(otherElection, row.ElectionGuid);
        Assert.Equal(otherPerson, row.PersonGuid);
    }

    private static SmsLog SeedLog(
        string phone,
        string sid,
        DateTimeOffset? sent = null,
        Guid? electionGuid = null,
        Guid? personGuid = null) =>
        new()
        {
            SmsSid = sid,
            Phone = phone,
            SentDate = sent ?? DateTimeOffset.Parse("2026-04-01T12:00:00Z"),
            LastStatus = "delivered",
            LastDate = DateTimeOffset.Parse("2026-04-01T12:01:00Z"),
            ErrorCode = null,
            ElectionGuid = electionGuid,
            PersonGuid = personGuid
        };
}
