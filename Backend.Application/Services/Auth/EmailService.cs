using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace Backend.Application.Services.Auth;

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
                <p><strong>{resetToken}</strong></p>
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
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(
            _configuration["Email:FromName"],
            _configuration["Email:FromAddress"] ?? defaultReplyFromEmail
        ));
        message.To.Add(new MailboxAddress(toEmail, toEmail));
        message.Subject = "Verify Your Email Address";

        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = $@"
                <h2>Welcome to TallyJ</h2>
                <p>Thank you for registering an account. Please verify your email address to complete your registration.</p>
                <p>Use the following verification code to confirm your email:</p>
                <p><strong>{verificationToken}</strong></p>
                <p>This code will expire in 24 hours.</p>
                <p>If you did not create this account, please ignore this email.</p>
            ",
            TextBody = $@"
Welcome to TallyJ

Thank you for registering an account. Please verify your email address to complete your registration.

Use the following verification code to confirm your email:

{verificationToken}

This code will expire in 24 hours.

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
}
