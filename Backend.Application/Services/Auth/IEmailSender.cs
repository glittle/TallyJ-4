using MimeKit;

namespace Backend.Application.Services.Auth;

public interface IEmailSender
{
    Task SendAsync(MimeMessage message);
}
