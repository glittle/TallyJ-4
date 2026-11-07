using Backend.DTOs.People;
using Backend.Helpers;

namespace Backend.Services;

/// <summary>
/// Routes a failed-recipient retry to the email/SMS queue or the WhatsApp queue.
/// </summary>
public static class AnnouncementRetry
{
    public static async Task<AnnouncementStatusDto> RetryAsync(
        IAnnouncementQueue announcements,
        IWhatsAppNotifyQueue whatsApp,
        Guid electionGuid,
        string channel,
        CancellationToken cancellationToken)
    {
        if (channel == AnnouncementChannels.WhatsApp)
        {
            var status = await whatsApp.RetryFailedAsync(electionGuid, cancellationToken);
            return FromWhatsApp(status);
        }

        return await announcements.RetryFailedAsync(electionGuid, channel, cancellationToken);
    }

    public static AnnouncementStatusDto FromWhatsApp(WhatsAppNotifyStatusDto status) =>
        new()
        {
            QueueToken = status.QueueToken,
            Channel = AnnouncementChannels.WhatsApp,
            Running = status.Running,
            Cancelled = status.Cancelled,
            Queued = status.Queued,
            Sent = status.Sent,
            Skipped = status.Skipped,
            Failed = status.Failed,
            MoreFailedRemain = status.MoreFailedRemain,
            Results = status.Results.Select(row => new AnnouncementPersonResultDto
            {
                PersonGuid = row.PersonGuid,
                Outcome = row.Outcome,
                Error = row.Error
            }).ToList()
        };
}
