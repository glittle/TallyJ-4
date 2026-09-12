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

    /// <summary>
    /// Election-scoped <c>OnlineVoter.VoterId</c> for a kiosk code. Person letters
    /// stay unique per election; the login window row must not be shared globally.
    /// </summary>
    public static string ToVoterId(Guid electionGuid, string code)
    {
        var normalized = code.Trim().ToUpperInvariant();
        return $"{normalized}.{electionGuid:N}";
    }

    public static bool TryParseVoterId(string? voterId, out Guid electionGuid, out string code)
    {
        electionGuid = default;
        code = string.Empty;
        if (string.IsNullOrWhiteSpace(voterId))
        {
            return false;
        }

        var dot = voterId.LastIndexOf('.');
        if (dot <= 0 || voterId.Length - dot != 33)
        {
            return false;
        }

        if (!Guid.TryParseExact(voterId[(dot + 1)..], "N", out electionGuid))
        {
            return false;
        }

        code = voterId[..dot].Trim().ToUpperInvariant();
        return code.Length > 0;
    }

    /// <summary>
    /// True when <paramref name="voterId"/> is the short letters or the
    /// election-scoped kiosk <see cref="ToVoterId"/> for this person.
    /// </summary>
    public static bool PersonMatchesVoterId(Guid electionGuid, string? kioskCode, string voterId)
    {
        if (!HasLiveCode(kioskCode))
        {
            return false;
        }

        if (string.Equals(kioskCode, voterId, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return TryParseVoterId(voterId, out var parsedElection, out var code)
               && parsedElection == electionGuid
               && string.Equals(kioskCode, code, StringComparison.OrdinalIgnoreCase);
    }
}
