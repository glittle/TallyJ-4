using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Backend.Entities;
using Microsoft.IdentityModel.Tokens;

namespace Backend.Services;

public partial class OnlineVotingService
{
    /// <summary>
    /// Generates a JWT token for an authenticated online voter.
    /// </summary>
    /// <param name="onlineVoter">The online voter to generate the token for.</param>
    /// <returns>A JWT token string valid for 24 hours.</returns>
    private string GenerateJwtToken(OnlineVoter onlineVoter)
    {
        var key = _configuration["Jwt:Key"] ?? "DefaultSecretKeyForDevelopmentPurposesOnly123456789";
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("voterId", onlineVoter.VoterId),
            new Claim("voterIdType", onlineVoter.VoterIdType),
            new Claim("voterType", "online")
        };

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"] ?? "Backend",
            audience: _configuration["Jwt:Audience"] ?? "BackendClient",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(24),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Notifies existing browser sessions for this voter id that another device just logged in
    /// (v3 VoterPersonalHub Login parity). Failures are logged inside the notification service.
    /// </summary>
    private Task NotifyLoginElsewhereAsync(string voterId)
    {
        return _signalRNotificationService.NotifyVoterLoginElsewhereAsync(voterId);
    }

    private static string SanitizeForLog(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return input.Replace("\r", "").Replace("\n", "");
    }
}
