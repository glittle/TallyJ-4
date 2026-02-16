using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Security.Claims;
using Backend.Authorization;

namespace Backend.Tests.UnitTests;

public class SuperAdminHandlerTests
{
    private SuperAdminHandler CreateHandler(string[] emails)
    {
        var settings = Options.Create(new SuperAdminSettings { Emails = emails });
        var logger = new Mock<ILogger<SuperAdminHandler>>();
        return new SuperAdminHandler(settings, logger.Object);
    }

    private AuthorizationHandlerContext CreateContext(ClaimsPrincipal? user)
    {
        var requirement = new SuperAdminRequirement();
        return new AuthorizationHandlerContext(
            new[] { requirement },
            user ?? new ClaimsPrincipal(),
            null);
    }

    [Fact]
    public async Task HandleAsync_MatchingEmail_Succeeds()
    {
        var handler = CreateHandler(new[] { "admin@tallyj.test" });
        var claims = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("email", "admin@tallyj.test") }, "Test"));
        var context = CreateContext(claims);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_CaseInsensitiveEmail_Succeeds()
    {
        var handler = CreateHandler(new[] { "Admin@TallyJ.Test" });
        var claims = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("email", "admin@tallyj.test") }, "Test"));
        var context = CreateContext(claims);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_NonMatchingEmail_Fails()
    {
        var handler = CreateHandler(new[] { "admin@tallyj.test" });
        var claims = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("email", "other@tallyj.test") }, "Test"));
        var context = CreateContext(claims);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task HandleAsync_NoEmailClaim_Fails()
    {
        var handler = CreateHandler(new[] { "admin@tallyj.test" });
        var claims = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("sub", "some-id") }, "Test"));
        var context = CreateContext(claims);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task HandleAsync_UnauthenticatedUser_Fails()
    {
        var handler = CreateHandler(new[] { "admin@tallyj.test" });
        var claims = new ClaimsPrincipal(new ClaimsIdentity());
        var context = CreateContext(claims);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task HandleAsync_EmptyEmailList_Fails()
    {
        var handler = CreateHandler(Array.Empty<string>());
        var claims = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim("email", "admin@tallyj.test") }, "Test"));
        var context = CreateContext(claims);

        await handler.HandleAsync(context);

        Assert.True(context.HasFailed);
    }

    [Fact]
    public async Task HandleAsync_ClaimTypesEmail_Succeeds()
    {
        var handler = CreateHandler(new[] { "admin@tallyj.test" });
        var claims = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.Email, "admin@tallyj.test") }, "Test"));
        var context = CreateContext(claims);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }
}



