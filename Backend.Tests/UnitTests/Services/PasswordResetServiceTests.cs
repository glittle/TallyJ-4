using Backend.Context;
using Backend.Localization;
using Microsoft.Data.Sqlite;
using Backend.DTOs.Auth;
using Backend.Identity;
using Backend.Services.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace Backend.Tests.UnitTests.Services;

public class PasswordResetServiceTests
{
    private const string Email = "officer@example.com";
    private const string Token = "reset-token+secret/==";

    [Fact]
    public async Task Generate_UnknownAddress_IsSuccessWithoutEmail()
    {
        var (service, users, email, logs) = Create();
        users.Setup(manager => manager.FindByEmailAsync(Email)).ReturnsAsync((AppUser?)null);

        var (success, error) = await service.GenerateResetTokenAsync(new ForgotPasswordRequest { Email = Email });

        Assert.True(success);
        Assert.Null(error);
        email.Verify(
            sender => sender.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        AssertNoSecrets(logs);
    }

    [Fact]
    public async Task Generate_OAuthAccount_IsSuccessWithoutEmail()
    {
        var (service, users, email, logs) = Create();
        var user = new AppUser { Email = Email, AuthMethod = "Google" };
        users.Setup(manager => manager.FindByEmailAsync(Email)).ReturnsAsync(user);

        var (success, _) = await service.GenerateResetTokenAsync(new ForgotPasswordRequest { Email = Email });

        Assert.True(success);
        users.Verify(manager => manager.GeneratePasswordResetTokenAsync(It.IsAny<AppUser>()), Times.Never);
        email.Verify(
            sender => sender.SendPasswordResetEmailAsync(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never);
        AssertNoSecrets(logs);
    }

    [Fact]
    public async Task Generate_LocalAccount_SendsIdentityToken_AndHidesSendFailure()
    {
        var (service, users, email, logs) = Create();
        var user = new AppUser { Id = "user-1", Email = Email, AuthMethod = "Local,Google", PasswordResetToken = "old-plaintext" };
        users.Setup(manager => manager.FindByEmailAsync(Email)).ReturnsAsync(user);
        users.Setup(manager => manager.HasPasswordAsync(user)).ReturnsAsync(true);
        users.Setup(manager => manager.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);
        users.Setup(manager => manager.GeneratePasswordResetTokenAsync(user)).ReturnsAsync(Token);
        email.Setup(sender => sender.SendPasswordResetEmailAsync(Email, Token))
            .ThrowsAsync(new InvalidOperationException($"failed for {Email} {Token}"));

        var (success, error) = await service.GenerateResetTokenAsync(new ForgotPasswordRequest { Email = Email });

        Assert.True(success);
        Assert.Null(error);
        Assert.Null(user.PasswordResetToken);
        AssertNoSecrets(logs);
    }

    [Fact]
    public async Task Reset_InvalidToken_DoesNotReturnProviderText()
    {
        var (service, users, _, logs) = Create();
        var user = new AppUser { Id = "user-1", Email = Email, AuthMethod = "Local" };
        users.Setup(manager => manager.FindByEmailAsync(Email)).ReturnsAsync(user);
        users.Setup(manager => manager.ResetPasswordAsync(user, Token, "NewPassword123!"))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "InvalidToken",
                Description = $"Invalid token {Token} for {Email}"
            }));

        var (success, error) = await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = Email,
            Token = Token,
            NewPassword = "NewPassword123!",
            ConfirmPassword = "NewPassword123!"
        });

        Assert.False(success);
        Assert.Equal(PasswordResetService.InvalidTokenKey, error);
        AssertNoSecrets(logs);
    }

    [Fact]
    public async Task Reset_WeakPassword_ReturnsPolicyKeyWithoutThePassword()
    {
        const string password = "Short1234!";
        var (service, users, _, logs) = Create();
        var user = new AppUser { Id = "user-1", Email = Email, AuthMethod = "Local" };
        users.Setup(manager => manager.FindByEmailAsync(Email)).ReturnsAsync(user);
        users.Setup(manager => manager.ResetPasswordAsync(user, Token, password))
            .ReturnsAsync(IdentityResult.Failed(new IdentityError
            {
                Code = "PasswordTooShort",
                Description = $"Passwords must be at least 12 characters. Saw {password}"
            }));

        var (success, error) = await service.ResetPasswordAsync(new ResetPasswordRequest
        {
            Email = Email,
            Token = Token,
            NewPassword = password,
            ConfirmPassword = password
        });

        Assert.False(success);
        Assert.Equal("auth.passwordMinLength12", error);
        Assert.DoesNotContain(password, error);
        AssertNoSecrets(logs);
    }

    private static void AssertNoSecrets(ListLogger<PasswordResetService> logs)
    {
        var written = string.Join('\n', logs.Lines);
        Assert.DoesNotContain(Email, written);
        Assert.DoesNotContain(Token, written);
    }

    private static (PasswordResetService Service, Mock<UserManager<AppUser>> Users, Mock<EmailService> Email, ListLogger<PasswordResetService> Logs) Create()
    {
        var users = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        var configuration = new ConfigurationBuilder().AddInMemoryCollection().Build();
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(host => host.EnvironmentName).Returns(Environments.Development);
        var email = new Mock<EmailService>(
            configuration,
            environment.Object,
            Mock.Of<ILogger<EmailService>>(),
            Mock.Of<IEmailSender>(),
            Mock.Of<IJsonLocalizationProvider>());
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<MainDbContext>().UseSqlite(connection).Options;
        var db = new MainDbContext(options);
        db.Database.EnsureCreated();
        var logs = new ListLogger<PasswordResetService>();
        var service = new PasswordResetService(users.Object, email.Object, db, logs);
        return (service, users, email, logs);
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Lines { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Lines.Add(formatter(state, exception));
            if (exception != null)
            {
                Lines.Add(exception.ToString());
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
