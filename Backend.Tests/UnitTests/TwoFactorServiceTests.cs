using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OtpNet;
using Backend.DTOs.Auth;
using Backend.Services.Auth;
using Backend.Entities;
using Backend.Identity;
using Backend.Services;

namespace Backend.Tests.UnitTests;

public class TwoFactorServiceTests : ServiceTestBase
{
    private readonly TwoFactorService _service;
    private readonly UserManager<AppUser> _userManager;
    private readonly Mock<IStringLocalizer<TwoFactorService>> _localizerMock;
    private readonly Mock<EmailService> _emailServiceMock;
    private readonly EncryptionService _encryptionService;

    public TwoFactorServiceTests()
    {
        var userStore = new UserStore<AppUser>(Context);
        _userManager = new UserManager<AppUser>(
            userStore,
            Options.Create(new IdentityOptions
            {
                Password = new PasswordOptions
                {
                    RequireDigit = false,
                    RequiredLength = 1,
                    RequireNonAlphanumeric = false,
                    RequireUppercase = false,
                    RequireLowercase = false,
                }
            }),
            new PasswordHasher<AppUser>(),
            Array.Empty<IUserValidator<AppUser>>(),
            Array.Empty<IPasswordValidator<AppUser>>(),
            new UpperInvariantLookupNormalizer(),
            new IdentityErrorDescriber(),
            null!,
            NullLogger<UserManager<AppUser>>.Instance);

        _localizerMock = new Mock<IStringLocalizer<TwoFactorService>>();
        // Return the key as the value so unconfigured keys still surface a readable error.
        _localizerMock.Setup(x => x[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key, resourceNotFound: false));

        var emailConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var emailSenderMock = new Mock<IEmailSender>();
        _emailServiceMock = new Mock<EmailService>(emailConfig, NullLogger<EmailService>.Instance, emailSenderMock.Object);

        var config = new Dictionary<string, string?>
        {
            ["Encryption:Key"] = "ThisIsATestEncryptionKeyThatIsLongEnoughForAES256"
        };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();
        _encryptionService = new EncryptionService(configuration);

        _service = new TwoFactorService(
            _userManager,
            _localizerMock.Object,
            Context,
            _emailServiceMock.Object,
            _encryptionService,
            NullLogger<TwoFactorService>.Instance);
    }

    private async Task<AppUser> CreateUserAsync(string id, string email = "test@example.com",
        bool twoFactorEnabled = false, string authMethod = "Local")
    {
        var user = new AppUser
        {
            Id = id,
            UserName = email,
            Email = email,
            TwoFactorEnabled = twoFactorEnabled,
            AuthMethod = authMethod
        };

        IdentityResult result;
        if (string.Equals(authMethod, "Local", StringComparison.OrdinalIgnoreCase)
            || authMethod.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Any(m => string.Equals(m, "Local", StringComparison.OrdinalIgnoreCase)))
        {
            result = await _userManager.CreateAsync(user, "password123");
        }
        else
        {
            // OAuth-only accounts typically have no password.
            result = await _userManager.CreateAsync(user);
        }

        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(e => e.Description)));
        return user;
    }

    [Fact]
    public async Task SetupAsync_UserNotFound_ReturnsError()
    {
        _localizerMock.Setup(x => x["auth.errors.userNotFound"])
            .Returns(new LocalizedString("auth.errors.userNotFound", "User not found"));

        var result = await _service.SetupAsync("nonexistent");

        Assert.False(result.Success);
        Assert.Contains("User not found", result.Error);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task SetupAsync_UserAlreadyHas2FA_ReturnsError()
    {
        await CreateUserAsync("user1", twoFactorEnabled: true);

        _localizerMock.Setup(x => x["auth.errors.twoFactorAlreadyEnabled"])
            .Returns(new LocalizedString("auth.errors.twoFactorAlreadyEnabled", "2FA already enabled"));

        var result = await _service.SetupAsync("user1");

        Assert.False(result.Success);
        Assert.Contains("2FA already enabled", result.Error);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task SetupAsync_ValidUser_CreatesTokenAndReturnsResponse()
    {
        await CreateUserAsync("user1");

        var result = await _service.SetupAsync("user1");

        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.NotNull(result.Response);
        Assert.NotNull(result.Response.Secret);
        Assert.NotNull(result.Response.QrCodeDataUrl);

        var token = await Context.Set<TwoFactorToken>().FirstOrDefaultAsync(t => t.UserId == "user1");
        Assert.NotNull(token);
        Assert.False(token.IsEnabled);
        Assert.NotNull(token.Secret);

        var decryptedSecret = _encryptionService.Decrypt(token.Secret);
        Assert.Equal(result.Response.Secret, decryptedSecret);
    }

    [Fact]
    public async Task SetupAsync_IncompletePriorSetup_ReplacesTokenWithNewSecret()
    {
        await CreateUserAsync("user1");

        var oldSecret = "JBSWY3DPEHPK3PXP";
        var oldGuid = Guid.NewGuid();
        Context.Set<TwoFactorToken>().Add(new TwoFactorToken
        {
            TokenGuid = oldGuid,
            UserId = "user1",
            Secret = _encryptionService.Encrypt(oldSecret),
            IsEnabled = false,
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-10)
        });
        await Context.SaveChangesAsync();

        var result = await _service.SetupAsync("user1");

        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.NotNull(result.Response);
        Assert.NotEqual(oldSecret, result.Response.Secret);

        var tokens = await Context.Set<TwoFactorToken>().Where(t => t.UserId == "user1").ToListAsync();
        Assert.Single(tokens);
        Assert.False(tokens[0].IsEnabled);
        Assert.NotEqual(oldGuid, tokens[0].TokenGuid);
        Assert.Null(tokens[0].VerifiedAt);

        var decryptedSecret = _encryptionService.Decrypt(tokens[0].Secret);
        Assert.Equal(result.Response.Secret, decryptedSecret);
        Assert.NotEqual(oldSecret, decryptedSecret);
    }

    [Fact]
    public async Task SetupAsync_GoogleOnlyUser_ReturnsError()
    {
        await CreateUserAsync("google1", email: "google@example.com", authMethod: "Google");

        _localizerMock.Setup(x => x["auth.errors.twoFactorOnlyForLocal"])
            .Returns(new LocalizedString("auth.errors.twoFactorOnlyForLocal", "2FA only for local"));

        var result = await _service.SetupAsync("google1");

        Assert.False(result.Success);
        Assert.Contains("2FA only for local", result.Error);
        Assert.Null(result.Response);
        Assert.Empty(Context.Set<TwoFactorToken>().Where(t => t.UserId == "google1"));
    }

    [Fact]
    public async Task SetupAsync_LocalAndGoogleUser_AllowsSetup()
    {
        await CreateUserAsync("hybrid1", email: "hybrid@example.com", authMethod: "Local,Google");

        var result = await _service.SetupAsync("hybrid1");

        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.NotNull(result.Response.Secret);
    }

    [Fact]
    public async Task GetStatusAsync_GoogleOnlyWithOrphaned2FA_ClearsAndReportsDisabled()
    {
        var user = await CreateUserAsync(
            "google2",
            email: "google2@example.com",
            twoFactorEnabled: true,
            authMethod: "Google");

        Context.Set<TwoFactorToken>().Add(new TwoFactorToken
        {
            TokenGuid = Guid.NewGuid(),
            UserId = user.Id,
            Secret = _encryptionService.Encrypt("JBSWY3DPEHPK3PXP"),
            IsEnabled = true,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await Context.SaveChangesAsync();

        var result = await _service.GetStatusAsync(user.Id);

        Assert.True(result.Success);
        Assert.False(result.IsEnabled);
        Assert.Null(result.Method);

        var updated = await _userManager.FindByIdAsync(user.Id);
        Assert.False(updated!.TwoFactorEnabled);
        Assert.Empty(Context.Set<TwoFactorToken>().Where(t => t.UserId == user.Id));
    }

    [Fact]
    public async Task GetStatusAsync_LocalEnabled_ReturnsTotp()
    {
        await CreateUserAsync("user1", twoFactorEnabled: true);

        var result = await _service.GetStatusAsync("user1");

        Assert.True(result.Success);
        Assert.True(result.IsEnabled);
        Assert.Equal("totp", result.Method);
    }

    [Theory]
    [InlineData("Local", true)]
    [InlineData("local", true)]
    [InlineData("Local,Google", true)]
    [InlineData("Google,Local", true)]
    [InlineData("Google", false)]
    [InlineData("Telegram", false)]
    [InlineData("", false)]
    public void IsLocalCapable_ParsesAuthMethod(string authMethod, bool expected)
    {
        var user = new AppUser { AuthMethod = authMethod };
        Assert.Equal(expected, TwoFactorService.IsLocalCapable(user));
    }

    [Fact]
    public async Task EnableAsync_UserNotFound_ReturnsError()
    {
        _localizerMock.Setup(x => x["auth.errors.userNotFound"])
            .Returns(new LocalizedString("auth.errors.userNotFound", "User not found"));

        var request = new Enable2FARequest { Code = "123456" };

        var result = await _service.EnableAsync("nonexistent", request);

        Assert.False(result.Success);
        Assert.Contains("User not found", result.Error);
    }

    [Fact]
    public async Task EnableAsync_NoTokenSetup_ReturnsError()
    {
        await CreateUserAsync("user1");

        _localizerMock.Setup(x => x["auth.errors.twoFactorNotSetup"])
            .Returns(new LocalizedString("auth.errors.twoFactorNotSetup", "2FA not setup"));

        var request = new Enable2FARequest { Code = "123456" };

        var result = await _service.EnableAsync("user1", request);

        Assert.False(result.Success);
        Assert.Contains("2FA not setup", result.Error);
    }

    [Fact]
    public async Task EnableAsync_InvalidCode_ReturnsError()
    {
        var user = await CreateUserAsync("user1");

        var token = new TwoFactorToken
        {
            TokenGuid = Guid.NewGuid(),
            UserId = "user1",
            Secret = _encryptionService.Encrypt("JBSWY3DPEHPK3PXP"),
            IsEnabled = false
        };
        Context.Set<TwoFactorToken>().Add(token);
        await Context.SaveChangesAsync();

        _localizerMock.Setup(x => x["auth.errors.invalid2FACode"])
            .Returns(new LocalizedString("auth.errors.invalid2FACode", "Invalid 2FA code"));

        var request = new Enable2FARequest { Code = "000000" };

        var result = await _service.EnableAsync("user1", request);

        Assert.False(result.Success);
        Assert.Contains("Invalid 2FA code", result.Error);
    }

    [Fact]
    public async Task EnableAsync_ValidCode_Enables2FA()
    {
        var secret = "JBSWY3DPEHPK3PXP";
        var user = await CreateUserAsync("user1");

        var token = new TwoFactorToken
        {
            TokenGuid = Guid.NewGuid(),
            UserId = "user1",
            Secret = _encryptionService.Encrypt(secret),
            IsEnabled = false
        };
        Context.Set<TwoFactorToken>().Add(token);
        await Context.SaveChangesAsync();

        var totp = new Totp(Base32Encoding.ToBytes(secret));
        var validCode = totp.ComputeTotp();

        var request = new Enable2FARequest { Code = validCode };

        var result = await _service.EnableAsync("user1", request);

        Assert.True(result.Success);
        Assert.Null(result.Error);

        var updatedToken = await Context.Set<TwoFactorToken>().FirstAsync(t => t.UserId == "user1");
        Assert.True(updatedToken.IsEnabled);
        Assert.NotNull(updatedToken.VerifiedAt);

        var updatedUser = await _userManager.FindByIdAsync("user1");
        Assert.True(updatedUser!.TwoFactorEnabled);
    }

    [Fact]
    public async Task VerifyAsync_UserNotFound_ReturnsError()
    {
        _localizerMock.Setup(x => x["auth.errors.invalid2FACode"])
            .Returns(new LocalizedString("auth.errors.invalid2FACode", "Invalid 2FA code"));

        var result = await _service.VerifyAsync("nonexistent", "123456");

        Assert.False(result.Success);
        Assert.Contains("Invalid 2FA code", result.Error);
    }

    [Fact]
    public async Task VerifyAsync_ValidCode_ReturnsSuccess()
    {
        var secret = "JBSWY3DPEHPK3PXP";
        await CreateUserAsync("user1");

        var token = new TwoFactorToken
        {
            TokenGuid = Guid.NewGuid(),
            UserId = "user1",
            Secret = _encryptionService.Encrypt(secret),
            IsEnabled = true
        };
        Context.Set<TwoFactorToken>().Add(token);
        await Context.SaveChangesAsync();

        var totp = new Totp(Base32Encoding.ToBytes(secret));
        var validCode = totp.ComputeTotp();

        var result = await _service.VerifyAsync("user1", validCode);

        Assert.True(result.Success);
        Assert.Null(result.Error);
    }
}
