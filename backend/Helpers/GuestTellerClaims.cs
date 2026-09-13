using System.Security.Claims;

namespace Backend.Helpers;

/// <summary>
/// Guest tellers authenticate with an election access code. Their JWT has
/// <c>isTeller=true</c> and <c>authMethod=AccessCode</c>
/// (see <c>JwtTokenService.GenerateTellerToken</c>).
/// </summary>
public static class GuestTellerClaims
{
    public const string IsTellerClaimType = "isTeller";
    public const string AuthMethodClaimType = "authMethod";
    public const string AccessCodeAuthMethod = "AccessCode";

    public static bool IsGuestTeller(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return false;
        }

        var isTellerClaim = user.FindFirst(IsTellerClaimType)?.Value;
        var authMethod = user.FindFirst(AuthMethodClaimType)?.Value;

        return bool.TryParse(isTellerClaim, out var isGuestTeller) && isGuestTeller
               && string.Equals(authMethod, AccessCodeAuthMethod, StringComparison.OrdinalIgnoreCase);
    }
}
