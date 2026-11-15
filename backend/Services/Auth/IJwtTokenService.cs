using System.Security.Claims;
using Backend.Entities;
using Backend.Identity;

namespace Backend.Services.Auth;

public interface IJwtTokenService
{
    string GenerateToken(AppUser user);

    /// <summary>
    /// Guest teller session for one election. Sets <c>isTeller=true</c>,
    /// <c>authMethod=AccessCode</c>, and <c>electionGuid</c>. This is the
    /// shared-passcode login, not an account teller.
    /// Known tellers use <see cref="GenerateToken"/>.
    /// </summary>
    string GenerateGuestTellerToken(Guid electionGuid);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
    RefreshToken CreateRefreshToken(string userId, string token);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
