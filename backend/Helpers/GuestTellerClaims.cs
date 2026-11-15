using System.Security.Claims;

namespace Backend.Helpers;

/// <summary>
/// Guest tellers authenticate with an election passcode. Their JWT has
/// <c>isTeller=true</c> and <c>authMethod=AccessCode</c>
/// (see <c>JwtTokenService.GenerateGuestTellerToken</c>).
/// Known-account tellers do not get the <c>isTeller</c> claim.
/// </summary>
public static class GuestTellerClaims
{
    /// <summary>
    /// Claim type <c>isTeller</c>. Despite the name, the flag marks a guest
    /// session. Known tellers omit it.
    /// </summary>
    public const string GuestTellerFlagClaimType = "isTeller";

    public const string AuthMethodClaimType = "authMethod";
    public const string AccessCodeAuthMethod = "AccessCode";

    /// <summary>
    /// True only when both the guest flag and <c>authMethod=AccessCode</c> are set.
    /// API authorization uses this. Hub Known/Guest routing uses
    /// <see cref="HasGuestTellerFlag"/> and does not look at <c>authMethod</c>.
    /// </summary>
    public static bool IsGuestTeller(ClaimsPrincipal? user)
    {
        if (user == null)
        {
            return false;
        }

        var authMethod = user.FindFirst(AuthMethodClaimType)?.Value;

        return HasGuestTellerFlag(user)
               && string.Equals(authMethod, AccessCodeAuthMethod, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// True when <c>isTeller</c> parses as true. <c>authMethod</c> is ignored,
    /// matching Main hub and package-import hub group routing. A token with
    /// the flag and a different auth method is a guest for those hubs and is
    /// not a guest for <see cref="IsGuestTeller"/>.
    /// </summary>
    public static bool HasGuestTellerFlag(ClaimsPrincipal? user)
    {
        var claim = user?.FindFirst(GuestTellerFlagClaimType)?.Value;
        return bool.TryParse(claim, out var isGuestTeller) && isGuestTeller;
    }
}
