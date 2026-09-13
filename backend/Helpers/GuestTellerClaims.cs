using System.Security.Claims;

namespace Backend.Helpers;

/// <summary>
/// Guest tellers authenticate with an election access code. Their JWT has
/// <c>isTeller=true</c> (see <c>JwtTokenService.GenerateTellerToken</c>).
/// </summary>
public static class GuestTellerClaims
{
    public const string IsTellerClaimType = "isTeller";

    public static bool IsGuestTeller(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return false;
        }

        var claim = user.FindFirst(IsTellerClaimType)?.Value;
        return bool.TryParse(claim, out var isGuestTeller) && isGuestTeller;
    }
}
