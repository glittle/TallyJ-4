using Backend.Context;
using Backend.Entities;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Auth;

/// <summary>
/// Single Twilio status-callback path (v3 Public/SmsStatus). Updates SmsLog when present,
/// auto-learns a lasting SmsStatus block on selected terminal failures, and sets
/// SmsStatus to OK when a delivered/completed callback matches an existing SID.
/// </summary>
public class TwilioSmsStatusService : ITwilioSmsStatusService
{
    private readonly MainDbContext _context;
    private readonly ILogger<TwilioSmsStatusService> _logger;

    public TwilioSmsStatusService(MainDbContext context, ILogger<TwilioSmsStatusService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task ProcessCallbackAsync(
        string? smsSid,
        string? messageStatus,
        string? to,
        int? errorCode,
        CancellationToken cancellationToken = default)
    {
        var log = await UpdateSmsLogIfPresentAsync(smsSid, messageStatus, to, errorCode, cancellationToken);
        await TryLearnSmsStatusAsync(messageStatus, to, errorCode, cancellationToken);
        if (log != null)
        {
            await TrySetOkFromDeliveredAsync(messageStatus, to, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// v3 <c>TwilioHelper.LogSmsStatus</c>: update the existing SmsLog row for this SID.
    /// Do not insert a log row from a callback.
    /// </summary>
    private async Task<SmsLog?> UpdateSmsLogIfPresentAsync(
        string? smsSid,
        string? messageStatus,
        string? to,
        int? errorCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(smsSid))
        {
            return null;
        }

        var log = await _context.SmsLogs
            .FirstOrDefaultAsync(sl => sl.SmsSid == smsSid, cancellationToken);
        if (log == null)
        {
            return null;
        }

        log.LastStatus = messageStatus;
        log.ErrorCode = errorCode;
        log.LastDate = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(to))
        {
            log.Phone = to.Trim();
        }

        return log;
    }

    private async Task TryLearnSmsStatusAsync(
        string? messageStatus,
        string? to,
        int? errorCode,
        CancellationToken cancellationToken)
    {
        var reason = TwilioSmsStatusHelper.TryLearnReason(messageStatus, errorCode);
        if (reason is null)
        {
            return;
        }

        var row = await FindExistingPhoneOnlineVoterAsync(to, cancellationToken);
        if (row == null)
        {
            _logger.LogInformation(
                "{Method}: no phone OnlineVoter ({Status}, {ErrorCode})",
                nameof(ProcessCallbackAsync),
                KnownMessageStatus(messageStatus),
                errorCode);
            return;
        }

        if (!OnlineVoterSmsStatus.CanLearnFromCallback(row.SmsStatus))
        {
            _logger.LogInformation(
                "{Method}: SmsStatus already blocked ({Status}, {ErrorCode})",
                nameof(ProcessCallbackAsync),
                KnownMessageStatus(messageStatus),
                errorCode);
            return;
        }

        row.SmsStatus = reason;
        _logger.LogInformation(
            "{Method}: learned SmsStatus ({Status}, {ErrorCode})",
            nameof(ProcessCallbackAsync),
            KnownMessageStatus(messageStatus),
            errorCode);
    }

    /// <summary>
    /// Delivered/completed on an existing SID means the phone just worked. Set the
    /// matching P row to OK even when a teller (or earlier callback) had blocked it.
    /// Does not insert a row. Does not convert a non-P occupant.
    /// </summary>
    private async Task TrySetOkFromDeliveredAsync(
        string? messageStatus,
        string? to,
        CancellationToken cancellationToken)
    {
        if (!TwilioSmsStatusHelper.IsDeliveredSuccess(messageStatus))
        {
            return;
        }

        var row = await FindExistingPhoneOnlineVoterAsync(to, cancellationToken);
        if (row == null)
        {
            _logger.LogInformation(
                "{Method}: no phone OnlineVoter ({Status})",
                nameof(ProcessCallbackAsync),
                KnownMessageStatus(messageStatus));
            return;
        }

        if (row.SmsStatus == OnlineVoterSmsStatus.Ok)
        {
            return;
        }

        row.SmsStatus = OnlineVoterSmsStatus.Ok;
        _logger.LogInformation(
            "{Method}: set SmsStatus OK ({Status})",
            nameof(ProcessCallbackAsync),
            KnownMessageStatus(messageStatus));
    }

    /// <summary>
    /// Existing phone row whose VoterId matches Twilio To (exact stored string, then
    /// the +/- variant). The query is <c>VoterId == key AND VoterIdType == "P"</c>
    /// (same as paid-send / <see cref="OnlineVoterPhoneHelper.FindTrackedPhoneOnlineVoterAsync"/>).
    /// A non-P occupant of a candidate VoterId is not returned; no convert, no wipe, no insert.
    /// Tracked (not AsNoTracking) so <see cref="OnlineVoter.SmsStatus"/> can be written.
    /// </summary>
    private async Task<OnlineVoter?> FindExistingPhoneOnlineVoterAsync(
        string? twilioTo,
        CancellationToken cancellationToken)
    {
        foreach (var key in TwilioSmsStatusHelper.VoterIdLookupKeys(twilioTo))
        {
            var row = await OnlineVoterPhoneHelper.FindTrackedPhoneOnlineVoterAsync(
                _context, key, cancellationToken);
            if (row != null)
            {
                return row;
            }
        }

        return null;
    }

    private static string KnownMessageStatus(string? messageStatus) =>
        messageStatus?.ToLowerInvariant() switch
        {
            "undelivered" => "undelivered",
            "failed" => "failed",
            "delivered" => "delivered",
            "completed" => "completed",
            "sent" => "sent",
            "queued" => "queued",
            "sending" => "sending",
            _ => "other"
        };
}
