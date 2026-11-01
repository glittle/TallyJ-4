using Backend.Context;
using Backend.Entities;
using Backend.Helpers;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using MimeKit;

namespace Backend.Services;

/// <summary>
/// Emails a test code to a restricted owner's own account address.
/// Does not open online voting and does not store a voter login code.
/// </summary>
public interface IOwnerTestCodeService
{
    /// <summary>
    /// Sends the test email when the account is restricted or waiting for review.
    /// </summary>
    Task<OwnerReviewActionResult> SendAsync(string? userId, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public class OwnerTestCodeService : IOwnerTestCodeService
{
    public const string NotAllowedKey = "elections.ownerRestricted.testCodeNotAllowed";
    public const string FrozenKey = "elections.ownerRestricted.testCodeFrozen";
    public const string SendFailedKey = "elections.ownerRestricted.testCodeFailed";
    public const string Subject = "[TallyJ] Test code";

    private readonly MainDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OwnerTestCodeService> _logger;

    /// <summary>
    /// Initializes the test-code sender.
    /// </summary>
    public OwnerTestCodeService(
        MainDbContext context,
        IEmailSender emailSender,
        IConfiguration configuration,
        ILogger<OwnerTestCodeService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _configuration = configuration;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OwnerReviewActionResult> SendAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var id))
        {
            return OwnerReviewActionResult.Fail(OwnerReviewService.NotFoundKey);
        }

        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id.ToString(), cancellationToken);
        if (user == null || string.IsNullOrWhiteSpace(user.Email))
        {
            return OwnerReviewActionResult.Fail(OwnerReviewService.NotFoundKey);
        }

        var row = await _context.OwnerSendControls.AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == id, cancellationToken);
        if (row == null || !OwnerRestrictionStatus.AllowsOwnEmailTest(row.RestrictionStatus))
        {
            return OwnerReviewActionResult.Fail(NotAllowedKey);
        }

        if (row.SendsFrozen)
        {
            return OwnerReviewActionResult.Fail(FrozenKey);
        }

        var code = SecureCode.FromAlphabet("ABCDEFGHJKLMNPQRSTUVWXYZ23456789", 6);
        try
        {
            await SendTestEmailAsync(user.Email, code, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Owner test code email failed for {OwnerUserId}", id);
            return OwnerReviewActionResult.Fail(SendFailedKey);
        }

        _logger.LogInformation("Sent an owner test code to {OwnerUserId}", id);
        return OwnerReviewActionResult.Ok();
    }

    private async Task SendTestEmailAsync(string email, string code, CancellationToken cancellationToken)
    {
        var fromAddress = _configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            fromAddress = "noreply@tallyj.com";
        }

        var fromName = _configuration["Email:FromName"];
        if (string.IsNullOrWhiteSpace(fromName))
        {
            fromName = "TallyJ";
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        message.To.Add(new MailboxAddress(email, email));
        message.Subject = Subject;
        message.Body = new TextPart("plain")
        {
            Text = "This is a test code for your TallyJ account only (" + email + "). "
                + "It does not open online voting and voters cannot use it.\n\n"
                + "Code: " + code + "\n"
        };
        cancellationToken.ThrowIfCancellationRequested();
        await _emailSender.SendAsync(message);
    }
}
