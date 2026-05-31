using MimeKit;

namespace Backend.Services.Auth;

public interface IEmailSender
{
    Task SendAsync(MimeMessage message);
}
