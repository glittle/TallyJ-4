using Backend.Entities;

namespace Backend.Helpers;

/// <summary>
/// Whether guest tellers may discover and log in. The source of truth is
/// <see cref="Election.ListedForPublicAsOf"/>, not a separate open flag.
/// <see cref="Election.ListForPublic"/> is a derived, unpersisted view of that timestamp.
/// </summary>
public static class ElectionTellerAccessHelper
{
    /// <summary>
    /// Guest tellers may discover and log in only when
    /// <paramref name="listedForPublicAsOf"/> is set and is not still in the
    /// future. Null means closed. <paramref name="asOf"/> is the clock (UTC now
    /// when omitted) so a caller can test a specific instant.
    /// </summary>
    public static bool IsGuestTellerAccessOpen(
        DateTimeOffset? listedForPublicAsOf,
        DateTimeOffset? asOf = null)
    {
        if (listedForPublicAsOf == null)
        {
            return false;
        }

        return listedForPublicAsOf <= (asOf ?? DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// Legacy bool from import and create. <c>true</c> opens guest access by
    /// keeping an existing <see cref="Election.ListedForPublicAsOf"/>, or
    /// stamping now when it was closed, so a repeat import does not move the
    /// original open time. <c>false</c> clears the timestamp. <c>null</c>
    /// leaves the election unchanged.
    /// </summary>
    public static void ApplyListForPublicFlag(Election election, bool? listForPublic)
    {
        if (!listForPublic.HasValue)
        {
            return;
        }

        election.ListedForPublicAsOf = listForPublic == true
            ? election.ListedForPublicAsOf ?? DateTimeOffset.UtcNow
            : null;
    }

    /// <summary>
    /// Imported guest access. A timestamp wins over the legacy bool, including
    /// when the two disagree. A null timestamp falls back to
    /// <see cref="ApplyListForPublicFlag"/>.
    /// </summary>
    public static void ApplyImportedGuestAccess(
        Election election,
        bool? listForPublic,
        DateTimeOffset? listedForPublicAsOf)
    {
        if (listedForPublicAsOf != null)
        {
            election.ListedForPublicAsOf = listedForPublicAsOf;
            return;
        }

        ApplyListForPublicFlag(election, listForPublic);
    }
}
