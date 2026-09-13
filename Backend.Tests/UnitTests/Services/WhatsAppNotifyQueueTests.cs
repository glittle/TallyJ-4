using Backend.Context;
using Backend.DTOs.People;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Head-teller WhatsApp notify queue. Tests mock the GreenAPI sender — no live account.
/// </summary>
public class WhatsAppNotifyQueueTests : IDisposable
{
    private readonly DbContextOptions<MainDbContext> _options = new DbContextOptionsBuilder<MainDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
        .Options;

    public void Dispose()
    {
        using var db = new TestMainDbContext(_options);
        db.Database.EnsureDeleted();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task Start_OkNumbers_SendAndWriteSmsLog()
    {
        var electionGuid = Guid.NewGuid();
        var ok = await SeedPersonAsync(electionGuid, "+14168972901", whatsAppStatus: "OK", smsStatus: "landline");
        var client = MockSender();
        var delays = 0;
        var queue = CreateQueue(client, _ =>
        {
            delays++;
            return Task.CompletedTask;
        });

        var started = await queue.StartAsync(electionGuid, [ok.PersonGuid]);
        var status = await WaitUntilIdle(queue, electionGuid);

        Assert.Equal(1, started.Queued);
        Assert.Equal(0, started.Skipped);
        Assert.False(status.Running);
        Assert.Equal(1, status.Sent);
        Assert.Equal(0, status.Failed);
        Assert.Equal(WhatsAppNotifyOutcome.Sent, status.Results[0].Outcome);
        Assert.Equal(0, delays);
        client.Verify(
            c => c.SendMessageAsync(ok.Phone!, It.Is<string>(m => m.Contains("Hello")), It.IsAny<CancellationToken>()),
            Times.Once);

        await using var db = new TestMainDbContext(_options);
        var log = await db.SmsLogs.SingleAsync();
        Assert.Equal(ok.Phone, log.Phone);
        Assert.Equal(electionGuid, log.ElectionGuid);
        Assert.Equal(ok.PersonGuid, log.PersonGuid);
        Assert.Equal(SmsLogSendHelper.DefaultLastStatus, log.LastStatus);
        var row = await db.OnlineVoters.SingleAsync(ov => ov.VoterId == ok.Phone);
        Assert.Equal("landline", row.SmsStatus);
        Assert.Equal("OK", row.WhatsAppStatus);
    }

    [Theory]
    [InlineData(null, WhatsAppNotifyOutcome.SkippedUnchecked)]
    [InlineData("no-wa", WhatsAppNotifyOutcome.SkippedNoWa)]
    [InlineData("check-failed", WhatsAppNotifyOutcome.SkippedCheckFailed)]
    [InlineData("blocked", WhatsAppNotifyOutcome.SkippedNotOk)]
    public async Task Start_NonOkWhatsAppStatus_SkippedWithoutSend(string? status, string expected)
    {
        var electionGuid = Guid.NewGuid();
        var person = await SeedPersonAsync(electionGuid, "+14168972902", whatsAppStatus: status, smsStatus: "OK");
        var client = new Mock<IGreenApiWhatsAppClient>(MockBehavior.Strict);
        client.Setup(c => c.IsConfigured()).Returns(true);
        var queue = CreateQueue(client);

        var result = await queue.StartAsync(electionGuid, [person.PersonGuid]);
        var done = await WaitUntilIdle(queue, electionGuid);

        Assert.Equal(0, result.Queued);
        Assert.Equal(1, result.Skipped);
        Assert.Equal(expected, done.Results[0].Outcome);
        client.Verify(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("E")]
    [InlineData("C")]
    [InlineData("T")]
    public async Task Start_NonPOccupant_SkippedNotConverted(string existingType)
    {
        var electionGuid = Guid.NewGuid();
        const string phone = "+14168972903";
        await SeedElectionAsync(electionGuid);
        await using (var db = new TestMainDbContext(_options))
        {
            db.People.Add(Person(electionGuid, phone));
            db.OnlineVoters.Add(new OnlineVoter
            {
                VoterId = phone,
                VoterIdType = existingType,
                WhatsAppStatus = "OK",
                SmsStatus = "OK"
            });
            await db.SaveChangesAsync();
        }

        var client = new Mock<IGreenApiWhatsAppClient>(MockBehavior.Strict);
        client.Setup(c => c.IsConfigured()).Returns(true);
        var queue = CreateQueue(client);

        var result = await queue.StartAsync(electionGuid, [await FirstPersonGuidAsync(phone)]);

        Assert.Equal(1, result.Skipped);
        Assert.Equal(WhatsAppNotifyOutcome.SkippedNonP, result.Results[0].Outcome);
        client.Verify(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        await using var check = new TestMainDbContext(_options);
        var occupant = await check.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal(existingType, occupant.VoterIdType);
        Assert.Equal("OK", occupant.WhatsAppStatus);
    }

    [Fact]
    public async Task Start_OtherElectionPerson_Ignored()
    {
        var electionGuid = Guid.NewGuid();
        var inElection = await SeedPersonAsync(electionGuid, "+14168972904", whatsAppStatus: "OK");
        var other = await SeedPersonAsync(Guid.NewGuid(), "+14168972905", whatsAppStatus: "OK");
        var client = MockSender();
        var queue = CreateQueue(client);

        var started = await queue.StartAsync(electionGuid, [inElection.PersonGuid, other.PersonGuid]);
        var done = await WaitUntilIdle(queue, electionGuid);

        Assert.Equal(1, started.Queued);
        Assert.Equal(1, started.Skipped);
        Assert.Equal(WhatsAppNotifyOutcome.SkippedOtherElection, started.Results[1].Outcome);
        Assert.Equal(1, done.Sent);
        client.Verify(c => c.SendMessageAsync(inElection.Phone!, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.SendMessageAsync(other.Phone!, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Start_OverMax_DoesNotCallProvider()
    {
        var tooMany = Enumerable.Range(0, StartWhatsAppNotifyDto.MaxSelectedPeople + 1)
            .Select(_ => Guid.NewGuid())
            .ToList();
        var client = new Mock<IGreenApiWhatsAppClient>(MockBehavior.Strict);
        var queue = CreateQueue(client);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            queue.StartAsync(Guid.NewGuid(), tooMany));

        Assert.Equal(PeopleMessageKeys.PhoneWhatsAppTooMany, ex.Message);
    }

    [Fact]
    public async Task Abort_StopsRemaining_AlreadySentStaySent()
    {
        var electionGuid = Guid.NewGuid();
        var first = await SeedPersonAsync(electionGuid, "+14168972906", whatsAppStatus: "OK");
        var second = await SeedPersonAsync(electionGuid, "+14168972907", whatsAppStatus: "OK");
        var firstStarted = new TaskCompletionSource();
        var firstContinue = new TaskCompletionSource();
        var client = new Mock<IGreenApiWhatsAppClient>();
        client.Setup(c => c.IsConfigured()).Returns(true);
        client
            .Setup(c => c.SendMessageAsync(first.Phone!, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(async (string _, string _, CancellationToken _) =>
            {
                firstStarted.TrySetResult();
                await firstContinue.Task;
                return GreenApiWhatsAppSendResult.Succeeded("id-first");
            });
        client
            .Setup(c => c.SendMessageAsync(second.Phone!, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(GreenApiWhatsAppSendResult.Succeeded("id-second"));
        var queue = CreateQueue(client);

        var started = await queue.StartAsync(electionGuid, [first.PersonGuid, second.PersonGuid]);
        await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(2));
        var aborted = queue.Abort(electionGuid, started.QueueToken);
        Assert.NotNull(aborted);
        firstContinue.TrySetResult();
        var done = await WaitUntilIdle(queue, electionGuid);

        Assert.True(done.Cancelled);
        Assert.Equal(1, done.Sent);
        Assert.Equal(WhatsAppNotifyOutcome.Sent, done.Results[0].Outcome);
        Assert.Equal(WhatsAppNotifyOutcome.Cancelled, done.Results[1].Outcome);
        client.Verify(c => c.SendMessageAsync(first.Phone!, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        client.Verify(c => c.SendMessageAsync(second.Phone!, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);

        await using var db = new TestMainDbContext(_options);
        Assert.Equal(1, await db.SmsLogs.CountAsync());
        Assert.Equal(first.Phone, (await db.SmsLogs.SingleAsync()).Phone);
    }

    [Fact]
    public async Task Start_UsesDelayBetweenSends()
    {
        var electionGuid = Guid.NewGuid();
        var first = await SeedPersonAsync(electionGuid, "+14168972908", whatsAppStatus: "OK");
        var second = await SeedPersonAsync(electionGuid, "+14168972909", whatsAppStatus: "OK");
        var delays = 0;
        var client = MockSender();
        var queue = CreateQueue(client, _ =>
        {
            delays++;
            return Task.CompletedTask;
        });

        await queue.StartAsync(electionGuid, [first.PersonGuid, second.PersonGuid]);
        await WaitUntilIdle(queue, electionGuid);

        Assert.Equal(1, delays);
        client.Verify(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Start_NoPhone_Skipped()
    {
        var electionGuid = Guid.NewGuid();
        await SeedElectionAsync(electionGuid);
        var personGuid = Guid.NewGuid();
        await using (var db = new TestMainDbContext(_options))
        {
            db.People.Add(new Person
            {
                PersonGuid = personGuid,
                ElectionGuid = electionGuid,
                LastName = "Smith",
                FirstName = "Pat",
                Phone = null,
                RowVersion = new byte[8]
            });
            await db.SaveChangesAsync();
        }

        var client = new Mock<IGreenApiWhatsAppClient>(MockBehavior.Strict);
        client.Setup(c => c.IsConfigured()).Returns(true);
        var queue = CreateQueue(client);

        var result = await queue.StartAsync(electionGuid, [personGuid]);

        Assert.Equal(WhatsAppNotifyOutcome.SkippedNoPhone, result.Results[0].Outcome);
    }

    [Fact]
    public async Task Start_NotConfigured_DoesNotSend()
    {
        var electionGuid = Guid.NewGuid();
        var person = await SeedPersonAsync(electionGuid, "+14168972910", whatsAppStatus: "OK");
        var client = new Mock<IGreenApiWhatsAppClient>(MockBehavior.Strict);
        client.Setup(c => c.IsConfigured()).Returns(false);
        var queue = CreateQueue(client);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            queue.StartAsync(electionGuid, [person.PersonGuid]));

        Assert.Equal(PeopleMessageKeys.PhoneWhatsAppNotConfigured, ex.Message);
    }

    [Fact]
    public async Task Start_MissingSmsText_DoesNotSend()
    {
        var electionGuid = Guid.NewGuid();
        await SeedElectionAsync(electionGuid, smsText: "  ");
        var person = await SeedPersonAsync(electionGuid, "+14168972911", whatsAppStatus: "OK", seedElection: false);
        var client = new Mock<IGreenApiWhatsAppClient>(MockBehavior.Strict);
        client.Setup(c => c.IsConfigured()).Returns(true);
        var queue = CreateQueue(client);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            queue.StartAsync(electionGuid, [person.PersonGuid]));

        Assert.Equal(PeopleMessageKeys.WhatsAppNotifyTextNotSet, ex.Message);
    }

    private WhatsAppNotifyQueue CreateQueue(
        Mock<IGreenApiWhatsAppClient> client,
        Func<CancellationToken, Task>? delay = null)
    {
        var services = new ServiceCollection();
        services.AddScoped<MainDbContext>(_ => new TestMainDbContext(_options));
        services.AddSingleton<IGreenApiWhatsAppClient>(client.Object);
        var provider = services.BuildServiceProvider();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ClientEnv:frontendUrl"] = "https://example.test"
            })
            .Build();

        return new WhatsAppNotifyQueue(
            provider.GetRequiredService<IServiceScopeFactory>(),
            configuration,
            Mock.Of<ILogger<WhatsAppNotifyQueue>>(),
            delay ?? (_ => Task.CompletedTask));
    }

    private static Mock<IGreenApiWhatsAppClient> MockSender()
    {
        var client = new Mock<IGreenApiWhatsAppClient>();
        client.Setup(c => c.IsConfigured()).Returns(true);
        client
            .Setup(c => c.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string phone, string _, CancellationToken _) =>
                GreenApiWhatsAppSendResult.Succeeded("id-" + phone[^4..]));
        return client;
    }

    private static async Task<WhatsAppNotifyStatusDto> WaitUntilIdle(
        IWhatsAppNotifyQueue queue,
        Guid electionGuid)
    {
        for (var i = 0; i < 200; i++)
        {
            var status = queue.GetStatus(electionGuid);
            if (status is not { Running: true })
            {
                return status ?? throw new InvalidOperationException("missing notify status");
            }

            await Task.Delay(10);
        }

        throw new TimeoutException("WhatsApp notify queue still running");
    }

    private async Task<Person> SeedPersonAsync(
        Guid electionGuid,
        string phone,
        string? whatsAppStatus,
        string? smsStatus = null,
        bool seedElection = true)
    {
        if (seedElection)
        {
            await SeedElectionAsync(electionGuid);
        }

        var person = Person(electionGuid, phone);
        await using var db = new TestMainDbContext(_options);
        if (!await db.Elections.AnyAsync(e => e.ElectionGuid == electionGuid))
        {
            await SeedElectionAsync(electionGuid);
        }

        db.People.Add(person);
        db.OnlineVoters.Add(new OnlineVoter
        {
            VoterId = phone,
            VoterIdType = "P",
            WhatsAppStatus = whatsAppStatus,
            SmsStatus = smsStatus
        });
        await db.SaveChangesAsync();
        return person;
    }

    private async Task SeedElectionAsync(Guid electionGuid, string smsText = "Hello {FirstName}")
    {
        await using var db = new TestMainDbContext(_options);
        if (await db.Elections.AnyAsync(e => e.ElectionGuid == electionGuid))
        {
            return;
        }

        db.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = "Notify test",
            NumberToElect = 3,
            ElectionType = "LSA",
            SmsText = smsText,
            RowVersion = new byte[8]
        });
        await db.SaveChangesAsync();
    }

    private async Task<Guid> FirstPersonGuidAsync(string phone)
    {
        await using var db = new TestMainDbContext(_options);
        return await db.People.Where(p => p.Phone == phone).Select(p => p.PersonGuid).SingleAsync();
    }

    private static Person Person(Guid electionGuid, string phone) => new()
    {
        PersonGuid = Guid.NewGuid(),
        ElectionGuid = electionGuid,
        LastName = "Smith",
        FirstName = "Pat",
        Phone = phone,
        RowVersion = new byte[8]
    };
}
