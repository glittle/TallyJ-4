using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Moq;
using OtpNet;
using TallyJ4.Application.DTOs.Auth;
using TallyJ4.Application.Services.Auth;
using TallyJ4.Domain.Entities;
using TallyJ4.Domain.Identity;
using TallyJ4.Services;

namespace TallyJ4.Tests.UnitTests;

public class TwoFactorServiceTests : ServiceTestBase
{
    private readonly TwoFactorService _service;
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<IStringLocalizer<TwoFactorService>> _localizerMock;
    private readonly Mock<EmailService> _emailServiceMock;
    private readonly EncryptionService _encryptionService;

    public TwoFactorServiceTests()
    {
        _userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);

        _localizerMock = new Mock<IStringLocalizer<TwoFactorService>>();
        _emailServiceMock = new Mock<EmailService>(null!, null!);

        // Create encryption service with test config
        var config = new Dictionary<string, string>
        {
            ["Encryption:Key"] = "ThisIsATestEncryptionKeyThatIsLongEnoughForAES256"
        };
        var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(config)
            .Build();
        _encryptionService = new EncryptionService(configuration);

        _service = new TwoFactorService(
            _userManagerMock.Object,
            _localizerMock.Object,
            Context,
            _emailServiceMock.Object,
            _encryptionService);
    }

    [Fact]
    public async Task SetupAsync_UserNotFound_ReturnsError()
    {
        // Arrange
        _userManagerMock.Setup(x => x.FindByIdAsync("nonexistent"))
            .ReturnsAsync((AppUser)null!);

        _localizerMock.Setup(x => x["auth.errors.userNotFound"])
            .Returns(new LocalizedString("auth.errors.userNotFound", "User not found"));

        // Act
        var result = await _service.SetupAsync("nonexistent");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User not found", result.Error);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task SetupAsync_UserAlreadyHas2FA_ReturnsError()
    {
        // Arrange
        var user = new AppUser { Id = "user1", TwoFactorEnabled = true };
        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

        _localizerMock.Setup(x => x["auth.errors.twoFactorAlreadyEnabled"])
            .Returns(new LocalizedString("auth.errors.twoFactorAlreadyEnabled", "2FA already enabled"));

        // Act
        var result = await _service.SetupAsync("user1");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("2FA already enabled", result.Error);
        Assert.Null(result.Response);
    }

    [Fact]
    public async Task SetupAsync_ValidUser_CreatesTokenAndReturnsResponse()
    {
        // Arrange
        var user = new AppUser { Id = "user1", Email = "test@example.com", TwoFactorEnabled = false };
        _userManagerMock.Setup(x => x.FindByIdAsync("user1")).ReturnsAsync(user);

        // Act
        var result = await _service.SetupAsync("user1");

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
        Assert.NotNull(result.Response);
        Assert.NotNull(result.Response.Secret);
        Assert.NotNull(result.Response.QrCodeDataUrl);

        // Verify token was created in database
        var token = await Context.Set<TwoFactorToken>().FirstOrDefaultAsync(t => t.UserId == "user1");
        Assert.NotNull(token);
        Assert.False(token.IsEnabled);
        Assert.NotNull(token.Secret);

        // Verify secret can be decrypted
        var decryptedSecret = _encryptionService.Decrypt(token.Secret);
        Assert.Equal(result.Response.Secret, decryptedSecret);
    }

    [Fact]
    public async Task EnableAsync_UserNotFound_ReturnsError()
    {
        // Arrange
        _userManagerMock.Setup(x => x.Users.Include(u => u.TwoFactorToken)
            .FirstOrDefaultAsync(u => u.Id == "nonexistent", default))
            .ReturnsAsync((AppUser)null!);

        _localizerMock.Setup(x => x["auth.errors.userNotFound"])
            .Returns(new LocalizedString("auth.errors.userNotFound", "User not found"));

        var request = new Enable2FARequest { Code = "123456" };

        // Act
        var result = await _service.EnableAsync("nonexistent", request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("User not found", result.Error);
    }

    [Fact]
    public async Task EnableAsync_NoTokenSetup_ReturnsError()
    {
        // Arrange
        var user = new AppUser { Id = "user1" };
        _userManagerMock.Setup(x => x.Users.Include(u => u.TwoFactorToken)
            .FirstOrDefaultAsync(u => u.Id == "user1", default))
            .ReturnsAsync(user);

        _localizerMock.Setup(x => x["auth.errors.twoFactorNotSetup"])
            .Returns(new LocalizedString("auth.errors.twoFactorNotSetup", "2FA not setup"));

        var request = new Enable2FARequest { Code = "123456" };

        // Act
        var result = await _service.EnableAsync("user1", request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("2FA not setup", result.Error);
    }

    [Fact]
    public async Task EnableAsync_InvalidCode_ReturnsError()
    {
        // Arrange
        var token = new TwoFactorToken
        {
            TokenGuid = Guid.NewGuid(),
            UserId = "user1",
            Secret = _encryptionService.Encrypt("JBSWY3DPEHPK3PXP"), // Valid base32 secret
            IsEnabled = false
        };

        var user = new AppUser { Id = "user1", TwoFactorToken = token };
        _userManagerMock.Setup(x => x.Users.Include(u => u.TwoFactorToken)
            .FirstOrDefaultAsync(u => u.Id == "user1", default))
            .ReturnsAsync(user);

        _localizerMock.Setup(x => x["auth.errors.invalid2FACode"])
            .Returns(new LocalizedString("auth.errors.invalid2FACode", "Invalid 2FA code"));

        var request = new Enable2FARequest { Code = "000000" }; // Invalid code

        // Act
        var result = await _service.EnableAsync("user1", request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid 2FA code", result.Error);
    }

    [Fact]
    public async Task EnableAsync_ValidCode_Enables2FA()
    {
        // Arrange
        var secret = "JBSWY3DPEHPK3PXP"; // Valid base32 secret
        var token = new TwoFactorToken
        {
            TokenGuid = Guid.NewGuid(),
            UserId = "user1",
            Secret = _encryptionService.Encrypt(secret),
            IsEnabled = false
        };

        var user = new AppUser { Id = "user1", Email = "test@example.com", TwoFactorToken = token };
        _userManagerMock.Setup(x => x.Users.Include(u => u.TwoFactorToken)
            .FirstOrDefaultAsync(u => u.Id == "user1", default))
            .ReturnsAsync(user);

        // Generate a valid TOTP code for the secret
        var totp = new OtpNet.Totp(Base32Encoding.ToBytes(secret));
        var validCode = totp.ComputeTotp();

        var request = new Enable2FARequest { Code = validCode };

        // Act
        var result = await _service.EnableAsync("user1", request);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);

        // Verify token was updated
        Assert.True(token.IsEnabled);
        Assert.True(user.TwoFactorEnabled);
        Assert.NotNull(token.VerifiedAt);
    }

    [Fact]
    public async Task VerifyAsync_UserNotFound_ReturnsError()
    {
        // Arrange
        _userManagerMock.Setup(x => x.Users.Include(u => u.TwoFactorToken)
            .FirstOrDefaultAsync(u => u.Id == "nonexistent", default))
            .ReturnsAsync((AppUser)null!);

        _localizerMock.Setup(x => x["auth.errors.invalid2FACode"])
            .Returns(new LocalizedString("auth.errors.invalid2FACode", "Invalid 2FA code"));

        // Act
        var result = await _service.VerifyAsync("nonexistent", "123456");

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid 2FA code", result.Error);
    }

    [Fact]
    public async Task VerifyAsync_ValidCode_ReturnsSuccess()
    {
        // Arrange
        var secret = "JBSWY3DPEHPK3PXP"; // Valid base32 secret
        var token = new TwoFactorToken
        {
            TokenGuid = Guid.NewGuid(),
            UserId = "user1",
            Secret = _encryptionService.Encrypt(secret),
            IsEnabled = true
        };

        var user = new AppUser { Id = "user1", TwoFactorToken = token };
        _userManagerMock.Setup(x => x.Users.Include(u => u.TwoFactorToken)
            .FirstOrDefaultAsync(u => u.Id == "user1", default))
            .ReturnsAsync(user);

        // Generate a valid TOTP code for the secret
        var totp = new OtpNet.Totp(Base32Encoding.ToBytes(secret));
        var validCode = totp.ComputeTotp();

        // Act
        var result = await _service.VerifyAsync("user1", validCode);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.Error);
    }
}