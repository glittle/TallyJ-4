using System.Security.Claims;
using Backend.Domain.Entities;
using Backend.Domain.Identity;

namespace Backend.Application.Services.Auth;

public interface IJwtTokenService
{
    string GenerateToken(AppUser user);
    string GenerateTellerToken(Guid electionGuid);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
    RefreshToken CreateRefreshToken(string userId, string token);
    ClaimsPrincipal GetPrincipalFromExpiredToken(string token);
}
