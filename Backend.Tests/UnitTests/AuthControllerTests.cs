using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
    private readonly Mock<ILocalAuthService> _localAuthServiceMock;
    private readonly Mock<IPasswordResetService> _passwordResetServiceMock;
    private readonly Mock<ITwoFactorService> _twoFactorServiceMock;
    private readonly Mock<IJwtTokenService> _jwtTokenServiceMock;
    private readonly Mock<UserManager<AppUser>> _userManagerMock;
    private readonly Mock<RoleManager<IdentityRole>> _roleManagerMock;
    private readonly Mock<ILogger<AuthController>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<SignInManager<AppUser>> _signInManagerMock;
    private readonly Mock<IOptions<SuperAdminSettings>> _superAdminSettingsMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<OAuthStateService> _oauthStateServiceMock;
    private readonly Mock<ISecurityAuditService> _securityAuditServiceMock;
    private readonly Mock<IRemoteLogService> _remoteLogServiceMock;

    public AuthControllerTests()
    {
        _localAuthServiceMock = new Mock<ILocalAuthService>();
        _passwordResetServiceMock = new Mock<IPasswordResetService>();
        _twoFactorServiceMock = new Mock<ITwoFactorService>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
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
        _remoteLogServiceMock = new Mock<IRemoteLogService>();

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
            _securityAuditServiceMock.Object,
            _remoteLogServiceMock.Object);

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
        _configurationMock.Setup(c => c["Frontend:BaseUrl"]).Returns((string?)null);

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
        Assert.Equal("Email verified successfully", ((dynamic)okResult.Value!).message);
    }

    [Fact]
    public async Task VerifyEmail_WithNonexistentUser_ReturnsBadRequest()
    {
        // Arrange
        var request = new VerifyEmailRequest { Email = "nonexistent@example.com", Token = "token" };

        _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email)).ReturnsAsync((AppUser?)null);

        // Act
        var result = await _controller.VerifyEmail(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("User not found", ((dynamic)badRequestResult.Value!).error);
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
        Assert.Equal("Email is already verified", ((dynamic)badRequestResult.Value!).error);
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
        Assert.Contains("Invalid token", ((dynamic)badRequestResult.Value!).error);
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

    [Fact]
    public async Task Register_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "test@example.com",
            Password = "TestPass123!",
            DisplayName = "Test User"
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

        _localAuthServiceMock.Setup(x => x.RegisterAsync(request))
            .ReturnsAsync((true, null, expectedResponse));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Equal(expectedResponse.Token, response.Token);
        Assert.Equal(expectedResponse.Email, response.Email);
    }

    [Fact]
    public async Task Register_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Email = "invalid-email",
            Password = "weak",
            DisplayName = "Test User"
        };

        _localAuthServiceMock.Setup(x => x.RegisterAsync(request))
            .ReturnsAsync((false, "Registration failed", null));

        // Act
        var result = await _controller.Register(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = badRequestResult.Value;
        Assert.NotNull(errorResponse);
        var errorProperty = errorResponse.GetType().GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal("Registration failed", errorProperty.GetValue(errorResponse));
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOk()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "TestPass123!"
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

        _localAuthServiceMock.Setup(x => x.LoginAsync(request))
            .ReturnsAsync((true, null, expectedResponse));

        // Act
        var result = await _controller.Login(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<AuthResponse>(okResult.Value);
        Assert.Null(response.Token); // Tokens should be in cookies, not response
        Assert.Null(response.RefreshToken);
        Assert.Equal(expectedResponse.Email, response.Email);
    }

    [Fact]
    public async Task Login_InvalidCredentials_ReturnsBadRequest()
    {
        // Arrange
        var request = new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword"
        };

        _localAuthServiceMock.Setup(x => x.LoginAsync(request))
            .ReturnsAsync((false, "Invalid credentials", null));

        // Act
        var result = await _controller.Login(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = badRequestResult.Value;
        Assert.NotNull(errorResponse);
        var errorProperty = errorResponse.GetType().GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal("Invalid credentials", errorProperty.GetValue(errorResponse));
    }

    [Fact]
    public async Task ForgotPassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new ForgotPasswordRequest { Email = "test@example.com" };

        _passwordResetServiceMock.Setup(x => x.GenerateResetTokenAsync(request))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        var messageProperty = response.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal("Password reset email sent if account exists", messageProperty.GetValue(response));
    }

    [Fact]
    public async Task ForgotPassword_InvalidRequest_ReturnsBadRequest()
    {
        // Arrange
        var request = new ForgotPasswordRequest { Email = "invalid@example.com" };

        _passwordResetServiceMock.Setup(x => x.GenerateResetTokenAsync(request))
            .ReturnsAsync((false, "Email not found"));

        // Act
        var result = await _controller.ForgotPassword(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = badRequestResult.Value;
        Assert.NotNull(errorResponse);
        var errorProperty = errorResponse.GetType().GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal("Email not found", errorProperty.GetValue(errorResponse));
    }

    [Fact]
    public async Task ResetPassword_ValidRequest_ReturnsOk()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Email = "test@example.com",
            Token = "valid-reset-token",
            NewPassword = "NewPass123!"
        };

        _passwordResetServiceMock.Setup(x => x.ResetPasswordAsync(request))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        var messageProperty = response.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal("Password reset successful", messageProperty.GetValue(response));
    }

    [Fact]
    public async Task ResetPassword_InvalidToken_ReturnsBadRequest()
    {
        // Arrange
        var request = new ResetPasswordRequest
        {
            Email = "test@example.com",
            Token = "invalid-token",
            NewPassword = "NewPass123!"
        };

        _passwordResetServiceMock.Setup(x => x.ResetPasswordAsync(request))
            .ReturnsAsync((false, "Invalid token"));

        // Act
        var result = await _controller.ResetPassword(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = badRequestResult.Value;
        Assert.NotNull(errorResponse);
        var errorProperty = errorResponse.GetType().GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal("Invalid token", errorProperty.GetValue(errorResponse));
    }

    [Fact]
    public async Task Setup2FA_AuthenticatedUser_ReturnsOk()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new AppUser { Id = userId, Email = "test@example.com" };
        var setupResponse = new TwoFactorSetupResponse
        {
            Secret = "JBSWY3DPEHPK3PXP",
            QrCodeDataUrl = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA..."
        };

        SetupAuthenticatedUser(userId);
        _userManagerMock.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
        _twoFactorServiceMock.Setup(x => x.SetupAsync(userId))
            .ReturnsAsync((true, null, setupResponse));

        // Act
        var result = await _controller.Setup2FA();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<TwoFactorSetupResponse>(okResult.Value);
        Assert.Equal(setupResponse.Secret, response.Secret);
        Assert.Equal(setupResponse.QrCodeDataUrl, response.QrCodeDataUrl);
    }

    [Fact]
    public async Task Setup2FA_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange - no user claims set up

        // Act
        var result = await _controller.Setup2FA();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public async Task Enable2FA_ValidCode_ReturnsOk()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new AppUser { Id = userId, Email = "test@example.com" };
        var request = new Enable2FARequest { Code = "123456" };

        SetupAuthenticatedUser(userId);
        _userManagerMock.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
        _twoFactorServiceMock.Setup(x => x.EnableAsync(userId, request))
            .ReturnsAsync((true, null));

        // Act
        var result = await _controller.Enable2FA(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        var messageProperty = response.GetType().GetProperty("message");
        Assert.NotNull(messageProperty);
        Assert.Equal("Two-factor authentication enabled", messageProperty.GetValue(response));
    }

    [Fact]
    public async Task Enable2FA_InvalidCode_ReturnsBadRequest()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new AppUser { Id = userId, Email = "test@example.com" };
        var request = new Enable2FARequest { Code = "wrong-code" };

        SetupAuthenticatedUser(userId);
        _userManagerMock.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
        _twoFactorServiceMock.Setup(x => x.EnableAsync(userId, request))
            .ReturnsAsync((false, "Invalid verification code"));

        // Act
        var result = await _controller.Enable2FA(request);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        var errorResponse = badRequestResult.Value;
        Assert.NotNull(errorResponse);
        var errorProperty = errorResponse.GetType().GetProperty("error");
        Assert.NotNull(errorProperty);
        Assert.Equal("Invalid verification code", errorProperty.GetValue(errorResponse));
    }

    [Fact]
    public async Task Get2FAStatus_Enabled_ReturnsStatus()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new AppUser { Id = userId, Email = "test@example.com", TwoFactorEnabled = true };

        SetupAuthenticatedUser(userId);
        _userManagerMock.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _controller.Get2FAStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        var isEnabledProperty = response.GetType().GetProperty("isEnabled");
        var methodProperty = response.GetType().GetProperty("method");
        Assert.NotNull(isEnabledProperty);
        Assert.NotNull(methodProperty);
        Assert.True((bool)isEnabledProperty.GetValue(response)!);
        Assert.Equal("totp", methodProperty.GetValue(response));
    }

    [Fact]
    public async Task Get2FAStatus_Disabled_ReturnsStatus()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new AppUser { Id = userId, Email = "test@example.com", TwoFactorEnabled = false };

        SetupAuthenticatedUser(userId);
        _userManagerMock.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);

        // Act
        var result = await _controller.Get2FAStatus();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        var isEnabledProperty = response.GetType().GetProperty("isEnabled");
        var methodProperty = response.GetType().GetProperty("method");
        Assert.NotNull(isEnabledProperty);
        Assert.NotNull(methodProperty);
        Assert.False((bool)isEnabledProperty.GetValue(response)!);
        Assert.Null(methodProperty.GetValue(response));
    }

    [Fact]
    public async Task GetUserRoles_AuthenticatedUser_ReturnsRoles()
    {
        // Arrange
        var userId = "test-user-id";
        var user = new AppUser { Id = userId, Email = "test@example.com" };
        var roles = new List<string> { "User", "Admin" };

        SetupAuthenticatedUser(userId);
        _userManagerMock.Setup(um => um.FindByIdAsync(userId)).ReturnsAsync(user);
        _userManagerMock.Setup(um => um.GetRolesAsync(user)).ReturnsAsync(roles);

        // Act
        var result = await _controller.GetUserRoles();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        var userIdProperty = response.GetType().GetProperty("userId");
        var emailProperty = response.GetType().GetProperty("email");
        var rolesProperty = response.GetType().GetProperty("roles");
        Assert.NotNull(userIdProperty);
        Assert.NotNull(emailProperty);
        Assert.NotNull(rolesProperty);
        Assert.Equal(userId, userIdProperty.GetValue(response));
        Assert.Equal(user.Email, emailProperty.GetValue(response));
        Assert.Equal(roles, rolesProperty.GetValue(response));
    }

    [Fact]
    public async Task GetUserRoles_UnauthenticatedUser_ReturnsUnauthorized()
    {
        // Arrange - no user claims set up

        // Act
        var result = await _controller.GetUserRoles();

        // Assert
        Assert.IsType<UnauthorizedResult>(result);
    }

    [Fact]
    public void GetAllRoles_ReturnsAllRoles()
    {
        // Arrange
        var expectedRoles = new List<string> { "Admin", "User", "Moderator" };
        _roleManagerMock.Setup(rm => rm.Roles).Returns(expectedRoles.Select(r => new IdentityRole(r)).AsQueryable());

        // Act
        var result = _controller.GetAllRoles();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        var rolesProperty = response.GetType().GetProperty("roles");
        Assert.NotNull(rolesProperty);
        var roles = Assert.IsType<List<string>>(rolesProperty.GetValue(response));
        Assert.Equal(expectedRoles, roles);
    }

    private void SetupAuthenticatedUser(string userId)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId)
        };
        var identity = new ClaimsIdentity(claims, "TestAuth");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext();
        httpContext.User = principal;
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    private string InvokeGetFrontendUrl(string? returnUrl)
    {
        // Use reflection to access the private method
        var method = typeof(AuthController).GetMethod("GetFrontendUrl",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (method == null)
        {
            throw new InvalidOperationException("GetFrontendUrl method not found");
        }
        return (string)(method.Invoke(_controller, [returnUrl]) ?? throw new InvalidOperationException("Method returned null"));
    }
}



