using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TallyJ4.Application.DTOs.Auth;
using TallyJ4.Application.Services.Auth;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Identity;

namespace TallyJ4.Backend.Controllers;

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

    public AuthController(
        LocalAuthService localAuthService,
        PasswordResetService passwordResetService,
        TwoFactorService twoFactorService,
        JwtTokenService jwtTokenService,
        MainDbContext context,
        UserManager<AppUser> userManager,
        RoleManager<IdentityRole> roleManager,
        ILogger<AuthController> logger)
    {
        _localAuthService = localAuthService;
        _passwordResetService = passwordResetService;
        _twoFactorService = twoFactorService;
        _jwtTokenService = jwtTokenService;
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

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
            Requires2FA = false
        });
    }

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

    [Authorize(Policy = "AdminOnly")]
    [HttpGet("roles/all")]
    public IActionResult GetAllRoles()
    {
        var roles = _roleManager.Roles.Select(r => r.Name).ToList();
        return Ok(new { roles });
    }
}
