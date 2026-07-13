using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Backend.Services.Auth;

public class EmailService
{
    private const string defaultReplyFromEmail = "noreply@tallyj.com";
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;
    private readonly IEmailSender _emailSender;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger, IEmailSender emailSender)
    {
        _configuration = configuration;
        _logger = logger;
        _emailSender = emailSender;
    }

    public virtual async Task SendPasswordResetEmailAsync(string toEmail, string resetToken)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"],
            _configuration["Email:FromAddress"] ?? defaultReplyFromEmail
        ));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = "Password Reset Request";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <h2>Password Reset Request</h2>
                <p>You requested a password reset for your TallyJ account.</p>
                <p>Use the following token to reset your password:</p>
                <p><strong>{System.Net.WebUtility.HtmlEncode(resetToken)}</strong></p>
                <p>This token will expire in 1 hour.</p>
                <p>If you did not request this reset, please ignore this email.</p>
            ",
            TextBody = $@"
Password Reset Request

You requested a password reset for your TallyJ account.
Use the following token to reset your password:

{resetToken}

This token will expire in 1 hour.
If you did not request this reset, please ignore this email.
            "
        };

        message.Body = bodyBuilder.ToMessageBody();

        try
        {
            await _emailSender.SendAsync(message);
            _logger.LogInformation("Password reset email sent to {Email}", toEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", toEmail);
            throw;
        }
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
            _logger.LogInformation("Email change confirmation sent to {Email}", newEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email change confirmation to {Email}", newEmail);
            throw;
        }
    }

    private string BuildFrontendUrl(string path, params (string Key, string Value)[] query)
    {
        var baseUrl = (_configuration["Frontend:BaseUrl"] ?? "http://localhost:8095").TrimEnd('/');
        var qs = string.Join("&", query.Select(q =>
            $"{Uri.EscapeDataString(q.Key)}={Uri.EscapeDataString(q.Value)}"));
        var normalizedPath = path.StartsWith('/') ? path : "/" + path;
        return string.IsNullOrEmpty(qs) ? $"{baseUrl}{normalizedPath}" : $"{baseUrl}{normalizedPath}?{qs}";
    }
}
