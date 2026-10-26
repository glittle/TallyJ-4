using Backend.Context;
using Backend.Entities;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Shared last-status and masked failure log for bulk email, SMS, and WhatsApp.
/// A paid send still goes through <see cref="ICodeSendGuard"/>; this type does not reserve a slot.
/// </summary>
internal static class BulkSendSupport
{
    /// <summary>
    /// Inserts or updates the person's last announcement row.
    /// </summary>
    public static async Task UpsertLastAsync(
        MainDbContext db,
        Guid electionGuid,
        Guid personGuid,
        string channel,
        string outcome,
        string? error,
        CancellationToken cancellationToken)
    {
        var row = await db.PersonLastAnnouncements
            .FirstOrDefaultAsync(item => item.PersonGuid == personGuid, cancellationToken);
        var safeError = AnnouncementErrorText.Sanitize(error);
        if (safeError == null && !string.IsNullOrWhiteSpace(error))
        {
            safeError = "send-failed";
        }

        if (row == null)
        {
            db.PersonLastAnnouncements.Add(new PersonLastAnnouncement
            {
                PersonGuid = personGuid,
                ElectionGuid = electionGuid,
                Channel = channel,
                Outcome = outcome,
                Error = safeError,
                AttemptedAt = DateTimeOffset.UtcNow
            });
        }
        else
        {
            row.ElectionGuid = electionGuid;
            row.Channel = channel;
            row.Outcome = outcome;
            row.Error = safeError;
            row.AttemptedAt = DateTimeOffset.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Writes a masked <see cref="CodeSendLog"/> for a destination that was not reserved
    /// (invalid phone or email). Does not consume a cap.
    /// </summary>
    public static async Task LogUnreservedBlockAsync(
        MainDbContext db,
        Guid electionGuid,
        string channel,
        string destination,
        string reason,
        CancellationToken cancellationToken)
    {
        db.CodeSendLogs.Add(new CodeSendLog
        {
            ElectionGuid = electionGuid,
            Channel = channel,
            MaskedDestination = Fit(DestinationMask.Mask(destination), 80),
            Outcome = Fit(CodeSendBlockReason.Blocked(reason), 40),
            SentAt = DateTimeOffset.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// One warning that lists every masked destination that did not go out
    /// because the provider failed, the guard blocked it, or the address was invalid.
    /// </summary>
    public static void LogFailures(
        ILogger logger,
        Guid electionGuid,
        string channel,
        IReadOnlyCollection<string> maskedDestinations)
    {
        if (maskedDestinations.Count == 0)
        {
            return;
        }

        logger.LogWarning(
            "Bulk send finished for election {ElectionGuid} channel {Channel} with {FailedCount} failures. Masked destinations: {MaskedDestinations}",
            electionGuid,
            channel,
            maskedDestinations.Count,
            string.Join(", ", maskedDestinations));
    }

    public static string Mask(string? destination) => DestinationMask.Mask(destination);

    private static string Fit(string value, int max)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}
