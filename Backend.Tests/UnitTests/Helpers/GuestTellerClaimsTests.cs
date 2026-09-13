using System.Security.Claims;
using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class GuestTellerClaimsTests
{
    [Fact]
    public void IsGuestTeller_RequiresIsTellerAndAccessCode()
    {
        var guest = Principal(
            (GuestTellerClaims.IsTellerClaimType, "true"),
            (GuestTellerClaims.AuthMethodClaimType, GuestTellerClaims.AccessCodeAuthMethod));
        Assert.True(GuestTellerClaims.IsGuestTeller(guest));
    }

    [Fact]
    public void IsGuestTeller_IsTellerWithoutAccessCode_IsFalse()
    {
        var user = Principal(
            (GuestTellerClaims.IsTellerClaimType, "true"),
            (GuestTellerClaims.AuthMethodClaimType, "Local"));
        Assert.False(GuestTellerClaims.IsGuestTeller(user));
    }

    [Fact]
    public void IsGuestTeller_AccessCodeWithoutIsTeller_IsFalse()
    {
        var user = Principal(
            (GuestTellerClaims.AuthMethodClaimType, GuestTellerClaims.AccessCodeAuthMethod));
        Assert.False(GuestTellerClaims.IsGuestTeller(user));
    }

    [Fact]
    public void IsGuestTeller_NullUser_IsFalse()
    {
        Assert.False(GuestTellerClaims.IsGuestTeller(null));
    }

    private static ClaimsPrincipal Principal(params (string type, string value)[] claims)
    {
        var identity = new ClaimsIdentity("test");
        foreach (var (type, value) in claims)
        {
            identity.AddClaim(new Claim(type, value));
        }

        return new ClaimsPrincipal(identity);
    }
}
