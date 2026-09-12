namespace Backend.Helpers;

/// <summary>
/// Kiosk login window: tellers mint or renew a code; voters may use it for
/// <see cref="LifetimeMinutes"/> minutes. Matches v3 Setup copy and issue #182.
/// </summary>
public static class KioskCodeLifetime
{
    /// <summary>
    /// Minutes a minted or renewed kiosk code remains valid for login.
    /// </summary>
    public const int LifetimeMinutes = 15;

    /// <summary>
    /// <c>OnlineVoter.VoterIdType</c> for a kiosk / personal code.
    /// </summary>
    public const string VoterIdType = "C";

    /// <summary>
    /// v3 stored a used code as empty (not null) so it is distinct from "never assigned".
    /// </summary>
    public static bool IsConsumed(string? kioskCode) => kioskCode == string.Empty;

    public static bool HasLiveCode(string? kioskCode) =>
        !string.IsNullOrWhiteSpace(kioskCode);

    public static DateTimeOffset? ExpiresAt(DateTimeOffset? verifyCodeDate)
    {
        if (verifyCodeDate == null)
        {
            return null;
        }

        return verifyCodeDate.Value.AddMinutes(LifetimeMinutes);
    }

    public static bool IsLoginWindowOpen(DateTimeOffset? verifyCodeDate, DateTimeOffset? now = null)
    {
        var expiresAt = ExpiresAt(verifyCodeDate);
        if (expiresAt == null)
        {
            return false;
        }

        return expiresAt > (now ?? DateTimeOffset.UtcNow);
    }
}
