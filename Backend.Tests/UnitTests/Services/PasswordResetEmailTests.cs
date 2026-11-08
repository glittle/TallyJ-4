using System.Globalization;
using Backend.Configuration;
using Backend.Localization;
using Backend.Services.Auth;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;

namespace Backend.Tests.UnitTests.Services;

public class PasswordResetEmailTests
{
    [Fact]
    public async Task SendPasswordResetEmail_UsesConfiguredOriginAndEncodesToken()
    {
        const string token = "a+b/c==";
        const string email = "officer@example.com";
        var sender = new CapturingSender();
        var logs = new ListLogger<EmailService>();
        var service = CreateService(sender, logs, "https://officers.example.org", new NullLocales());

        await service.SendPasswordResetEmailAsync(email, token);

        var message = sender.Message;
        Assert.NotNull(message);
        Assert.Contains("officers.example.org", message!.TextBody);
        Assert.DoesNotContain("evil.example", message.TextBody);
        Assert.DoesNotContain("Use the following token", message.TextBody);

        var link = FirstLink(message.TextBody!);
        var query = QueryHelpers.ParseQuery(new Uri(link).Query);
        Assert.Equal(email, query["email"].ToString());
        Assert.Equal(token, query["token"].ToString());
        Assert.Contains("token=" + Uri.EscapeDataString(token), link);
        Assert.Contains(System.Net.WebUtility.HtmlEncode(link), message.HtmlBody);
        Assert.Contains("1 hour", message.TextBody);
        Assert.DoesNotContain("<strong>", message.HtmlBody);

        var written = string.Join('\n', logs.Lines);
        Assert.DoesNotContain(email, written);
        Assert.DoesNotContain(token, written);
    }

    [Fact]
    public async Task SendPasswordResetEmail_WhenSenderFails_DoesNotLogAddressOrToken()
    {
        const string token = "secret-token+value/==";
        const string email = "officer@example.com";
        var sender = new CapturingSender
        {
            Error = new InvalidOperationException($"smtp refused {email} {token}")
        };
        var logs = new ListLogger<EmailService>();
        var service = CreateService(sender, logs, FrontendUrlResolver.DevelopmentDefault, new NullLocales());

        var thrown = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SendPasswordResetEmailAsync(email, token));

        Assert.Contains(email, thrown.Message);
        var written = string.Join('\n', logs.Lines);
        Assert.DoesNotContain(email, written);
        Assert.DoesNotContain(token, written);
        Assert.Contains("Failed to send password reset email", written);
    }

    [Fact]
    public async Task SendPasswordResetEmail_UsesRequestCulture_AndKeepsTheConfiguredLink()
    {
        const string token = "a+b/c==";
        var previous = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr");
        try
        {
            var sender = new CapturingSender();
            var service = CreateService(
                sender,
                new ListLogger<EmailService>(),
                "https://officers.example.org",
                new FrenchSubjectLocales());

            await service.SendPasswordResetEmailAsync("officer@example.com", token);

            Assert.Equal("Réinitialiser le mot de passe", sender.Message!.Subject);
            var link = FirstLink(sender.Message.TextBody!);
            Assert.StartsWith("https://officers.example.org/reset-password?", link);
            Assert.Equal(token, QueryHelpers.ParseQuery(new Uri(link).Query)["token"].ToString());
            Assert.Contains("1 hour", sender.Message.TextBody);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    private static EmailService CreateService(
        CapturingSender sender,
        ListLogger<EmailService> logs,
        string origin,
        IJsonLocalizationProvider localization)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [FrontendUrlResolver.ConfigKey] = origin,
                ["Email:FromAddress"] = "noreply@example.org",
                ["Email:FromName"] = "TallyJ"
            })
            .Build();
        var environment = new Mock<IHostEnvironment>();
        environment.Setup(host => host.EnvironmentName).Returns(Environments.Production);
        return new EmailService(configuration, environment.Object, logs, sender, localization);
    }

    private sealed class NullLocales : IJsonLocalizationProvider
    {
        public string? GetString(string key, CultureInfo culture) => null;

        public Dictionary<string, string> GetAllStrings(CultureInfo culture) => [];
    }

    private sealed class FrenchSubjectLocales : IJsonLocalizationProvider
    {
        public string? GetString(string key, CultureInfo culture)
        {
            if (culture.TwoLetterISOLanguageName == "fr"
                && key == "auth.passwordResetEmail.subject")
            {
                return "Réinitialiser le mot de passe";
            }

            return null;
        }

        public Dictionary<string, string> GetAllStrings(CultureInfo culture) => [];
    }

    private static string FirstLink(string text)
    {
        var start = text.IndexOf("https://", StringComparison.Ordinal);
        Assert.True(start >= 0);
        var end = text.IndexOfAny([' ', '\r', '\n'], start);
        return end < 0 ? text[start..] : text[start..end];
    }

    private sealed class CapturingSender : IEmailSender
    {
        public MimeMessage? Message { get; private set; }
        public Exception? Error { get; init; }

        public Task SendAsync(MimeMessage message)
        {
            Message = message;
            if (Error != null)
            {
                throw Error;
            }

            return Task.CompletedTask;
        }
    }

    private sealed class ListLogger<T> : ILogger<T>
    {
        public List<string> Lines { get; } = [];

        public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Lines.Add(formatter(state, exception));
            if (exception != null)
            {
                Lines.Add(exception.ToString());
            }
        }

        private sealed class NullScope : IDisposable
        {
            public static readonly NullScope Instance = new();
            public void Dispose()
            {
            }
        }
    }
}
