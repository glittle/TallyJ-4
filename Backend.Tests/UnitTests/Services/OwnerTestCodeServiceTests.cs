using Backend.Context;
using Backend.Entities;
using Backend.Identity;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MimeKit;

namespace Backend.Tests.UnitTests.Services;

/// <summary>
/// Owner test codes are limited per account.
/// </summary>
public class OwnerTestCodeServiceTests : ServiceTestBase
{
    private readonly MutableClock _clock = new();
    private readonly CapturingEmailSender _email = new();
    private readonly Guid _ownerId = Guid.NewGuid();

    public OwnerTestCodeServiceTests()
    {
        Context.Users.Add(new AppUser
        {
            Id = _ownerId.ToString(),
            UserName = "owner@example.com",
            NormalizedUserName = "OWNER@EXAMPLE.COM",
            Email = "owner@example.com",
            NormalizedEmail = "OWNER@EXAMPLE.COM",
            EmailConfirmed = true,
            AuthMethod = "Local",
            SecurityStamp = Guid.NewGuid().ToString()
        });
        Context.OwnerSendControls.Add(new OwnerSendControl
        {
            UserId = _ownerId,
            RestrictionStatus = OwnerRestrictionStatus.Restricted
        });
        Context.SaveChanges();
    }

    [Fact]
    public async Task SecondSendInsideAMinute_IsRefused_AndTheNextDayStartsAgain()
    {
        var service = CreateService();

        Assert.True((await service.SendAsync(_ownerId.ToString())).Success);
        Assert.Single(_email.Messages);

        var throttled = await service.SendAsync(_ownerId.ToString());
        Assert.False(throttled.Success);
        Assert.Equal(OwnerTestCodeService.ThrottledKey, throttled.ErrorKey);
        Assert.Single(_email.Messages);

        _clock.UtcNow = _clock.UtcNow.AddMinutes(1);
        for (var sent = 2; sent <= OwnerTestCodeService.DailyCap; sent++)
        {
            Assert.True((await service.SendAsync(_ownerId.ToString())).Success);
            _clock.UtcNow = _clock.UtcNow.AddMinutes(1);
        }

        Assert.Equal(OwnerTestCodeService.DailyCap, _email.Messages.Count);
        var daily = await service.SendAsync(_ownerId.ToString());
        Assert.False(daily.Success);
        Assert.Equal(OwnerTestCodeService.ThrottledKey, daily.ErrorKey);
        Assert.Equal(OwnerTestCodeService.DailyCap, _email.Messages.Count);

        _clock.UtcNow = _clock.UtcNow.AddDays(1);
        Assert.True((await service.SendAsync(_ownerId.ToString())).Success);
        Assert.Equal(OwnerTestCodeService.DailyCap + 1, _email.Messages.Count);
    }

    private OwnerTestCodeService CreateService()
    {
        return new OwnerTestCodeService(
            Context,
            _email,
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Email:FromAddress"] = "noreply@tallyj.com",
                ["Email:FromName"] = "TallyJ"
            }).Build(),
            _clock,
            NullLogger<OwnerTestCodeService>.Instance);
    }

    private sealed class MutableClock : ICodeSendClock
    {
        public DateTimeOffset UtcNow { get; set; } = DateTimeOffset.Parse("2026-11-01T12:00:00Z");

        public DateOnly UtcDate => DateOnly.FromDateTime(UtcNow.UtcDateTime);
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public List<MimeMessage> Messages { get; } = new();

        public Task SendAsync(MimeMessage message)
        {
            Messages.Add(message);
            return Task.CompletedTask;
        }
    }
}
