using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Backend.DTOs.Auth;
using Backend.Services.Auth;
using Backend.Authorization;
using Backend;
using Backend.Configuration;
using Backend.Context;
using Backend.Identity;
using Backend.DTOs.Security;
using Backend.Helpers;
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
/// Implementation is split across partial files by concern.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public partial class AuthController : ControllerBase
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
    private readonly IHostEnvironment _hostEnvironment;
    private readonly SignInManager<AppUser> _signInManager;
    private readonly SuperAdminSettings _superAdminSettings;
    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ISecurityAuditService _securityAuditService;
    private readonly IRemoteLogService _remoteLogService;
    private readonly IComputerAssignmentService _assignmentService;
    private readonly IAccountService _accountService;

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
    /// <param name="assignmentService">Tracks active main teller connections for guest login eligibility.</param>
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
        IHostEnvironment hostEnvironment,
        SignInManager<AppUser> signInManager,
        IOptions<SuperAdminSettings> superAdminSettings,
        IHttpClientFactory httpClientFactory,
        ISecurityAuditService securityAuditService,
        IRemoteLogService remoteLogService,
        IComputerAssignmentService assignmentService,
        IAccountService accountService)
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
        _hostEnvironment = hostEnvironment;
        _signInManager = signInManager;
        _superAdminSettings = superAdminSettings.Value;
        _httpClientFactory = httpClientFactory;
        _accountService = accountService;
        _securityAuditService = securityAuditService;
        _remoteLogService = remoteLogService;
        _assignmentService = assignmentService;
    }

}
