using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TallyJ4.Application.DTOs.Auth;
using TallyJ4.Application.Services.Auth;
using TallyJ4.Domain;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Identity;
using TallyJ4.DTOs.Security;
using TallyJ4.Middleware;
using TallyJ4.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;

namespace TallyJ4.Backend.Controllers;

/// <summary>
/// Controller for handling authentication and authorization operations including user registration,
/// login, password management, two-factor authentication, and role management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LocalAuthService _localAuthService;
    private readonly PasswordResetService _passwordResetService;
    private readonly TwoFactorService _twoFactorService;
    private readonly JwtTokenService _jwtTokenService;
    private readonly MainDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly OAuthStateService _oauthStateService;
    private readonly ISecurityAuditService _securityAuditService;

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
    /// <param name="oauthStateService">Service for managing OAuth state parameters.</param>
    /// <param name="securityAuditService">Service for logging security events.</param>
    public AuthController(
        LocalAuthService localAuthService,
        PasswordResetService passwordResetService,
        TwoFactorService twoFactorService,
        JwtTokenService jwtTokenService,
        MainDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AuthController> logger,
        IConfiguration configuration,
        SignInManager<AppUser> signInManager,
        OAuthStateService oauthStateService,
        ISecurityAuditService securityAuditService)
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
        _oauthStateService = oauthStateService;
        _securityAuditService = securityAuditService;
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
                Severity = TallyJ4.Domain.SecurityEventSeverity.Info
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

        // Return response without tokens (tokens are in httpOnly cookies)
        return Ok(new AuthResponse
        {
            Token = "", // Tokens are in httpOnly cookies, not returned in response
            RefreshToken = "",
            Email = response.Email,
            Name = response.Name,
            AuthMethod = response.AuthMethod,
            Requires2FA = response.Requires2FA
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

        return Ok(response);
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token and sets secure cookies.
    /// </summary>
    /// <param name="request">The refresh token request containing the refresh token.</param>
    /// <returns>New access and refresh tokens if successful, or an error if the refresh token is invalid.</returns>
    [HttpPost("refreshToken")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var tokenHash = _jwtTokenService.HashRefreshToken(request.RefreshToken);
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

        // Set secure cookies with new tokens
        SecureCookieMiddleware.SetAuthCookies(
            HttpContext,
            newToken,
            newRefreshToken,
            user.Email!,
            user.DisplayName,
            user.AuthMethod ?? "Local",
            HttpContext.Request.IsHttps
        );

        return Ok(new AuthResponse
        {
            Token = "", // Tokens are in httpOnly cookies, not returned in response
            RefreshToken = "",
            Email = user.Email!,
            Name = user.DisplayName,
            AuthMethod = user.AuthMethod,
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

        var googleClientId = _configuration["Google:ClientId"];
        var googleClientSecret = _configuration["Google:ClientSecret"];

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

            return BadRequest(new { error = "Google authentication is not configured on this server. Please contact your administrator or use email/password login." });
        }

        // Generate PKCE code verifier and challenge for enhanced security
        var codeVerifier = GenerateCodeVerifier();
        var codeChallenge = GenerateCodeChallenge(codeVerifier);

        // Generate state parameter for CSRF protection
        var state = _oauthStateService.GenerateState(returnUrl);

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback)),
            Items =
            {
                { "returnUrl", returnUrl ?? string.Empty },
                { "code_verifier", codeVerifier }
            }
        };

        // Add PKCE and state parameters to the OAuth request
        properties.Parameters["code_challenge"] = codeChallenge;
        properties.Parameters["code_challenge_method"] = "S256";
        properties.Parameters["state"] = state;

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
    /// Handles the OAuth callback from Google with state parameter validation for CSRF protection.
    /// </summary>
    /// <returns>A redirect to the frontend with the JWT token.</returns>
    [HttpGet("google/callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        string? returnUrl = null;
        try
        {
            // Validate state parameter for CSRF protection
            var state = HttpContext.Request.Query["state"].ToString();
            if (string.IsNullOrEmpty(state))
            {
                _logger.LogWarning("Google callback: Missing state parameter");

                await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
                {
                    EventType = SecurityEventType.OAuthCallbackValidationFailure,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
                    Details = "Google OAuth callback failed: missing state parameter",
                    IsSuspicious = true,
                    Severity = SecurityEventSeverity.Warning
                });

                return Redirect(GetErrorRedirectUrl(null, "Invalid OAuth request - missing state parameter"));
            }

            var validatedReturnUrl = _oauthStateService.ValidateState(state);
            if (validatedReturnUrl == null)
            {
                _logger.LogWarning("Google callback: Invalid or expired state parameter: {State}", state);

                await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
                {
                    EventType = SecurityEventType.OAuthCallbackValidationFailure,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
                    Details = $"Google OAuth callback failed: invalid/expired state parameter - {state}",
                    IsSuspicious = true,
                    Severity = SecurityEventSeverity.Warning
                });

                return Redirect(GetErrorRedirectUrl(null, "Invalid OAuth request - state parameter validation failed"));
            }

            returnUrl = validatedReturnUrl;

            // Authenticate the external cookie explicitly
            var authenticateResult = await HttpContext.AuthenticateAsync(IdentityConstants.ExternalScheme);
            if (!authenticateResult.Succeeded || authenticateResult.Principal == null)
            {
                _logger.LogWarning("Google callback: Failed to authenticate external scheme");
                return Redirect(GetErrorRedirectUrl(returnUrl, "Failed to retrieve login information from Google"));
            }

            _logger.LogInformation("Google callback: External auth succeeded, principal: {Principal}",
                authenticateResult.Principal?.Identity?.Name ?? "null");

            // Extract return URL from authentication properties (fallback)
            if (string.IsNullOrEmpty(returnUrl) && authenticateResult.Properties?.Items.TryGetValue("returnUrl", out var returnUrlValue) == true)
            {
                returnUrl = returnUrlValue;
            }

            // Extract claims directly from the authenticated principal
            var email = authenticateResult.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
            {
                _logger.LogWarning("Google callback: Email claim is missing from Google response");
                return Redirect(GetErrorRedirectUrl(returnUrl, "Email not provided by Google"));
            }

            var googleId = authenticateResult.Principal.FindFirstValue(ClaimTypes.NameIdentifier);
            var displayName = authenticateResult.Principal.FindFirstValue(ClaimTypes.Name) ??
                              authenticateResult.Principal.FindFirstValue(ClaimTypes.GivenName);

            var user = await _userManager.FindByEmailAsync(email);
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
                    _logger.LogError("Google callback: Failed to create user {Email}: {Errors}",
                        email, string.Join(", ", createResult.Errors.Select(e => e.Description)));
                    return Redirect(GetErrorRedirectUrl(returnUrl, "Failed to create user account"));
                }

                var roleResult = await _userManager.AddToRoleAsync(user, "Officer");
                if (!roleResult.Succeeded)
                {
                    _logger.LogWarning("Google callback: Failed to assign Officer role to user {Email}: {Errors}",
                        email, string.Join(", ", roleResult.Errors.Select(e => e.Description)));
                }

                _logger.LogInformation("Google callback: Created new user {Email} with Google authentication", email);
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
                    _logger.LogInformation("Google callback: Linked Google account to existing user {Email}", email);
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

            // Set secure cookies with authentication data
            SecureCookieMiddleware.SetAuthCookies(
                HttpContext,
                token,
                refreshToken,
                user.Email!,
                user.DisplayName,
                user.AuthMethod ?? "Google",
                HttpContext.Request.IsHttps
            );

            // Clean up the external authentication cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var frontendUrl = GetFrontendUrl(returnUrl);
            var redirectUrl = $"{frontendUrl}/success";

            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.OAuthLoginSuccess,
                UserId = user.Id,
                Email = email,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent = HttpContext.Request.Headers.UserAgent.ToString(),
                Details = $"Google OAuth login successful for user {email}",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Info
            });

            _logger.LogInformation("Google callback: Successfully authenticated user {Email}, redirecting to {Url}", email, frontendUrl);
            return Redirect(redirectUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Google callback: Unexpected error during Google authentication");
            return Redirect(GetErrorRedirectUrl(returnUrl, "An unexpected error occurred during authentication"));
        }
    }

    private string GetFrontendUrl(string? returnUrl)
    {
        if (!string.IsNullOrEmpty(returnUrl) && Uri.TryCreate(returnUrl, UriKind.Absolute, out var uri))
        {
            return returnUrl;
        }

        var frontendBaseUrl = _configuration["Frontend:BaseUrl"];
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
    /// <returns>A success message indicating logout was successful.</returns>
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
    /// Generates a cryptographically secure random code verifier for PKCE.
    /// </summary>
    /// <returns>A base64url-encoded random string of 43-128 characters.</returns>
    internal static string GenerateCodeVerifier()
    {
        var randomBytes = new byte[32]; // 256 bits
        RandomNumberGenerator.Fill(randomBytes);
        return Base64UrlEncode(randomBytes);
    }

    /// <summary>
    /// Generates a code challenge from a code verifier using SHA256.
    /// </summary>
    /// <param name="codeVerifier">The code verifier string.</param>
    /// <returns>A base64url-encoded SHA256 hash of the code verifier.</returns>
    internal static string GenerateCodeChallenge(string codeVerifier)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    /// <summary>
    /// Encodes a byte array to base64url format (URL-safe base64).
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>A base64url-encoded string.</returns>
    internal static string Base64UrlEncode(byte[] bytes)
    {
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
