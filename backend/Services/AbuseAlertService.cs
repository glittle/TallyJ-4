using Backend.Authorization;
using Backend.Configuration;
using Backend.Context;
using Backend.Entities;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

namespace Backend.Services;

/// <summary>
/// Sends one email and one Sentry warning per alert key inside the throttle window.
/// </summary>
public class AbuseAlertService : IAbuseAlertService
{
    private readonly MainDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ISentryWarningCapture _sentry;
    private readonly ILogger<AbuseAlertService> _logger;
    private readonly AntiAbuseOptions _options;
    private readonly SuperAdminSettings _superAdmin;

    /// <summary>
    /// Initializes the alert sender.
    /// </summary>
    public AbuseAlertService(
        MainDbContext context,
        IEmailSender emailSender,
        ISentryWarningCapture sentry,
        ILogger<AbuseAlertService> logger,
        IOptions<AntiAbuseOptions> options,
        IOptions<SuperAdminSettings> superAdmin)
    {
        _context = context;
        _emailSender = emailSender;
        _sentry = sentry;
        _logger = logger;
        _options = options.Value;
        _superAdmin = superAdmin.Value;
    }

    /// <inheritdoc />
    public Task NotifyCapHitAsync(AbuseCapHitAlert alert, CancellationToken cancellationToken = default)
    {
        var body = $"""
            A paid-send cap was hit.
            Scope: {alert.Scope}
            Election: {alert.ElectionName ?? "(none)"} ({alert.ElectionGuid})
            Owner: {alert.OwnerUserId}
            Used: {alert.Used}
            Cap: {alert.Cap}
            Sending stays stopped until a super admin raises the cap.
            """;
        return SendOnceAsync(
            alert.AlertKey,
            "TallyJ paid-send cap hit",
            body,
            new Dictionary<string, string>
            {
                ["alertKey"] = alert.AlertKey,
                ["scope"] = alert.Scope,
                ["electionGuid"] = alert.ElectionGuid?.ToString() ?? "",
                ["ownerUserId"] = alert.OwnerUserId?.ToString() ?? "",
                ["used"] = alert.Used.ToString(),
                ["cap"] = alert.Cap.ToString()
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task NotifyFirstPaidSendAsync(AbuseFirstPaidSendAlert alert, CancellationToken cancellationToken = default)
    {
        var body = $"""
            An owner made their first paid send (SMS, voice, or WhatsApp).
            Election: {alert.ElectionName ?? "(none)"} ({alert.ElectionGuid})
            Owner: {alert.OwnerUserId}
            Channel: {alert.Channel}
            Destination: {alert.MaskedDestination}
            """;
        return SendOnceAsync(
            "first-paid:" + alert.OwnerUserId.ToString("N"),
            "TallyJ first paid send",
            body,
            new Dictionary<string, string>
            {
                ["ownerUserId"] = alert.OwnerUserId.ToString(),
                ["electionGuid"] = alert.ElectionGuid.ToString(),
                ["channel"] = alert.Channel,
                ["maskedDestination"] = alert.MaskedDestination
            },
            cancellationToken);
    }

    /// <inheritdoc />
    public Task NotifyElectionFlaggedAsync(AbuseElectionFlaggedAlert alert, CancellationToken cancellationToken = default)
    {
        var rows = alert.Rows.Count == 0
            ? "(none)"
            : string.Join(
                Environment.NewLine,
                alert.Rows.Select(row =>
                    $"row {row.RowNumber?.ToString() ?? "-"}: {row.MaskedValue} ({row.Reason})"));
        var body = $"""
            An election was flagged. SMS and WhatsApp are stopped. Online voting is blocked until a super admin clears the flag.
            Election: {alert.ElectionName ?? "(none)"} ({alert.ElectionGuid})
            Flagged entries: {alert.FlaggedEntryCount}
            {rows}
            """;
        var fields = new Dictionary<string, string>
        {
            ["electionGuid"] = alert.ElectionGuid.ToString(),
            ["flaggedEntryCount"] = alert.FlaggedEntryCount.ToString(),
            ["rows"] = rows
        };
        return SendOnceAsync(
            "flag:" + alert.ElectionGuid.ToString("N"),
            "TallyJ election flagged — online voting blocked",
            body,
            fields,
            cancellationToken);
    }

    private async Task SendOnceAsync(
        string alertKey,
        string subject,
        string body,
        Dictionary<string, string> fields,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var state = await _context.AbuseAlertStates
            .FirstOrDefaultAsync(item => item.AlertKey == alertKey, cancellationToken);
        if (state != null && state.LastSentAt > now.AddHours(-_options.ResolvedAlertThrottleHours))
        {
            _logger.LogInformation(
                "Abuse alert skipped by throttle {AlertKey}",
                alertKey);
            return;
        }

        _logger.LogWarning(
            "Abuse alert {AlertKey} {Subject} {@Fields}",
            alertKey,
            subject,
            fields);
        _sentry.Capture(subject, fields);

        var recipients = AlertRecipients();
        if (recipients.Count > 0)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(
                "TallyJ",
                "noreply@tallyj.local"));
            foreach (var recipient in recipients)
            {
                message.To.Add(MailboxAddress.Parse(recipient));
            }

            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };
            try
            {
                await _emailSender.SendAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Abuse alert email failed for {AlertKey}", alertKey);
            }
        }

        if (state == null)
        {
            _context.AbuseAlertStates.Add(new AbuseAlertState
            {
                AlertKey = alertKey,
                LastSentAt = now
            });
        }
        else
        {
            state.LastSentAt = now;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (state == null)
        {
            _logger.LogInformation(ex, "Abuse alert throttle row already stored for {AlertKey}", alertKey);
        }
    }

    private List<string> AlertRecipients()
    {
        var configured = _options.AlertEmails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .ToList();
        if (configured.Count > 0)
        {
            return configured;
        }

        return _superAdmin.Emails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
