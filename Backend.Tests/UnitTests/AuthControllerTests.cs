using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Backend.Application.DTOs.Auth;
using Backend.Application.Services.Auth;
using Backend.Controllers;
using Backend.Domain.Context;
using Backend.Domain.Identity;
using Backend.Middleware;
using Backend.Services;
using Xunit;
using Backend.Authorization;

namespace Backend.Tests.UnitTests;

/// <summary>
/// Unit tests for AuthController including PKCE and 2FA verification.
/// </summary>
public class AuthControllerTests : ServiceTestBase
{
    private readonly AuthController _controller;
    private readonly Mock<LocalAuthService> _localAuthServiceMock;
    private readonly Mock<PasswordResetService> _passwordResetServiceMock;
    private readonly Mock<TwoFactorService> _twoFactorServiceMock;
    private readonly Mock<JwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<SignInManager<AppUser>> _signInManagerMock;
    private readonly Mock<IOptions<SuperAdminSettings>> _superAdminSettingsMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<OAuthStateService> _oauthStateServiceMock;
    private readonly Mock<ISecurityAuditService> _securityAuditServiceMock;

    public AuthControllerTests()
    {
        _localAuthServiceMock = new Mock<LocalAuthService>();
        _passwordResetServiceMock = new Mock<PasswordResetService>();
        _twoFactorServiceMock = new Mock<TwoFactorService>();
        _jwtTokenServiceMock = new Mock<JwtTokenService>();
        _userManagerMock = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        _roleManagerMock = new Mock<RoleManager<IdentityRole>>(
            Mock.Of<IRoleStore<IdentityRole>>(),
            null!, null!, null!, null!);
        _loggerMock = new Mock<ILogger<AuthController>>();
        _configurationMock = new Mock<IConfiguration>();
        _signInManagerMock = new Mock<SignInManager<AppUser>>(
            _userManagerMock.Object,
            Mock.Of<IHttpContextAccessor>(),
            Mock.Of<IUserClaimsPrincipalFactory<AppUser>>(),
            null!, null!, null!, null!);
        _superAdminSettingsMock = new Mock<IOptions<SuperAdminSettings>>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _oauthStateServiceMock = new Mock<OAuthStateService>();
        _securityAuditServiceMock = new Mock<ISecurityAuditService>();

        _controller = new AuthController(
            _localAuthServiceMock.Object,
            _passwordResetServiceMock.Object,
            _twoFactorServiceMock.Object,
            _jwtTokenServiceMock.Object,
            Context,
            _userManagerMock.Object,
            _roleManagerMock.Object,
            _loggerMock.Object,
            _configurationMock.Object,
            _signInManagerMock.Object,
            _superAdminSettingsMock.Object,
            _httpClientFactoryMock.Object,
            _securityAuditServiceMock.Object);

        // Setup HttpContext for cookie middleware
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    [Fact]
    public void GenerateCodeVerifier_ReturnsValidBase64UrlString()
    {
        // Act
        var codeVerifier = AuthController.GenerateCodeVerifier();

        // Assert
        Assert.NotNull(codeVerifier);
        Assert.True(codeVerifier.Length >= 43); // Minimum length for PKCE
        Assert.True(codeVerifier.Length <= 128); // Maximum length for PKCE

        // Should only contain URL-safe characters
        Assert.DoesNotContain("+", codeVerifier);
        Assert.DoesNotContain("/", codeVerifier);
        Assert.DoesNotContain("=", codeVerifier);
    }

    [Fact]
    public void GenerateCodeChallenge_ReturnsValidBase64UrlString()
    {
        // Arrange
        var codeVerifier = "test_code_verifier_12345";

        // Act
        var codeChallenge = AuthController.GenerateCodeChallenge(codeVerifier);

        // Assert
        Assert.NotNull(codeChallenge);
        Assert.True(codeChallenge.Length > 0);

        // Should only contain URL-safe characters
        Assert.DoesNotContain("+", codeChallenge);
        Assert.DoesNotContain("/", codeChallenge);
        Assert.DoesNotContain("=", codeChallenge);
    }

    [Fact]
    public void GenerateCodeChallenge_IsDeterministic()
    {
        // Arrange
        var codeVerifier = "test_code_verifier_12345";

        // Act
        var codeChallenge1 = AuthController.GenerateCodeChallenge(codeVerifier);
        var codeChallenge2 = AuthController.GenerateCodeChallenge(codeVerifier);

        // Assert
        Assert.Equal(codeChallenge1, codeChallenge2);
    }

    [Fact]
    public void Base64UrlEncode_EncodesCorrectly()
    {
        // Arrange
        var bytes = new byte[] { 255, 254, 253, 252 };

        // Act
        var encoded = AuthController.Base64UrlEncode(bytes);

        // Assert
        Assert.Equal("__79_A", encoded); // Expected base64url encoding
    }

    [Fact]
    public async Task Verify2FA_ValidRequest_CallsLocalAuthServiceWithPassword()
    {
        // Arrange
        var request = new Verify2FARequest
        {
            Email = "test@example.com",
            Password = "TestPass123!",
            Code = "123456"
        };

        var expectedResponse = new AuthResponse
        {
            Token = "jwt-token",
            RefreshToken = "refresh-token",
            Email = "test@example.com",
            Name = "Test User",
            AuthMethod = "Local",
            Requires2FA = false
        };

        _localAuthServiceMock.Setup(x => x.LoginAsync(It.Is<LoginRequest>(r =>
            r.Email == request.Email &&
            r.Password == request.Password &&
            r.TwoFactorCode == request.Code)))
            .ReturnsAsync((true, null, expectedResponse));

        // Act
        var result = await _controller.Verify2FA(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal(expectedResponse.Token, response.Token);
        Assert.Equal(expectedResponse.Email, response.Email);
    }

    [Fact]
    public async Task Verify2FA_InvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var request = new Verify2FARequest
        {
            Email = "test@example.com",
            Password = "WrongPassword",
            Code = "123456"
        };

        _localAuthServiceMock.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
            .ReturnsAsync((false, "Invalid credentials", null));

        // Act
        var result = await _controller.Verify2FA(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = badRequestResult.Value;
        Assert.NotNull(errorResponse);
        // The error property should exist in the anonymous object
        var errorProperty = errorResponse.GetType().GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal("Invalid credentials", errorProperty.GetValue(errorResponse));
    }

    [Fact]
    public async Task Verify2FA_RequiresPasswordAndCode()
    {
        // Arrange
        var request = new Verify2FARequest
        {
            Email = "test@example.com",
            Password = "", // Empty password should still be validated
            Code = "123456"
        };

        _localAuthServiceMock.Setup(x => x.LoginAsync(It.Is<LoginRequest>(r =>
            r.Email == request.Email &&
            r.Password == "" && // Verify empty password is passed through
            r.TwoFactorCode == request.Code)))
            .ReturnsAsync((false, "Invalid credentials", null));

        // Act
        var result = await _controller.Verify2FA(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.NotNull(badRequestResult.Value);

        // Verify that LoginAsync was called with the empty password (not hardcoded empty string)
        _localAuthServiceMock.Verify(x => x.LoginAsync(It.Is<LoginRequest>(r =>
            r.Email == request.Email &&
            r.Password == "" &&
            r.TwoFactorCode == request.Code)), Times.Once);
    }

    [Fact]
    public void GetFrontendUrl_WithValidReturnUrl_ReturnsReturnUrl()
    {
        // Arrange
        var returnUrl = "https://example.com/auth/callback";
        _configurationMock.Setup(c => c["Frontend:BaseUrl"]).Returns("http://localhost:8095");

        // Act
        var result = InvokeGetFrontendUrl(returnUrl);

        // Assert
        Assert.Equal(returnUrl, result);
    }

    [Fact]
    public void GetFrontendUrl_WithConfiguredFrontendUrl_ReturnsConfiguredUrl()
    {
        // Arrange
        var configuredUrl = "https://myapp.com";
        _configurationMock.Setup(c => c["Frontend:BaseUrl"]).Returns(configuredUrl);

        // Act
        var result = InvokeGetFrontendUrl(null);

        // Assert
        Assert.Equal(configuredUrl + "/auth/google/callback", result);
    }

    [Fact]
    public void GetFrontendUrl_WithNullConfiguration_ReturnsFallbackUrl()
    {
        // Arrange
        _configurationMock.Setup(c => c["Frontend:BaseUrl"]).Returns((string)null);

        // Act
        var result = InvokeGetFrontendUrl(null);

        // Assert
        Assert.Equal("http://localhost:8095/auth/google/callback", result);
    }

    [Fact]
    public void GetFrontendUrl_WithEmptyConfiguration_ReturnsFallbackUrl()
    {
        // Arrange
        _configurationMock.Setup(c => c["Frontend:BaseUrl"]).Returns("");

        // Act
        var result = InvokeGetFrontendUrl(null);

        // Assert
        Assert.Equal("http://localhost:8095/auth/google/callback", result);
    }

    [Fact]
    public void GetFrontendUrl_WithWhitespaceConfiguration_ReturnsFallbackUrl()
    {
        // Arrange
        _configurationMock.Setup(c => c["Frontend:BaseUrl"]).Returns("   ");

        // Act
        var result = InvokeGetFrontendUrl(null);

        // Assert
        Assert.Equal("http://localhost:8095/auth/google/callback", result);
    }

    [Fact]
    public async Task VerifyEmail_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var request = new VerifyEmailRequest { Email = "test@example.com", Token = "valid-token" };
        var user = new AppUser { Email = "test@example.com", EmailConfirmed = false };

        _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(um => um.ConfirmEmailAsync(user, request.Token))
            .ReturnsAsync(IdentityResult.Success);

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal("Email verified successfully", ((dynamic)okResult.Value).message);
    }

    [Fact]
    public async Task VerifyEmail_WithNonexistentUser_ReturnsBadRequest()
    {
        // Arrange
        var request = new VerifyEmailRequest { Email = "nonexistent@example.com", Token = "token" };

        _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync((AppUser)null);

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("User not found", ((dynamic)badRequestResult.Value).error);
    }

    [Fact]
    public async Task VerifyEmail_WithAlreadyVerifiedEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new VerifyEmailRequest { Email = "test@example.com", Token = "token" };
        var user = new AppUser { Email = "test@example.com", EmailConfirmed = true };

        _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync(user);

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("Email is already verified", ((dynamic)badRequestResult.Value).error);
    }

    [Fact]
    public async Task VerifyEmail_WithInvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new VerifyEmailRequest { Email = "test@example.com", Token = "invalid-token" };
        var user = new AppUser { Email = "test@example.com", EmailConfirmed = false };
        var identityError = new IdentityError { Description = "Invalid token" };
        var identityResult = IdentityResult.Failed(identityError);

        _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync(user);
        _userManagerMock.Setup(um => um.ConfirmEmailAsync(user, request.Token))
            .ReturnsAsync(identityResult);

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid token", ((dynamic)badRequestResult.Value).error);
    }

    [Fact]
    public void GetFrontendUrl_WithInvalidReturnUrl_ReturnsConfiguredUrl()
    {
        // Arrange
        var invalidReturnUrl = "not-a-valid-url";
        var configuredUrl = "https://myapp.com";
        _configurationMock.Setup(c => c["Frontend:BaseUrl"]).Returns(configuredUrl);

        // Act
        var result = InvokeGetFrontendUrl(invalidReturnUrl);

        // Assert
        Assert.Equal(configuredUrl + "/auth/google/callback", result);
    }

    private string InvokeGetFrontendUrl(string? returnUrl)
    {
        // Use reflection to access the private method
        var method = typeof(AuthController).GetMethod("GetFrontendUrl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (string)method.Invoke(_controller, new object[] { returnUrl });
    }
}



