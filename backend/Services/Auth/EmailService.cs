using System.Globalization;
using Backend.Configuration;
using Backend.Localization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Backend.Services.Auth;

public class EmailService
{
    private const string defaultReplyFromEmail = "noreply@tallyj.com";
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<EmailService> _logger;
    private readonly IEmailSender _emailSender;
    private readonly IJsonLocalizationProvider _localization;

    public EmailService(
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<EmailService> logger,
        IEmailSender emailSender,
        IJsonLocalizationProvider localization)
    {
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _emailSender = emailSender;
        _localization = localization;
    }

    public virtual async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var resetUrl = BuildFrontendUrl(
            "/reset-password",
            ("email", toEmail),
            ("token", resetToken));
        var expiryText = DescribeResetLifespan(PasswordResetTokenProvider.Lifespan);

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"],
            _configuration["Email:FromAddress"] ?? defaultReplyFromEmail
        ));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        var subject = Phrase(
            "auth.passwordResetEmail.subject",
            "Reset your TallyJ password");
        var heading = Phrase(
            "auth.passwordResetEmail.heading",
            "Reset your TallyJ password");
        var intro = Phrase(
            "auth.passwordResetEmail.intro",
            "A password reset was requested for this TallyJ account.");
        var action = Phrase(
            "auth.passwordResetEmail.action",
            "Reset password");
        var linkLead = Phrase(
            "auth.passwordResetEmail.linkLead",
            "Or open this link in your browser:");
        var expiry = Phrase(
            "auth.passwordResetEmail.expiry",
            "This link expires in {0} and can be used once. If you did not request this, you can ignore this email.",
            expiryText);

        message.Subject = subject;

        var safeUrl = System.Net.WebUtility.HtmlEncode(resetUrl);
        var safeHeading = System.Net.WebUtility.HtmlEncode(heading);
        var safeIntro = System.Net.WebUtility.HtmlEncode(intro);
        var safeAction = System.Net.WebUtility.HtmlEncode(action);
        var safeLinkLead = System.Net.WebUtility.HtmlEncode(linkLead);
        var safeExpiry = System.Net.WebUtility.HtmlEncode(expiry);
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <h2>{safeHeading}</h2>
                <p>{safeIntro}</p>
                <p><a href=""{safeUrl}"" style=""display:inline-block;padding:10px 16px;background:#409eff;color:#fff;text-decoration:none;border-radius:4px;"">{safeAction}</a></p>
                <p>{safeLinkLead}</p>
                <p><a href=""{safeUrl}"">{safeUrl}</a></p>
                <p>{safeExpiry}</p>
            ",
            TextBody = $@"
{heading}

{intro}

{action}:
{resetUrl}

{linkLead}
{resetUrl}

{expiry}
            "
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            await _emailSender.SendAsync(message);
            _logger.LogInformation("Password reset email sent");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Failed to send password reset email ({ExceptionType})",
                ex.GetType().Name);
            throw;
        }
    }

    /// <summary>
    /// Request UI culture, then English, then the built-in English sentence.
    /// A self-hosted site with no locale files still sends a usable message.
    /// The link itself is never taken from the catalog.
    /// </summary>
    private string Phrase(string key, string fallback, params object[] args)
    {
        var format = _localization.GetString(key, CultureInfo.CurrentUICulture);
        if (string.IsNullOrEmpty(format))
        {
            format = _localization.GetString(key, CultureInfo.GetCultureInfo("en"));
        }

        if (string.IsNullOrEmpty(format))
        {
            format = fallback;
        }

        return args.Length == 0 ? format : string.Format(CultureInfo.CurrentUICulture, format, args);
    }

    private static string DescribeResetLifespan(TimeSpan lifespan)
    {
        var hours = (int)lifespan.TotalHours;
        if (hours == 1)
        {
            return "1 hour";
        }

        if (hours > 1)
        {
            return $"{hours} hours";
        }

        return "1 hour";
    }

    public virtual async Task Send2FASetupEmailAsync(string toEmail, string qrCodeBase64)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"],
            _configuration["Email:FromAddress"] ?? defaultReplyFromEmail
        ));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = "Two-Factor Authentication Setup";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <h2>Two-Factor Authentication Setup</h2>
                <p>You have enabled two-factor authentication for your TallyJ account.</p>
                <p>Scan the QR code below with your authenticator app (Google Authenticator, Authy, etc.):</p>
                <img src='data:image/png;base64,{qrCodeBase64}' alt='2FA QR Code' />
                <p>If you did not enable 2FA, please contact support immediately.</p>
            ",
            TextBody = @"
Two-Factor Authentication Setup

You have enabled two-factor authentication for your TallyJ account.
Please use your authenticator app to scan the QR code sent in the HTML version of this email.
If you did not enable 2FA, please contact support immediately.
            "
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            await _emailSender.SendAsync(message);
            _logger.LogInformation("2FA setup email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send 2FA setup email to {Email}", toEmail);
            throw;
        }
    }

    public virtual async Task SendEmailVerificationEmailAsync(string toEmail, string verificationToken)
    {
        var verifyUrl = BuildFrontendUrl(
            "/verify-email",
            ("email", toEmail),
            ("token", verificationToken));

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"],
            _configuration["Email:FromAddress"] ?? defaultReplyFromEmail
        ));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = "Verify Your Email Address";

        var safeUrl = System.Net.WebUtility.HtmlEncode(verifyUrl);
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <h2>Welcome to TallyJ</h2>
                <p>Thank you for registering an account. Please verify your email address to complete your registration.</p>
                <p><a href=""{safeUrl}"" style=""display:inline-block;padding:10px 16px;background:#409eff;color:#fff;text-decoration:none;border-radius:4px;"">Verify email</a></p>
                <p>Or open this link in your browser:</p>
                <p><a href=""{safeUrl}"">{safeUrl}</a></p>
                <p>This link will expire in 24 hours.</p>
                <p>If you did not create this account, please ignore this email.</p>
            ",
            TextBody = $@"
Welcome to TallyJ

Thank you for registering an account. Please verify your email address to complete your registration.

Open this link to verify your email:
{verifyUrl}

This link will expire in 24 hours.

If you did not create this account, please ignore this email.
            "
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            await _emailSender.SendAsync(message);
            _logger.LogInformation("Email verification email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email verification email to {Email}", toEmail);
            throw;
        }
    }

    /// <summary>
    /// Sends confirmation to the new address for an in-progress email change.
    /// </summary>
    public virtual async Task SendEmailChangeConfirmationAsync(
        string userId,
        string newEmail,
        string confirmationToken,
        string shortCode)
    {
        var confirmUrl = BuildFrontendUrl(
            "/confirm-email-change",
            ("token", confirmationToken));

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"],
            _configuration["Email:FromAddress"] ?? defaultReplyFromEmail
        ));
        message.To.Add(new MailboxAddress(newEmail, newEmail));
        message.Subject = "Confirm your new TallyJ email address";

        var safeUrl = System.Net.WebUtility.HtmlEncode(confirmUrl);
        var safeCode = System.Net.WebUtility.HtmlEncode(shortCode);
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <h2>Confirm your new email</h2>
                <p>You requested to change the email address on your TallyJ account to this address.</p>
                <p><a href=""{safeUrl}"" style=""display:inline-block;padding:10px 16px;background:#409eff;color:#fff;text-decoration:none;border-radius:4px;"">Confirm new email</a></p>
                <p>Or open this link:</p>
                <p><a href=""{safeUrl}"">{safeUrl}</a></p>
                <p>Alternatively, sign in with your current email and enter this code on your profile page:</p>
                <p style=""font-size:1.4rem;letter-spacing:0.2em;""><strong>{safeCode}</strong></p>
                <p>This confirmation expires in 24 hours. If you did not request this change, you can ignore this email — your current login email will stay the same.</p>
            ",
            TextBody = $@"
Confirm your new email

You requested to change the email address on your TallyJ account to this address.

Open this link to confirm:
{confirmUrl}

Or sign in with your current email and enter this code on your profile page:
{shortCode}

This confirmation expires in 24 hours. If you did not request this change, ignore this email.
            "
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            await _emailSender.SendAsync(message);
            _logger.LogInformation("Email change confirmation sent for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email change confirmation for user {UserId}", userId);
            throw;
        }
    }

    private string BuildFrontendUrl(string path, params (string Key, string Value)[] query)
        => FrontendUrlResolver.Build(_configuration, _hostEnvironment, path, query);
}
