using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TallyJ4.Application.DTOs.Auth;
using TallyJ4.Application.Services.Auth;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly LocalAuthService _localAuthService;
    private readonly PasswordResetService _passwordResetService;
    private readonly TwoFactorService _twoFactorService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        LocalAuthService localAuthService,
        PasswordResetService passwordResetService,
        TwoFactorService twoFactorService,
        ILogger<AuthController> logger)
    {
        _localAuthService = localAuthService;
        _passwordResetService = passwordResetService;
        _twoFactorService = twoFactorService;
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
}
