using Backend.DTOs.Auth;
using Backend.Entities;
using Backend.Identity;
using Backend.Services.Auth;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Backend.Tests.UnitTests;

public class AccountInviteServiceTests : ServiceTestBase
{
    private readonly AccountInviteService _service;

    public AccountInviteServiceTests()
    {
        var localAuth = new Mock<ILocalAuthService>();
        var userManager = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ClientEnv:frontendUrl"] = "https://localhost:8095"
            })
            .Build();
        var env = new Mock<IHostEnvironment>();
        env.Setup(e => e.EnvironmentName).Returns(Environments.Development);
        var logger = new Mock<ILogger<AccountInviteService>>();

        _service = new AccountInviteService(
            Context,
            localAuth.Object,
            userManager.Object,
            config,
            env.Object,
            logger.Object);
    }

    [Fact]
    public async Task CreateAsync_ReturnsRawTokenOnce_AndStoresOnlyHash()
    {
        var created = await _service.CreateAsync("admin-user-id");

        created.Token.Should().NotBeNullOrWhiteSpace();
        created.InviteUrl.Should().StartWith("https://localhost:8095/register?invite=");
        created.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddDays(6));

        var stored = Context.AccountInvites.Single();
        stored.TokenHash.Should().Be(AccountInviteService.HashToken(created.Token));
        stored.CreatedByUserId.Should().Be("admin-user-id");
        stored.UsedAt.Should().BeNull();
        Context.AccountInvites.Any(i => i.TokenHash == created.Token).Should().BeFalse();
    }

    [Fact]
    public async Task PeekAsync_IsFalse_ForMissingUsedAndExpired()
    {
        var created = await _service.CreateAsync("admin-user-id");
        (await _service.PeekAsync(created.Token)).Valid.Should().BeTrue();
        (await _service.PeekAsync("no-such-token")).Valid.Should().BeFalse();
        (await _service.PeekAsync(null)).Valid.Should().BeFalse();

        var stored = Context.AccountInvites.Single();
        stored.UsedAt = DateTimeOffset.UtcNow;
        await Context.SaveChangesAsync();
        (await _service.PeekAsync(created.Token)).Valid.Should().BeFalse();

        stored.UsedAt = null;
        stored.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1);
        await Context.SaveChangesAsync();
        (await _service.PeekAsync(created.Token)).Valid.Should().BeFalse();
    }
}
