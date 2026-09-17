using System.Security.Cryptography;
using Backend.Configuration;
using Backend.Context;
using Backend.DTOs.Auth;
using Backend.Entities;
using Backend.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Auth;

/// <summary>
/// Issues and redeems one-time local-account invites. Open <c>registerAccount</c> stays disabled;
/// this is the only email/password create path after IdP-first (#347).
/// </summary>
public class AccountInviteService : IAccountInviteService
{
    public const string InvalidInviteKey = "auth.errors.invalidInvite";
    public const int LifetimeDays = 7;

    private readonly MainDbContext _context;
    private readonly ILocalAuthService _localAuthService;
    private readonly UserManager<AppUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<AccountInviteService> _logger;

    public AccountInviteService(
        MainDbContext context,
        ILocalAuthService localAuthService,
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<AccountInviteService> logger)
    {
        _context = context;
        _localAuthService = localAuthService;
        _userManager = userManager;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
    }

    public async Task<AccountInviteCreatedDto> CreateAsync(string createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);

        var rawToken = CreateRawToken();
        var now = DateTimeOffset.UtcNow;
        var invite = new AccountInvite
        {
            Id = Guid.NewGuid(),
            TokenHash = HashToken(rawToken),
            CreatedByUserId = createdByUserId,
            CreatedAt = now,
            ExpiresAt = now.AddDays(LifetimeDays)
        };

        _context.AccountInvites.Add(invite);
        await _context.SaveChangesAsync();

        var inviteUrl = FrontendUrlResolver.Build(
            _configuration,
            _hostEnvironment,
            "/register",
            ("invite", rawToken));

        _logger.LogInformation(
            "Account invite {InviteId} issued by {UserId}, expires {ExpiresAt}",
            invite.Id,
            createdByUserId,
            invite.ExpiresAt);

        return new AccountInviteCreatedDto
        {
            Token = rawToken,
            InviteUrl = inviteUrl,
            ExpiresAt = invite.ExpiresAt
        };
    }

    public async Task<AccountInviteStatusDto> PeekAsync(string? token)
    {
        var invite = await FindUsableInviteAsync(token);
        return new AccountInviteStatusDto { Valid = invite != null };
    }

    public async Task<(bool Success, string? Error, AuthResponse? Response)> RedeemAsync(
        RegisterWithInviteRequest request)
    {
        var hash = TryHashToken(request.Token);
        if (hash == null)
        {
            return (false, InvalidInviteKey, null);
        }

        var now = DateTimeOffset.UtcNow;
        var consumed = await _context.AccountInvites
            .Where(i => i.TokenHash == hash && i.UsedAt == null && i.ExpiresAt > now)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.UsedAt, now));

        if (consumed != 1)
        {
            return (false, InvalidInviteKey, null);
        }

        var (success, error, response) = await _localAuthService.RegisterAsync(request);
        if (!success)
        {
            await _context.AccountInvites
                .Where(i => i.TokenHash == hash && i.UsedAt != null && i.CreatedUserId == null)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.UsedAt, (DateTimeOffset?)null));

            return (false, error, null);
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user != null)
        {
            await _context.AccountInvites
                .Where(i => i.TokenHash == hash)
                .ExecuteUpdateAsync(s => s.SetProperty(i => i.CreatedUserId, user.Id));
        }

        return (true, null, response);
    }

    internal static string HashToken(string rawToken)
        => Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken)));

    private static string? TryHashToken(string? rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken) || rawToken.Length > 200)
        {
            return null;
        }

        return HashToken(rawToken.Trim());
    }

    private async Task<AccountInvite?> FindUsableInviteAsync(string? token)
    {
        var hash = TryHashToken(token);
        if (hash == null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        return await _context.AccountInvites
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.TokenHash == hash && i.UsedAt == null && i.ExpiresAt > now);
    }

    private static string CreateRawToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }
}
