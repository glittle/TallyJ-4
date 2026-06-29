using Backend.Entities;

namespace Backend.Helpers;

/// <summary>
/// Guest teller access is controlled by <see cref="Backend.Entities.Election.ListedForPublicAsOf"/>.
/// </summary>
public static class ElectionTellerAccessHelper
{
    /// <summary>
    /// Returns true when guest tellers may discover and log in to an election.
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
    /// Maps legacy import/create <c>ListForPublic</c> flags onto <see cref="Backend.Entities.Election.ListedForPublicAsOf"/>.
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
    /// Applies imported guest-access timestamps and legacy boolean flags.
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