using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using OtpNet;
using Backend.Application.DTOs.Auth;
using Backend.Application.Services.Auth;
using Backend.Domain.Entities;
using Backend.Domain.Identity;
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
        bool twoFactorEnabled = false)
    {
        var user = new AppUser
        {
            Id = id,
            UserName = email,
            Email = email,
            TwoFactorEnabled = twoFactorEnabled,
            AuthMethod = "Local"
        };
        var result = await _userManager.CreateAsync(user, "password123");
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
