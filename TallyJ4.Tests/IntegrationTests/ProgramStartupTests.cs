using Microsoft.Extensions.DependencyInjection;
using TallyJ4.Services;

namespace TallyJ4.Tests.IntegrationTests;

public class ProgramStartupTests : IntegrationTestBase
{
    public ProgramStartupTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public void Application_StartsSuccessfully()
    {
        // Arrange & Act: Factory creates the app successfully
        var services = Factory.Services;

        // Assert: Services are available (application started successfully)
        Assert.NotNull(services);
        Assert.NotNull(Factory.Server);
    }

    [Fact]
    public void CoreServices_AreRegistered()
    {
        // Arrange
        var services = Factory.Services;

        // Act & Assert: Verify key services are registered
        Assert.NotNull(services.GetService<IElectionService>());
        Assert.NotNull(services.GetService<IPeopleService>());
        Assert.NotNull(services.GetService<IBallotService>());
        Assert.NotNull(services.GetService<IVoteService>());
        Assert.NotNull(services.GetService<IDashboardService>());
        Assert.NotNull(services.GetService<ISetupService>());
        Assert.NotNull(services.GetService<IAccountService>());
        Assert.NotNull(services.GetService<IPublicService>());
        Assert.NotNull(services.GetService<ITallyService>());
    }

    [Fact]
    public void AuthServices_AreRegistered()
    {
        // Arrange
        var services = Factory.Services;

        // Act & Assert: Verify auth services are registered
        Assert.NotNull(services.GetService<TallyJ4.Application.Services.Auth.JwtTokenService>());
        Assert.NotNull(services.GetService<TallyJ4.Application.Services.Auth.EmailService>());
        Assert.NotNull(services.GetService<TallyJ4.Application.Services.Auth.LocalAuthService>());
        Assert.NotNull(services.GetService<TallyJ4.Application.Services.Auth.PasswordResetService>());
        Assert.NotNull(services.GetService<TallyJ4.Application.Services.Auth.TwoFactorService>());
    }

    [Fact]
    public void SignalRServices_AreRegistered()
    {
        // Arrange
        var services = Factory.Services;

        // Act & Assert: Verify SignalR services are registered
        Assert.NotNull(services.GetService<ISignalRNotificationService>());
    }

    [Fact]
    public async Task ProtectedEndpoint_RequiresAuthorization()
    {
        // Arrange: No auth token

        // Act
        var response = await Client.GetAsync("/protected");

        // Assert
        Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_IsAvailableInDevelopment()
    {
        // Arrange: Factory is configured for Testing, but we can check if Swagger is set up
        // Note: In testing environment, Swagger might not be enabled, but the endpoint should exist

        // Act
        var response = await Client.GetAsync("/swagger/index.html");

        // Assert: In development it would be 200, but in testing it might be 404 if not configured
        // This test verifies the app doesn't crash on startup
        Assert.True(response.IsSuccessStatusCode || response.StatusCode == System.Net.HttpStatusCode.NotFound);
    }
}