using Backend.DTOs.Auth;

namespace Backend.Services.Auth;

/// <summary>
/// SuperAdmin-issued one-time invites for creating a single local email/password account.
/// </summary>
public interface IAccountInviteService
{
    Task<AccountInviteCreatedDto> CreateAsync(string createdByUserId);

    Task<AccountInviteStatusDto> PeekAsync(string? token);

    Task<(bool Success, string? Error, AuthResponse? Response)> RedeemAsync(RegisterWithInviteRequest request);
}
