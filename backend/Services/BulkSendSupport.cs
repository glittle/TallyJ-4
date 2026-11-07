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
            Apply(row, electionGuid, channel, outcome, safeError);
        }

        try
        {
            await SaveOnceAsync(db, cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Another channel or teller inserted this person first. Update that row
            // instead of letting the queue cancel people whose send already succeeded.
            db.ChangeTracker.Clear();
            var winner = await db.PersonLastAnnouncements
                .FirstOrDefaultAsync(item => item.PersonGuid == personGuid, cancellationToken);
            if (winner == null)
            {
                return;
            }

            Apply(winner, electionGuid, channel, outcome, safeError);
            try
            {
                await db.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                db.ChangeTracker.Clear();
            }
        }
    }

    /// <summary>
    /// Test hook that runs once, immediately before the first save. Production leaves it null.
    /// </summary>
    internal static Func<CancellationToken, Task>? BeforeSaveForTests { get; set; }

    private static void Apply(
        PersonLastAnnouncement row,
        Guid electionGuid,
        string channel,
        string outcome,
        string? safeError)
    {
        row.ElectionGuid = electionGuid;
        row.Channel = channel;
        row.Outcome = outcome;
        row.Error = safeError;
        row.AttemptedAt = DateTimeOffset.UtcNow;
    }

    private static async Task SaveOnceAsync(MainDbContext db, CancellationToken cancellationToken)
    {
        var hook = BeforeSaveForTests;
        BeforeSaveForTests = null;
        if (hook != null)
        {
            await hook(cancellationToken);
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
    /// Channel token safe to write to a log. The request value is not copied into the message,
    /// so a newline or an address in that field cannot forge a log line or leak a mailbox.
    /// </summary>
    public static string LogChannel(string? channel)
    {
        if (string.Equals(channel, AnnouncementChannels.Email, StringComparison.Ordinal))
        {
            return "email";
        }

        if (string.Equals(channel, AnnouncementChannels.Sms, StringComparison.Ordinal))
        {
            return "sms";
        }

        if (string.Equals(channel, AnnouncementChannels.WhatsApp, StringComparison.Ordinal))
        {
            return "whatsapp";
        }

        return "unknown";
    }

    /// <summary>
    /// One warning with the failure count. Masked addresses stay in <see cref="CodeSendLog"/>
    /// and are not written here: a masked email still contains the domain.
    /// </summary>
    public static void LogFailures(
        ILogger logger,
        Guid electionGuid,
        string channel,
        int failedCount)
    {
        if (failedCount <= 0)
        {
            return;
        }

        logger.LogWarning(
            "Bulk send finished for election {ElectionGuid} channel {Channel} with {FailedCount} failures",
            electionGuid,
            LogChannel(channel),
            failedCount);
    }

    private static string Fit(string value, int max)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }
}

/// <summary>
/// Count of failed or invalid attempts for the end-of-run warning. Addresses are not stored.
/// </summary>
internal sealed class FailureNotices
{
    public int Count { get; private set; }

    public void Add() => Count++;
}
