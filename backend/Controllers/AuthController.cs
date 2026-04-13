using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Backend.Application.DTOs.Auth;
using Backend.Application.Services.Auth;
using Backend.Authorization;
using Backend.Domain;
using Backend.Domain.Context;
using Backend.Domain.Identity;
using Backend.DTOs.Security;
using Backend.Middleware;
using Backend.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Controllers;

/// <summary>
/// Controller for handling authentication and authorization operations including user registration,
/// login, password management, two-factor authentication, and role management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly ILocalAuthService _localAuthService;
    private readonly IPasswordResetService _passwordResetService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly MainDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly SuperAdminSettings _superAdminSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ISecurityAuditService _securityAuditService;
    private readonly IRemoteLogService _remoteLogService;

    /// <summary>
    /// Initializes a new instance of the AuthController.
    /// </summary>
    /// <param name="localAuthService">Service for local authentication operations.</param>
    /// <param name="passwordResetService">Service for password reset operations.</param>
    /// <param name="twoFactorService">Service for two-factor authentication operations.</param>
    /// <param name="jwtTokenService">Service for JWT token management.</param>
    /// <param name="context">The main database context.</param>
    /// <param name="userManager">ASP.NET Core Identity user manager.</param>
    /// <param name="roleManager">ASP.NET Core Identity role manager.</param>
    /// <param name="logger">Logger for recording operations.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="signInManager">ASP.NET Core Identity sign-in manager.</param>
    /// <param name="superAdminSettings">Configuration settings for super admin functionality.</param>
    /// <param name="httpClientFactory">HTTP client factory for external API requests.</param>
    /// <param name="securityAuditService">Service for logging security events.</param>
    /// <param name="remoteLogService">Service for sending remote log messages.</param>
    public AuthController(
        ILocalAuthService localAuthService,
        IPasswordResetService passwordResetService,
        ITwoFactorService twoFactorService,
        IJwtTokenService jwtTokenService,
        MainDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AuthController> logger,
        IConfiguration configuration,
        SignInManager<AppUser> signInManager,
        IOptions<SuperAdminSettings> superAdminSettings,
        IHttpClientFactory httpClientFactory,
        ISecurityAuditService securityAuditService,
        IRemoteLogService remoteLogService)
    {
        _localAuthService = localAuthService;
        _passwordResetService = passwordResetService;
        _twoFactorService = twoFactorService;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
        _configuration = configuration;
        _signInManager = signInManager;
        _superAdminSettings = superAdminSettings.Value;
        _httpClientFactory = httpClientFactory;
        _securityAuditService = securityAuditService;
        _remoteLogService = remoteLogService;

        Console.WriteLine("[AUTH CONTROLLER] AuthController instantiated");
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">The registration request containing user details.</param>
    /// <returns>The authentication response if successful, or an error if registration fails.</returns>
    [HttpPost("registerAccount")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _localAuthService.RegisterAsync(request);

        if (!success)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.AccountCreated,
                Email = request.Email,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Registration failed: {error}",
                IsSuspicious = false,
                Severity = Backend.Domain.SecurityEventSeverity.Info
            });
            return BadRequest(new { error });
        }

        // Get the user ID for logging
        var user = await _userManager.FindByEmailAsync(request.Email);
        var userId = user?.Id;

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.AccountCreated,
            UserId = userId,
            Email = request.Email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "User account created successfully",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        return Ok(response);
    }

    /// <summary>
    /// Authenticates a user and sets secure cookies with access tokens.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <returns>The authentication response with user info if successful, or an error if login fails.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _localAuthService.LoginAsync(request);

        if (!success)
        {
            // Determine if this is a suspicious login attempt
            var isSuspicious = error?.Contains("locked") == true || error?.Contains("invalid") == true;
            var severity = isSuspicious ? SecurityEventSeverity.Warning : SecurityEventSeverity.Info;

            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.LoginFailure,
                Email = request.Email,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Login failed: {error}",
                IsSuspicious = isSuspicious,
                Severity = severity
            });

            return BadRequest(new { error });
        }

        // Get user ID for successful login logging
        var user = await _userManager.FindByEmailAsync(request.Email);
        var userId = user?.Id;

        // Successful login
        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.LoginSuccess,
            UserId = userId,
            Email = request.Email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = response?.Requires2FA == true ? "Login successful, 2FA required" : "Login successful",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        // Only set cookies if 2FA is not required
        if (response != null && !response.Requires2FA && !string.IsNullOrEmpty(response.Token))
        {
            // Set secure cookies instead of returning tokens in response
            SecureCookieMiddleware.SetAuthCookies(
                HttpContext,
                response.Token,
                response.RefreshToken ?? "",
                response.Email,
                response.Name,
                response.AuthMethod ?? "Local",
                HttpContext.Request.IsHttps
            );
        }

        // Return response without tokens (tokens are in httpOnly cookies)
        return Ok(new AuthResponse
        {
            Token = null, // Not returned - stored in httpOnly cookie
            RefreshToken = null, // Not returned - stored in httpOnly cookie
            Email = response?.Email ?? "",
            Name = response?.Name,
            AuthMethod = response?.AuthMethod ?? "Local",
            Requires2FA = response?.Requires2FA ?? false
        });
    }

    /// <summary>
    /// Authenticates an assistant teller using an election access code.
    /// </summary>
    /// <param name="request">The teller login request containing election GUID and access code.</param>
    /// <returns>A teller authentication response with a limited JWT if successful.</returns>
    [AllowAnonymous]
    [HttpPost("teller-login")]
    public async Task<IActionResult> TellerLogin([FromBody] TellerLoginRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == request.ElectionGuid);

        if (election == null)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TellerLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Teller login failed: election not found ({request.ElectionGuid})",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Info
            });
            return BadRequest(new { error = "Invalid election or access code" });
        }

        if (election.ListedForPublicAsOf == null)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TellerLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Teller login failed: election not open for tellers ({request.ElectionGuid})",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Info
            });
            return BadRequest(new { error = "This election is not currently open for teller access" });
        }

        if (string.IsNullOrEmpty(election.ElectionPasscode) ||
            !string.Equals(election.ElectionPasscode, request.AccessCode, StringComparison.Ordinal))
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TellerLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"Teller login failed: invalid access code for election ({request.ElectionGuid})",
                IsSuspicious = true,
                Severity = SecurityEventSeverity.Warning
            });
            return BadRequest(new { error = "Invalid election or access code" });
        }

        var token = _jwtTokenService.GenerateTellerToken(election.ElectionGuid);

        SecureCookieMiddleware.SetAuthCookies(
            HttpContext,
            token,
            "",
            "",
            "Teller",
            "AccessCode",
            HttpContext.Request.IsHttps
        );

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.TellerLoginSuccess,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = $"Teller login successful for election ({request.ElectionGuid})",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        return Ok(new TellerLoginResponse
        {
            ElectionGuid = election.ElectionGuid,
            ElectionName = election.Name
        });
    }

    /// <summary>
    /// Initiates a password reset by sending a reset email to the user.
    /// </summary>
    /// <param name="request">The forgot password request containing the user's email.</param>
    /// <returns>A success message if the email was sent, or an error if the request fails.</returns>
    [HttpPost("forgotPassword")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var (success, error) = await _passwordResetService.GenerateResetTokenAsync(request);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(new { message = "Password reset email sent if account exists" });
    }

    /// <summary>
    /// Resets a user's password using a reset token.
    /// </summary>
    /// <param name="request">The reset password request containing the token and new password.</param>
    /// <returns>A success message if the password was reset, or an error if the request fails.</returns>
    [HttpPost("resetPassword")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var (success, error) = await _passwordResetService.ResetPasswordAsync(request);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(new { message = "Password reset successful" });
    }

    /// <summary>
    /// Verifies a user's email address using a verification token.
    /// </summary>
    /// <param name="request">The verify email request containing the email and verification token.</param>
    /// <returns>A success message if the email was verified, or an error if the request fails.</returns>
    [HttpPost("verifyEmail")]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
        {
            return BadRequest(new { error = "User not found" });
        }

        if (user.EmailConfirmed)
        {
            return BadRequest(new { error = "Email is already verified" });
        }

        var result = await _userManager.ConfirmEmailAsync(user, request.Token);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return BadRequest(new { error = errors });
        }

        return Ok(new { message = "Email verified successfully" });
    }

    /// <summary>
    /// Sets up two-factor authentication for the authenticated user.
    /// </summary>
    /// <returns>The 2FA setup information including QR code and secret key.</returns>
    [Authorize]
    [HttpPost("setup2fa")]
    public async Task<IActionResult> Setup2FA()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _twoFactorService.SetupAsync(userId);

        if (!success)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TwoFactorSetup,
                UserId = userId,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"2FA setup failed: {error}",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Warning
            });
            return BadRequest(new { error });
        }

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.TwoFactorSetup,
            UserId = userId,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "2FA setup initiated successfully",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        return Ok(response);
    }

    /// <summary>
    /// Enables two-factor authentication for the authenticated user.
    /// </summary>
    /// <param name="request">The enable 2FA request containing the verification code.</param>
    /// <returns>A success message if 2FA was enabled, or an error if the request fails.</returns>
    [Authorize]
    [HttpPost("enable2fa")]
    public async Task<IActionResult> Enable2FA([FromBody] Enable2FARequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error) = await _twoFactorService.EnableAsync(userId, request);

        if (!success)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TwoFactorEnabled,
                UserId = userId,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"2FA enable failed: {error}",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Warning
            });
            return BadRequest(new { error });
        }

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.TwoFactorEnabled,
            UserId = userId,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "2FA enabled successfully",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        return Ok(new { message = "Two-factor authentication enabled" });
    }

    /// <summary>
    /// Disables two-factor authentication for the authenticated user.
    /// </summary>
    /// <param name="request">The disable 2FA request containing the verification code.</param>
    /// <returns>A success message if 2FA was disabled, or an error if the request fails.</returns>
    [Authorize]
    [HttpPost("disable2fa")]
    public async Task<IActionResult> Disable2FA([FromBody] Disable2FARequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error) = await _twoFactorService.DisableAsync(userId, request);

        if (!success)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TwoFactorDisabled,
                UserId = userId,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"2FA disable failed: {error}",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Warning
            });
            return BadRequest(new { error });
        }

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.TwoFactorDisabled,
            UserId = userId,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "2FA disabled successfully",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        return Ok(new { message = "Two-factor authentication disabled" });
    }

    /// <summary>
    /// Gets the two-factor authentication status for the authenticated user.
    /// </summary>
    /// <returns>The 2FA status including whether it is enabled and the method used.</returns>
    [Authorize]
    [HttpGet("2fa/status")]
    public async Task<IActionResult> Get2FAStatus()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        return Ok(new
        {
            isEnabled = user.TwoFactorEnabled,
            method = user.TwoFactorEnabled ? "totp" : (string?)null
        });
    }

    /// <summary>
    /// Verifies a two-factor authentication code for login.
    /// </summary>
    /// <param name="request">The verify 2FA request containing email, password, and verification code.</param>
    /// <returns>The authentication response with tokens if successful, or an error if verification fails.</returns>
    [HttpPost("verify2fa")]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var (success, error, response) = await _localAuthService.LoginAsync(new LoginRequest
        {
            Email = request.Email,
            Password = request.Password,
            TwoFactorCode = request.Code
        });

        if (!success)
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.TwoFactorVerificationFailure,
                Email = request.Email,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = $"2FA verification failed: {error}",
                IsSuspicious = true,
                Severity = SecurityEventSeverity.Warning
            });

            return BadRequest(new { error });
        }

        // Get user ID for logging
        var user = await _userManager.FindByEmailAsync(request.Email);
        var userId = user?.Id;

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.TwoFactorVerificationSuccess,
            UserId = userId,
            Email = request.Email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "2FA verification successful",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        // Set secure cookies with authentication data
        SecureCookieMiddleware.SetAuthCookies(
            HttpContext,
            response!.Token ?? "",
            response.RefreshToken ?? "",
            response.Email,
            response.Name,
            response.AuthMethod ?? "Local",
            HttpContext.Request.IsHttps
        );

        // Return response with tokens for backward compatibility with frontend
        return Ok(new AuthResponse
        {
            Token = response.Token, // Keep for backward compatibility
            RefreshToken = response.RefreshToken,
            Email = response.Email,
            Name = response.Name,
            AuthMethod = response.AuthMethod ?? "Local",
            Requires2FA = response.Requires2FA
        });
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token and sets secure cookies.
    /// Supports reading refresh token from either request body or httpOnly cookie.
    /// </summary>
    /// <param name="request">The refresh token request containing the refresh token (optional if using cookies).</param>
    /// <returns>New access and refresh tokens if successful, or an error if the refresh token is invalid.</returns>
    [HttpPost("refreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest? request)
    {
        // Try to get refresh token from request body first, then fall back to cookie
        var refreshTokenValue = request?.RefreshToken;

        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            // Try to get from cookie
            refreshTokenValue = HttpContext.Request.Cookies["refresh_token"];
        }

        if (string.IsNullOrEmpty(refreshTokenValue))
        {
            return BadRequest(new { error = "Refresh token is required" });
        }

        var tokenHash = _jwtTokenService.HashRefreshToken(refreshTokenValue);
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash && !rt.IsRevoked);

        if (refreshToken == null || refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return BadRequest(new { error = "Invalid or expired refresh token" });
        }

        // Generate new tokens
        var user = await _context.Users.FindAsync(refreshToken.UserId);
        if (user == null)
        {
            return BadRequest(new { error = "User not found" });
        }

        var newToken = _jwtTokenService.GenerateToken(user);
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenEntity = _jwtTokenService.CreateRefreshToken(user.Id, newRefreshToken);

        // Revoke old refresh token
        refreshToken.IsRevoked = true;
        refreshToken.RevokedReason = "Replaced by new token";
        refreshToken.ReplacedByToken = newRefreshToken;

        _context.RefreshTokens.Add(newRefreshTokenEntity);
        await _context.SaveChangesAsync();

        var authMethod = user.AuthMethod ?? "Local";

        // Set secure cookies with new tokens
        SecureCookieMiddleware.SetAuthCookies(
            HttpContext,
            newToken,
            newRefreshToken,
            user.Email!,
            user.DisplayName,
            authMethod,
            HttpContext.Request.IsHttps
        );

        return Ok(new AuthResponse
        {
            Token = null, // Not returned - stored in httpOnly cookie
            RefreshToken = null, // Not returned - stored in httpOnly cookie
            Email = user.Email!,
            Name = user.DisplayName,
            AuthMethod = authMethod,
            Requires2FA = false
        });
    }

    /// <summary>
    /// Gets the roles assigned to the authenticated user.
    /// </summary>
    /// <returns>The user's roles and basic information.</returns>
    [Authorize]
    [HttpGet("getUserRoles")]
    public async Task<IActionResult> GetUserRoles()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new { userId = user.Id, email = user.Email, roles });
    }

    /// <summary>
    /// Assigns a role to a user. Requires admin privileges.
    /// </summary>
    /// <param name="userId">The ID of the user to assign the role to.</param>
    /// <param name="request">The assign role request containing the role name.</param>
    /// <returns>A success message if the role was assigned, or an error if the operation fails.</returns>
    [Authorize(Policy = "AdminOnly")]
    [HttpPost("{userId}/assignRole")]
    public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        if (!await _roleManager.RoleExistsAsync(request.RoleName))
        {
            return BadRequest(new { error = "Role does not exist" });
        }

        var result = await _userManager.AddToRoleAsync(user, request.RoleName);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        return Ok(new { message = $"Role {request.RoleName} assigned to user {user.Email}" });
    }

    /// <summary>
    /// Removes a role from a user. Requires admin privileges.
    /// </summary>
    /// <param name="userId">The ID of the user to remove the role from.</param>
    /// <param name="roleName">The name of the role to remove.</param>
    /// <returns>A success message if the role was removed, or an error if the operation fails.</returns>
    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{userId}/{roleName}/removeRole")]
    public async Task<IActionResult> RemoveRole(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        return Ok(new { message = $"Role {roleName} removed from user {user.Email}" });
    }

    /// <summary>
    /// Gets all available roles in the system. Requires admin privileges.
    /// </summary>
    /// <returns>A list of all available roles.</returns>
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("getAllRoles")]
    public IActionResult GetAllRoles()
    {
        var roles = _roleManager.Roles.Select(r => r.Name).ToList();
        return Ok(new { roles });
    }

    /// <summary>
    /// Initiates Google OAuth login flow with PKCE (Proof Key for Code Exchange) and state parameter for CSRF protection.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful authentication (default: frontend origin).</param>
    /// <returns>A redirect to Google's OAuth consent screen.</returns>
    [HttpGet("google/login")]
    public async Task<IActionResult> GoogleLogin([FromQuery] string? returnUrl = null)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        _logger.LogInformation("Google login attempt from {ClientIp} with returnUrl: {ReturnUrl}", clientIp, returnUrl);

        var googleClientId = _configuration["Google:ClientId"];
        var googleClientSecret = _configuration["Google:ClientSecret"];

        _logger.LogInformation("Google ClientId configured: {!string.IsNullOrWhiteSpace(googleClientId)}, ClientSecret configured: {!string.IsNullOrWhiteSpace(googleClientSecret)}", !string.IsNullOrWhiteSpace(googleClientId), !string.IsNullOrWhiteSpace(googleClientSecret));

        if (string.IsNullOrWhiteSpace(googleClientId) || string.IsNullOrWhiteSpace(googleClientSecret)
            || googleClientId.StartsWith("<") || googleClientSecret.StartsWith("<"))
        {
            _logger.LogWarning("Google OAuth login attempted but credentials are not configured");

            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.OAuthLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = "Google OAuth login attempted but not configured",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Warning
            });

            _logger.LogInformation("Returning BadRequest for Google login");
            return BadRequest(new { error = "Google authentication is not configured on this server. Please contact your administrator or use email/password login." });
        }

        var redirect = HttpContext.Request.Query["redirect"].ToString();

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback)),
            Items =
            {
                { "returnUrl", returnUrl ?? string.Empty },
                { "redirect", redirect ?? "/elections" }
            }
        };

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.OAuthLoginInitiated,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = $"Google OAuth login initiated with return URL: {returnUrl ?? "default"}",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Gets information about the currently authenticated user.
    /// </summary>
    /// <returns>The current user's information if authenticated, or an error if not.</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized(new { error = "Not authenticated" });
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        // Check if user is a super admin by comparing email with configured super admin emails
        var isSuperAdmin = !string.IsNullOrEmpty(user.Email)
            && _superAdminSettings.Emails.Any(e => string.Equals(e, user.Email, StringComparison.OrdinalIgnoreCase));

        return Ok(new
        {
            email = user.Email,
            name = user.DisplayName,
            authMethod = user.AuthMethod,
            isSuperAdmin
        });
    }

    /// <summary>
    /// Handles the OAuth callback from Google with state parameter validation for CSRF protection.
    /// </summary>
    /// <returns>A redirect to the frontend with the JWT token.</returns>
    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        string? returnUrl = null;
        try
        {

            // Authenticate the external cookie explicitly
            var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                _logger.LogWarning("Google callback: Failed to authenticate external scheme");
                return Redirect(GetErrorRedirectUrl(returnUrl, "Failed to retrieve login information from Google"));
            }

            var principal = authenticateResult.Principal!;

            _logger.LogInformation("Google callback: External auth succeeded, principal: {Principal}",
                principal.Identity?.Name ?? "null");

            // Extract return URL and redirect from authentication properties
            if (authenticateResult.Properties?.Items.TryGetValue("returnUrl", out var returnUrlValue) == true)
            {
                returnUrl = returnUrlValue;
            }

            string redirectUrl;
            if (authenticateResult.Properties?.Items.TryGetValue("redirect", out var redirectValue) == true && !string.IsNullOrEmpty(redirectValue))
            {
                redirectUrl = redirectValue.StartsWith("http") ? redirectValue : $"http://localhost:8095{redirectValue}";
            }
            else
            {
                redirectUrl = "http://localhost:8095/elections";
            }

            // Extract claims directly from the authenticated principal
            var email = principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Google callback: Email claim is missing from Google response");
                return Redirect(GetErrorRedirectUrl(returnUrl, "Email not provided by Google"));
            }

            var googleId = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var displayName = principal.FindFirstValue(ClaimTypes.Name) ??
                              principal.FindFirstValue(ClaimTypes.GivenName);

            var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

            var (user, _) = await ProcessGoogleUserAsync(
                email,
                googleId!,
                displayName,
                clientIp,
                userAgent,
                $"Google OAuth login successful for user {email}"
            );

            // Clean up the external authentication cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            _logger.LogInformation("Google callback: Successfully authenticated user {Email}, redirecting to {Url}", email, redirectUrl);
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google callback: Unexpected error during Google authentication");
            return Redirect(GetErrorRedirectUrl(returnUrl, "An unexpected error occurred during authentication"));
        }
    }

    /// <summary>
    /// Authenticates a user via Google One Tap by validating a Google ID token credential.
    /// </summary>
    /// <param name="request">The request containing the Google ID token credential.</param>
    /// <returns>The authentication response with user info if successful, or an error if validation fails.</returns>
    [HttpPost("google/one-tap")]
    public async Task<IActionResult> GoogleOneTap([FromBody] GoogleOneTapRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var googleClientId = _configuration["Google:ClientId"];
        if (string.IsNullOrWhiteSpace(googleClientId) || googleClientId.StartsWith("<"))
        {
            _logger.LogWarning("Google One Tap attempted but Google Client ID is not configured");
            return BadRequest(new { error = "Google authentication is not configured on this server." });
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            };
            payload = await GoogleJsonWebSignature.ValidateAsync(request.Credential, settings);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Google One Tap: Invalid Google ID token");

            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.OAuthLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = "Google One Tap: Invalid ID token",
                IsSuspicious = true,
                Severity = SecurityEventSeverity.Warning
            });

            return BadRequest(new { error = "Invalid Google credential." });
        }

        var email = payload.Email;
        if (string.IsNullOrEmpty(email))
        {
            return BadRequest(new { error = "Email not provided by Google." });
        }

        var googleId = payload.Subject;
        var displayName = payload.Name ?? payload.GivenName;

        try
        {
            var (user, _) = await ProcessGoogleUserAsync(
                email,
                googleId,
                displayName,
                clientIp,
                userAgent,
                $"Google One Tap login successful for user {email}"
            );

            return Ok(new AuthResponse
            {
                Token = null,
                RefreshToken = null,
                Email = user.Email!,
                Name = user.DisplayName,
                AuthMethod = "Google",
                Requires2FA = false
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Google One Tap: Error processing user {Email}", email);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user using the Telegram Login Widget for officer / teller flows.
    /// </summary>
    [HttpPost("telegram")]
    public async Task<IActionResult> TelegramLogin([FromBody] TelegramLoginRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var botToken = _configuration["Telegram:BotToken"];
        if (string.IsNullOrWhiteSpace(botToken) || botToken.StartsWith("<"))
        {
            _logger.LogWarning("Telegram login attempted but Telegram bot token is not configured");
            return BadRequest(new { error = "Telegram authentication is not configured on this server." });
        }

        if (!ValidateTelegramHash(request, botToken))
        {
            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.OAuthLoginFailure,
                IpAddress = clientIp,
                UserAgent = userAgent,
                Details = "Telegram login: invalid hash",
                IsSuspicious = true,
                Severity = SecurityEventSeverity.Warning
            });

            return BadRequest(new { error = "Invalid Telegram data." });
        }

        var authAgeSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds() - request.AuthDate;
        if (authAgeSeconds > 86400)
        {
            return BadRequest(new { error = "Telegram login request expired." });
        }

        try
        {
            var (user, _) = await ProcessTelegramUserAsync(request, clientIp, userAgent);

            return Ok(new AuthResponse
            {
                Token = null,
                RefreshToken = null,
                Email = user.Email!,
                Name = user.DisplayName,
                AuthMethod = "Telegram",
                Requires2FA = false
            });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "Telegram login: error processing user {TelegramId}", request.Id);
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user using Facebook for officer / teller flows.
    /// </summary>
    [HttpPost("facebook")]
    public async Task<IActionResult> FacebookLogin([FromBody] FacebookLoginRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        try
        {
            var client = _httpClientFactory.CreateClient("Facebook");
            using var fbRequest = new HttpRequestMessage(HttpMethod.Get, "/me?fields=id,email,name");
            fbRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);
            var response = await client.SendAsync(fbRequest);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook API returned non-success for teller auth: {Status}", response.StatusCode);
                return BadRequest(new { error = "Invalid Facebook token." });
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            if (!doc.RootElement.TryGetProperty("email", out var emailElement))
            {
                return BadRequest(new { error = "Email not provided by Facebook." });
            }

            var email = emailElement.GetString();
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "Email not provided by Facebook." });
            }

            var fbId = doc.RootElement.TryGetProperty("id", out var idElement) ? idElement.GetString() : null;
            var displayName = doc.RootElement.TryGetProperty("name", out var nameElement) ? nameElement.GetString() : null;

            if (string.IsNullOrEmpty(fbId))
            {
                return BadRequest(new { error = "ID not provided by Facebook." });
            }

            var (user, _) = await ProcessExternalUserAsync(
                email,
                "Facebook",
                fbId,
                displayName,
                clientIp,
                userAgent,
                $"Facebook login successful for user {email}"
            );

            return Ok(new AuthResponse
            {
                Token = null,
                RefreshToken = null,
                Email = user.Email!,
                Name = user.DisplayName,
                AuthMethod = "Facebook",
                Requires2FA = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Facebook login: error processing user");
            return BadRequest(new { error = "Error authenticating with Facebook." });
        }
    }

    /// <summary>
    /// Authenticates a user using Kakao for officer / teller flows.
    /// </summary>
    [HttpPost("kakao")]
    public async Task<IActionResult> KakaoLogin([FromBody] KakaoLoginRequest request)
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        try
        {
            var client = _httpClientFactory.CreateClient("Kakao");
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/v2/user/me");
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", request.AccessToken);

            var response = await client.SendAsync(httpRequest);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Kakao API returned non-success for teller auth: {Status}", response.StatusCode);
                return BadRequest(new { error = "Invalid Kakao token." });
            }

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            string? email = null;
            string? displayName = null;
            var kakaoId = doc.RootElement.TryGetProperty("id", out var idElement) ? idElement.GetInt64().ToString() : null;

            if (doc.RootElement.TryGetProperty("kakao_account", out var account))
            {
                if (account.TryGetProperty("email", out var emailEl))
                    email = emailEl.GetString();

                if (account.TryGetProperty("profile", out var profile) && profile.TryGetProperty("nickname", out var nameEl))
                    displayName = nameEl.GetString();
            }

            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { error = "Email not provided by Kakao." });
            }
            if (string.IsNullOrEmpty(kakaoId))
            {
                return BadRequest(new { error = "ID not provided by Kakao." });
            }

            var (user, _) = await ProcessExternalUserAsync(
                email,
                "Kakao",
                kakaoId,
                displayName,
                clientIp,
                userAgent,
                $"Kakao login successful for user {email}"
            );

            return Ok(new AuthResponse
            {
                Token = null,
                RefreshToken = null,
                Email = user.Email!,
                Name = user.DisplayName,
                AuthMethod = "Kakao",
                Requires2FA = false
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kakao login: error processing user");
            return BadRequest(new { error = "Error authenticating with Kakao." });
        }
    }

    private async Task<(AppUser user, bool isNewUser)> ProcessExternalUserAsync(
        string email,
        string provider,
        string providerKey,
        string? displayName,
        string? clientIp,
        string? userAgent,
        string eventDetails)
    {
        var user = await _userManager.FindByEmailAsync(email);
        bool isNewUser = false;

        if (user == null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                AuthMethod = provider
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create user {Email}: {Errors}",
                    email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                throw new InvalidOperationException("Failed to create user account.");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Officer");
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to assign Officer role to user {Email}: {Errors}",
                    email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation("Created new user {Email} with {Provider} authentication", email, provider);
            isNewUser = true;
        }
        else
        {
            if (!string.IsNullOrEmpty(displayName) && string.IsNullOrEmpty(user.DisplayName))
            {
                user.DisplayName = displayName;
                await _userManager.UpdateAsync(user);
            }
        }

        var logins = await _userManager.GetLoginsAsync(user);
        if (!logins.Any(l => l.LoginProvider == provider && l.ProviderKey == providerKey))
        {
            var addLoginResult = await _userManager.AddLoginAsync(user, new UserLoginInfo(provider, providerKey, provider));
            if (!addLoginResult.Succeeded)
            {
                _logger.LogWarning("Failed to add {Provider} login to user {Email}: {Errors}",
                    provider, email, string.Join(", ", addLoginResult.Errors.Select(e => e.Description)));
            }

            var methods = string.IsNullOrEmpty(user.AuthMethod) ? new List<string>() : new List<string>(user.AuthMethod.Split(','));
            if (!methods.Contains(provider))
            {
                methods.Add(provider);
                user.AuthMethod = string.Join(",", methods);
                await _userManager.UpdateAsync(user);
            }
        }

        var token = _jwtTokenService.GenerateToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = _jwtTokenService.CreateRefreshToken(user.Id, refreshToken);
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        SecureCookieMiddleware.SetAuthCookies(
            HttpContext,
            token,
            refreshToken,
            user.Email!,
            user.DisplayName,
            user.AuthMethod ?? provider,
            HttpContext.Request.IsHttps
        );

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.OAuthLoginSuccess,
            UserId = user.Id,
            Email = email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = eventDetails,
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        await _remoteLogService.SendLogAsync($"Full teller login via {provider}", user.DisplayName ?? user.Email, null);

        return (user, isNewUser);
    }
    private async Task<(AppUser user, bool isNewUser)> ProcessGoogleUserAsync(
        string email,
        string googleId,
        string? displayName,
        string? clientIp,
        string? userAgent,
        string eventDetails)
    {
        var user = await _userManager.FindByEmailAsync(email);
        bool isNewUser = false;

        if (user == null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                GoogleId = googleId,
                AuthMethod = "Google"
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create user {Email}: {Errors}",
                    email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                throw new InvalidOperationException("Failed to create user account.");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Officer");
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to assign Officer role to user {Email}: {Errors}",
                    email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation("Created new user {Email} with Google authentication", email);
            isNewUser = true;
            await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleId, "Google"));
        }
        else
        {
            if (string.IsNullOrEmpty(user.GoogleId))
            {
                user.GoogleId = googleId;
                user.AuthMethod = "Google";
                if (!string.IsNullOrEmpty(displayName))
                {
                    user.DisplayName = displayName;
                }
                await _userManager.UpdateAsync(user);
                await _userManager.AddLoginAsync(user, new UserLoginInfo("Google", googleId, "Google"));
                _logger.LogInformation("Linked Google account to existing user {Email}", email);
            }
            else if (!string.IsNullOrEmpty(displayName) && string.IsNullOrEmpty(user.DisplayName))
            {
                user.DisplayName = displayName;
                await _userManager.UpdateAsync(user);
            }
        }

        var token = _jwtTokenService.GenerateToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = _jwtTokenService.CreateRefreshToken(user.Id, refreshToken);
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        SecureCookieMiddleware.SetAuthCookies(
            HttpContext,
            token,
            refreshToken,
            user.Email!,
            user.DisplayName,
            user.AuthMethod ?? "Google",
            HttpContext.Request.IsHttps
        );

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.OAuthLoginSuccess,
            UserId = user.Id,
            Email = email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = eventDetails,
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        await _remoteLogService.SendLogAsync($"Full teller login via Google" + (isNewUser ? " (new user)" : ""), user.DisplayName ?? user.Email, null);

        return (user, isNewUser);
    }

    private async Task<(AppUser user, bool isNewUser)> ProcessTelegramUserAsync(
        TelegramLoginRequest request,
        string? clientIp,
        string? userAgent)
    {
        var telegramId = request.Id.ToString();
        var displayName = $"{request.FirstName} {request.LastName}".Trim();
        if (string.IsNullOrWhiteSpace(displayName))
        {
            displayName = request.Username ?? $"Telegram {telegramId}";
        }

        var email = !string.IsNullOrWhiteSpace(request.Username)
            ? $"{request.Username}@telegram.local"
            : $"telegram_{telegramId}@telegram.local";

        var user = await _userManager.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramId);
        bool isNewUser = false;

        if (user == null)
        {
            user = new AppUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                TelegramId = telegramId,
                AuthMethod = "Telegram"
            };

            var createResult = await _userManager.CreateAsync(user);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create Telegram user {TelegramId}: {Errors}",
                    telegramId, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                throw new InvalidOperationException("Failed to create user account.");
            }

            var roleResult = await _userManager.AddToRoleAsync(user, "Officer");
            if (!roleResult.Succeeded)
            {
                _logger.LogWarning("Failed to assign Officer role to Telegram user {TelegramId}: {Errors}",
                    telegramId, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
            }

            _logger.LogInformation("Created new user for Telegram ID {TelegramId}", telegramId);
            isNewUser = true;
        }
        else
        {
            var needsUpdate = false;
            if (string.IsNullOrEmpty(user.TelegramId))
            {
                user.TelegramId = telegramId;
                needsUpdate = true;
                await _userManager.AddLoginAsync(user, new UserLoginInfo("Telegram", telegramId, "Telegram"));
            }

            if (string.IsNullOrEmpty(user.DisplayName) && !string.IsNullOrEmpty(displayName))
            {
                user.DisplayName = displayName;
                needsUpdate = true;
            }

            user.AuthMethod = "Telegram";

            if (needsUpdate)
            {
                await _userManager.UpdateAsync(user);
            }
        }

        var token = _jwtTokenService.GenerateToken(user);
        var refreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenEntity = _jwtTokenService.CreateRefreshToken(user.Id, refreshToken);
        _context.RefreshTokens.Add(refreshTokenEntity);
        await _context.SaveChangesAsync();

        SecureCookieMiddleware.SetAuthCookies(
            HttpContext,
            token,
            refreshToken,
            user.Email!,
            user.DisplayName,
            user.AuthMethod ?? "Telegram",
            HttpContext.Request.IsHttps
        );

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.OAuthLoginSuccess,
            UserId = user.Id,
            Email = user.Email,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = $"Telegram login successful for Telegram ID {telegramId}",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        await _remoteLogService.SendLogAsync($"Full teller login via Telegram" + (isNewUser ? " (new user)" : ""), user.DisplayName ?? user.Email, null);

        return (user, isNewUser);
    }

    private static string BuildTelegramDataCheckString(TelegramLoginRequest request)
    {
        var fields = new SortedDictionary<string, string>
        {
            ["auth_date"] = request.AuthDate.ToString(),
            ["first_name"] = request.FirstName,
            ["id"] = request.Id.ToString()
        };

        if (!string.IsNullOrEmpty(request.LastName)) fields["last_name"] = request.LastName;
        if (!string.IsNullOrEmpty(request.PhotoUrl)) fields["photo_url"] = request.PhotoUrl;
        if (!string.IsNullOrEmpty(request.Username)) fields["username"] = request.Username;

        return string.Join("\n", fields.Select(kv => $"{kv.Key}={kv.Value}"));
    }

    private static string ComputeTelegramHash(string dataCheckString, string botToken)
    {
        using var sha256 = SHA256.Create();
        var secretKey = sha256.ComputeHash(Encoding.UTF8.GetBytes(botToken));

        using var hmac = new HMACSHA256(secretKey);
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(dataCheckString));
        return Convert.ToHexString(computedHash).ToLowerInvariant();
    }

    private bool ValidateTelegramHash(TelegramLoginRequest request, string botToken)
    {
        var dataCheckString = BuildTelegramDataCheckString(request);
        var expectedHash = ComputeTelegramHash(dataCheckString, botToken);
        return string.Equals(expectedHash, request.Hash, StringComparison.OrdinalIgnoreCase);
    }

    private string GetFrontendUrl(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            return returnUrl;
        }

        var frontendBaseUrl = _configuration["Frontend:BaseUrl"]?.Trim();
        if (!string.IsNullOrEmpty(frontendBaseUrl))
        {
            return frontendBaseUrl + "/auth/google/callback";
        }

        // Fallback to localhost for development if not configured
        return "http://localhost:8095/auth/google/callback";
    }

    /// <summary>
    /// Logs out the current user by clearing authentication cookies.
    /// </summary>
    /// <returns>A success message confirming the user has been logged out.</returns>
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var clientIp = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.Logout,
            UserId = userId,
            IpAddress = clientIp,
            UserAgent = userAgent,
            Details = "User logged out",
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });

        SecureCookieMiddleware.ClearAuthCookies(HttpContext);

        return Ok(new { message = "Logged out successfully" });
    }

    private string GetErrorRedirectUrl(string? returnUrl, string errorMessage)
    {
        var frontendUrl = GetFrontendUrl(returnUrl);
        var baseUrl = frontendUrl.Split('?')[0].Replace("/auth/google/callback", "/login");
        return $"{baseUrl}?error={Uri.EscapeDataString(errorMessage)}&mode=officer";
    }

    /// <summary>
    /// Generates a cryptographically secure random code verifier for PKCE (Proof Key for Code Exchange).
    /// </summary>
    /// <returns>A base64url-encoded random string between 43-128 characters.</returns>
    public static string GenerateCodeVerifier()
    {
        var bytes = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    /// <summary>
    /// Generates a code challenge from a code verifier using SHA256 hashing.
    /// </summary>
    /// <param name="codeVerifier">The code verifier to hash.</param>
    /// <returns>A base64url-encoded SHA256 hash of the code verifier.</returns>
    public static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(codeVerifier);
        var hash = sha256.ComputeHash(bytes);
        return Base64UrlEncode(hash);
    }

    /// <summary>
    /// Encodes a byte array to base64url format (URL-safe base64).
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A base64url-encoded string.</returns>
    public static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }


}

