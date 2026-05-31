using System.Security.Claims;
using Backend.Entities;
using Backend.Identity;

namespace Backend.Services.Auth;

public interface IJwtTokenService
{
    string GenerateToken(AppUser user);
    string GenerateTellerToken(Guid electionGuid);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
    RefreshToken CreateRefreshToken(string userId, string token);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
