using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TallyJ4.Application.DTOs.Auth;
using TallyJ4.Application.Services.Auth;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Identity;
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
        SignInManager<AppUser> signInManager)
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
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">The registration request containing user details.</param>
    /// <returns>The authentication response if successful, or an error if registration fails.</returns>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var (success, error, response) = await _localAuthService.RegisterAsync(request);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(response);
    }

    /// <summary>
    /// Authenticates a user and returns access tokens.
    /// </summary>
    /// <param name="request">The login request containing email and password.</param>
    /// <returns>The authentication response with tokens if successful, or an error if login fails.</returns>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var (success, error, response) = await _localAuthService.LoginAsync(request);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(response);
    }

    /// <summary>
    /// Initiates a password reset by sending a reset email to the user.
    /// </summary>
    /// <param name="request">The forgot password request containing the user's email.</param>
    /// <returns>A success message if the email was sent, or an error if the request fails.</returns>
    [HttpPost("password/forgot")]
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
    [HttpPost("password/reset")]
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
    /// Sets up two-factor authentication for the authenticated user.
    /// </summary>
    /// <returns>The 2FA setup information including QR code and secret key.</returns>
    [Authorize]
    [HttpPost("2fa/setup")]
    public async Task<IActionResult> Setup2FA()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var (success, error, response) = await _twoFactorService.SetupAsync(userId);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(response);
    }

    /// <summary>
    /// Enables two-factor authentication for the authenticated user.
    /// </summary>
    /// <param name="request">The enable 2FA request containing the verification code.</param>
    /// <returns>A success message if 2FA was enabled, or an error if the request fails.</returns>
    [Authorize]
    [HttpPost("2fa/enable")]
    public async Task<IActionResult> Enable2FA([FromBody] Enable2FARequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var (success, error) = await _twoFactorService.EnableAsync(userId, request);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(new { message = "Two-factor authentication enabled" });
    }

    /// <summary>
    /// Disables two-factor authentication for the authenticated user.
    /// </summary>
    /// <param name="request">The disable 2FA request containing the verification code.</param>
    /// <returns>A success message if 2FA was disabled, or an error if the request fails.</returns>
    [Authorize]
    [HttpPost("2fa/disable")]
    public async Task<IActionResult> Disable2FA([FromBody] Disable2FARequest request)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var (success, error) = await _twoFactorService.DisableAsync(userId, request);

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(new { message = "Two-factor authentication disabled" });
    }

    /// <summary>
    /// Verifies a two-factor authentication code for login.
    /// </summary>
    /// <param name="request">The verify 2FA request containing email and verification code.</param>
    /// <returns>The authentication response with tokens if successful, or an error if verification fails.</returns>
    [HttpPost("2fa/verify")]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest request)
    {
        var (success, error, response) = await _localAuthService.LoginAsync(new LoginRequest
        {
            Email = request.Email,
            Password = "",
            TwoFactorCode = request.Code
        });

        if (!success)
        {
            return BadRequest(new { error });
        }

        return Ok(response);
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token.
    /// </summary>
    /// <param name="request">The refresh token request containing the refresh token.</param>
    /// <returns>New access and refresh tokens if successful, or an error if the refresh token is invalid.</returns>
    [HttpPost("refresh")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        var refreshToken = await _context.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken && !rt.IsRevoked);

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

        return Ok(new AuthResponse
        {
            Token = newToken,
            RefreshToken = newRefreshToken,
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
    [HttpGet("roles")]
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
    [HttpPost("users/{userId}/roles")]
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
    [HttpDelete("users/{userId}/roles/{roleName}")]
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
    [HttpGet("roles/all")]
    public IActionResult GetAllRoles()
    {
        var roles = _roleManager.Roles.Select(r => r.Name).ToList();
        return Ok(new { roles });
    }

    /// <summary>
    /// Initiates Google OAuth login flow.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful authentication (default: frontend origin).</param>
    /// <returns>A redirect to Google's OAuth consent screen.</returns>
    [HttpGet("google/login")]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl = null)
    {
        var googleClientId = _configuration["Google:ClientId"];
        var googleClientSecret = _configuration["Google:ClientSecret"];

        if (string.IsNullOrWhiteSpace(googleClientId) || string.IsNullOrWhiteSpace(googleClientSecret)
            || googleClientId.StartsWith("<") || googleClientSecret.StartsWith("<"))
        {
            _logger.LogWarning("Google OAuth login attempted but credentials are not configured");
            return BadRequest(new { error = "Google authentication is not configured on this server. Please contact your administrator or use email/password login." });
        }

        var properties = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleCallback)),
            Items = 
            { 
                { "returnUrl", returnUrl ?? string.Empty }
            }
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// Handles the OAuth callback from Google.
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
                return Redirect(GetErrorRedirectUrl(null, "Failed to retrieve login information from Google"));
            }

            _logger.LogInformation("Google callback: External auth succeeded, principal: {Principal}", 
                authenticateResult.Principal?.Identity?.Name ?? "null");
            
            // Extract return URL from authentication properties
            if (authenticateResult.Properties?.Items.TryGetValue("returnUrl", out var returnUrlValue) == true)
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

            // Clean up the external authentication cookie
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            var frontendUrl = GetFrontendUrl(returnUrl);
            var redirectUrl = $"{frontendUrl}?token={Uri.EscapeDataString(token)}&refreshToken={Uri.EscapeDataString(refreshToken)}&email={Uri.EscapeDataString(user.Email ?? "")}&name={Uri.EscapeDataString(user.DisplayName ?? "")}&authMethod={Uri.EscapeDataString(user.AuthMethod)}";

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

        var frontendOrigins = new[] { "http://localhost:8095", "http://localhost:5173", "http://localhost:5174" };
        return frontendOrigins[0] + "/auth/google/callback";
    }

    private string GetErrorRedirectUrl(string? returnUrl, string errorMessage)
    {
        var frontendUrl = GetFrontendUrl(returnUrl);
        var baseUrl = frontendUrl.Split('?')[0].Replace("/auth/google/callback", "/login");
        return $"{baseUrl}?error={Uri.EscapeDataString(errorMessage)}&mode=officer";
    }
}
