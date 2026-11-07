using Backend.Context;
using Backend.Entities;
using Backend.Services;
using Microsoft.EntityFrameworkCore;

namespace Backend.Tests.UnitTests.Services;

public class BulkSendSupportTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<MainDbContext> _options;

    public BulkSendSupportTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"bulk-upsert-{Guid.NewGuid():N}.db");
        _options = new DbContextOptionsBuilder<MainDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        using var db = new MainDbContext(_options);
        db.Database.EnsureCreated();
    }

    [Fact]
    public async Task Upsert_UniqueCollision_UpdatesTheWinningRow_InsteadOfThrowing()
    {
        var personId = Guid.NewGuid();
        var electionId = Guid.NewGuid();
        await using var db = new MainDbContext(_options);
        BulkSendSupport.BeforeSaveForTests = async _ =>
        {
            await using var other = new MainDbContext(_options);
            other.PersonLastAnnouncements.Add(new PersonLastAnnouncement
            {
                PersonGuid = personId,
                ElectionGuid = electionId,
                Channel = "sms",
                Outcome = "sent",
                AttemptedAt = DateTimeOffset.UtcNow
            });
            await other.SaveChangesAsync();
        };

        try
        {
            await BulkSendSupport.UpsertLastAsync(
                db, electionId, personId, "email", "failed", "send-failed", CancellationToken.None);
        }
        finally
        {
            BulkSendSupport.BeforeSaveForTests = null;
        }

        await using var check = new MainDbContext(_options);
        var row = await check.PersonLastAnnouncements.SingleAsync(item => item.PersonGuid == personId);
        Assert.Equal("email", row.Channel);
        Assert.Equal("failed", row.Outcome);
        Assert.Equal("send-failed", row.Error);
    }

    public void Dispose()
    {
        BulkSendSupport.BeforeSaveForTests = null;
        try
        {
            File.Delete(_dbPath);
        }
        catch (IOException)
        {
        }
    }
}
