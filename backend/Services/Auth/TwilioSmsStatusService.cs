using Backend.Context;
using Backend.Entities;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services.Auth;

/// <summary>
/// Single Twilio status-callback path (v3 Public/SmsStatus). Updates SmsLog when present
/// and auto-learns a lasting SmsStatus block on a matching phone OnlineVoter row.
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
        await UpdateSmsLogIfPresentAsync(smsSid, messageStatus, to, errorCode, cancellationToken);
        await TryLearnSmsStatusAsync(messageStatus, to, errorCode, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// v3 <c>TwilioHelper.LogSmsStatus</c>: update the existing SmsLog row for this SID.
    /// Do not insert a log row from a callback.
    /// </summary>
    private async Task UpdateSmsLogIfPresentAsync(
        string? smsSid,
        string? messageStatus,
        string? to,
        int? errorCode,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(smsSid))
        {
            return;
        }

        var log = await _context.SmsLogs
            .FirstOrDefaultAsync(sl => sl.SmsSid == smsSid, cancellationToken);
        if (log == null)
        {
            return;
        }

        log.LastStatus = messageStatus;
        log.ErrorCode = errorCode;
        log.LastDate = DateTimeOffset.UtcNow;
        if (!string.IsNullOrWhiteSpace(to))
        {
            log.Phone = to.Trim();
        }
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
    /// Existing <c>VoterIdType == "P"</c> row whose VoterId matches Twilio To
    /// (exact stored string, or the +/- variant). A non-P occupant of a candidate
    /// VoterId is skipped; no convert, no wipe, no insert.
    /// </summary>
    private async Task<OnlineVoter?> FindExistingPhoneOnlineVoterAsync(
        string? twilioTo,
        CancellationToken cancellationToken)
    {
        foreach (var key in TwilioSmsStatusHelper.VoterIdLookupKeys(twilioTo))
        {
            var row = await _context.OnlineVoters
                .FirstOrDefaultAsync(ov => ov.VoterId == key, cancellationToken);
            if (row == null)
            {
                continue;
            }

            if (row.VoterIdType != OnlineVoterPhoneHelper.PhoneVoterIdType)
            {
                continue;
            }

            return row;
        }

        return null;
    }

    private static string KnownMessageStatus(string? messageStatus) =>
        messageStatus?.ToLowerInvariant() switch
        {
            "undelivered" => "undelivered",
            "failed" => "failed",
            "delivered" => "delivered",
            "sent" => "sent",
            "queued" => "queued",
            "sending" => "sending",
            _ => "other"
        };
}
