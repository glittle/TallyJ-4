using Microsoft.Extensions.Hosting;

namespace Backend.Configuration;

/// <summary>
/// Which sign-in methods this host offers. Flags live under <c>SignIn</c>.
/// A method is offered only when its flag is on and, outside Development and Testing,
/// the provider settings that method already requires are present.
/// Officer and voter lists are independent. Guest teller passcode login is not in this set.
/// </summary>
public static class SignInCapabilities
{
    public const string OfficerDisabledKey = "auth.errors.signInMethodDisabled";
    public const string VoterDisabledKey = "voting.auth.methodDisabled";

    public static SignInCapabilitySet Evaluate(IConfiguration configuration, IHostEnvironment environment)
    {
        var relaxed = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        bool Flag(string key, bool fallback)
        {
            var raw = configuration[key];
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            return bool.TryParse(raw, out var parsed) ? parsed : fallback;
        }

        bool Offer(bool enabled, bool configured) => enabled && (configured || relaxed);

        var explicitOfficerTelegram = configuration["SignIn:Officers:Telegram"];
        var officersTelegram = string.IsNullOrWhiteSpace(explicitOfficerTelegram)
            ? Flag("ClientEnv:enableTelegramLogin", false)
            : Flag("SignIn:Officers:Telegram", false);
        var votersPhone = Flag("SignIn:Voters:PhoneCode", true);
        var twilio = TwilioConfigured(configuration);
        var whatsApp = GreenApiConfigured(configuration);

        return new SignInCapabilitySet
        {
            Officers = new OfficerSignInCapabilities
            {
                Local = Flag("SignIn:Officers:Local", true),
                Google = Offer(Flag("SignIn:Officers:Google", true), GoogleOfficerConfigured(configuration)),
                Facebook = Offer(Flag("SignIn:Officers:Facebook", true), FacebookConfigured(configuration)),
                Kakao = Offer(Flag("SignIn:Officers:Kakao", true), KakaoConfigured(configuration)),
                Telegram = Offer(officersTelegram, TelegramConfigured(configuration))
            },
            Voters = new VoterSignInCapabilities
            {
                Google = Offer(Flag("SignIn:Voters:Google", true), GoogleVoterConfigured(configuration)),
                Facebook = Offer(Flag("SignIn:Voters:Facebook", true), FacebookConfigured(configuration)),
                Kakao = Offer(Flag("SignIn:Voters:Kakao", true), KakaoConfigured(configuration)),
                Telegram = Offer(Flag("SignIn:Voters:Telegram", true), TelegramConfigured(configuration)),
                EmailCode = Flag("SignIn:Voters:EmailCode", true),
                PhoneCode = votersPhone,
                Sms = votersPhone && Offer(Flag("SignIn:Voters:Sms", true), twilio),
                Voice = votersPhone && Offer(Flag("SignIn:Voters:Voice", true), twilio),
                WhatsApp = votersPhone && Offer(Flag("SignIn:Voters:WhatsApp", true), whatsApp),
                DirectCode = Flag("SignIn:Voters:DirectCode", true)
            }
        };
    }

    public static bool AllowsVoterRequest(VoterSignInCapabilities voters, string? voterIdType, string? deliveryMethod)
    {
        if (string.Equals(voterIdType, "E", StringComparison.OrdinalIgnoreCase))
        {
            return voters.EmailCode
                && string.Equals(deliveryMethod, "email", StringComparison.OrdinalIgnoreCase);
        }

        if (string.Equals(voterIdType, "C", StringComparison.OrdinalIgnoreCase))
        {
            return voters.DirectCode;
        }

        if (!string.Equals(voterIdType, "P", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return deliveryMethod?.ToLowerInvariant() switch
        {
            "sms" => voters.Sms,
            "voice" => voters.Voice,
            "whatsapp" => voters.WhatsApp,
            _ => false
        };
    }

    /// <summary>
    /// One Tap validates the ID token with the client id only.
    /// Redirect login also needs the client secret.
    /// </summary>
    private static bool GoogleOfficerConfigured(IConfiguration configuration) =>
        Configured(configuration["ClientEnv:googleClientId"]);

    public static bool OfficerGoogleRedirectReady(IConfiguration configuration) =>
        Configured(configuration["ClientEnv:googleClientId"])
        && Configured(configuration["GoogleClientSecret"]);

    private static bool GoogleVoterConfigured(IConfiguration configuration) =>
        Configured(configuration["Google:ClientId"]);

    private static bool FacebookConfigured(IConfiguration configuration) =>
        Configured(configuration["ClientEnv:facebookAppId"]);

    private static bool KakaoConfigured(IConfiguration configuration) =>
        Configured(configuration["ClientEnv:kakaoApiJsKey"]);

    private static bool TelegramConfigured(IConfiguration configuration) =>
        Configured(configuration["Telegram:BotToken"])
        && Configured(configuration["ClientEnv:telegramBotUsername"]);

    private static bool TwilioConfigured(IConfiguration configuration) =>
        Configured(configuration["Twilio:AccountSid"])
        && Configured(configuration["Twilio:AuthToken"])
        && Configured(configuration["Twilio:FromNumber"]);

    private static bool GreenApiConfigured(IConfiguration configuration) =>
        Backend.Helpers.GreenApiWhatsAppHelper.IsConfigured(
            configuration["GreenApi:IdInstance"],
            configuration["GreenApi:ApiToken"]);

    private static bool Configured(string? value) =>
        !string.IsNullOrWhiteSpace(value) && !value.TrimStart().StartsWith('<');
}

public sealed class SignInCapabilitySet
{
    public OfficerSignInCapabilities Officers { get; init; } = new();
    public VoterSignInCapabilities Voters { get; init; } = new();
}

public sealed class OfficerSignInCapabilities
{
    public bool Local { get; init; }
    public bool Google { get; init; }
    public bool Facebook { get; init; }
    public bool Kakao { get; init; }
    public bool Telegram { get; init; }

    public bool Any => Local || Google || Facebook || Kakao || Telegram;
}

public sealed class VoterSignInCapabilities
{
    public bool Google { get; init; }
    public bool Facebook { get; init; }
    public bool Kakao { get; init; }
    public bool Telegram { get; init; }
    public bool EmailCode { get; init; }
    public bool PhoneCode { get; init; }
    public bool Sms { get; init; }
    public bool Voice { get; init; }
    public bool WhatsApp { get; init; }
    public bool DirectCode { get; init; }
}
