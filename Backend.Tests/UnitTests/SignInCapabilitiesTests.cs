using Backend.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Backend.Tests.UnitTests;

public class SignInCapabilitiesTests
{
    [Fact]
    public void Defaults_OfferCurrentMethods_AndKeepTelegramOffForOfficers()
    {
        var set = Evaluate(new Dictionary<string, string?>(), Environments.Production);

        Assert.True(set.Officers.Local);
        Assert.False(set.Officers.Google);
        Assert.False(set.Officers.Facebook);
        Assert.False(set.Officers.Kakao);
        Assert.False(set.Officers.Telegram);
        Assert.True(set.Voters.EmailCode);
        Assert.True(set.Voters.DirectCode);
        Assert.False(set.Voters.Google);
        Assert.False(set.Voters.Sms);
        Assert.False(set.Voters.WhatsApp);
    }

    [Fact]
    public void Production_OffersProvider_OnlyWhenFlagAndCredentialsExist()
    {
        var set = Evaluate(ConfiguredProviders(), Environments.Production);

        Assert.True(set.Officers.Google);
        Assert.True(set.Voters.Google);
        Assert.True(set.Voters.Sms);
        Assert.True(set.Voters.Voice);
        Assert.True(set.Voters.WhatsApp);
        Assert.True(set.Officers.Facebook);
        Assert.False(set.Officers.Telegram);
        Assert.True(set.Voters.Telegram);
    }

    [Fact]
    public void AudiencesStayIndependent()
    {
        var values = ConfiguredProviders();
        values["SignIn:Officers:Google"] = "false";
        values["SignIn:Voters:EmailCode"] = "false";
        values["SignIn:Voters:Sms"] = "false";

        var set = Evaluate(values, Environments.Production);

        Assert.False(set.Officers.Google);
        Assert.True(set.Voters.Google);
        Assert.False(set.Voters.EmailCode);
        Assert.False(set.Voters.Sms);
        Assert.True(set.Voters.Voice);
        Assert.True(set.Officers.Local);
    }

    [Fact]
    public void Production_ClientIdOnly_StillOffersOfficerGoogleOneTap()
    {
        var values = ConfiguredProviders();
        values.Remove("GoogleClientSecret");

        var set = Evaluate(values, Environments.Production);

        Assert.True(set.Officers.Google);
        Assert.True(set.Voters.Google);
        Assert.False(SignInCapabilities.OfficerGoogleRedirectReady(
            new ConfigurationBuilder().AddInMemoryCollection(values).Build()));
    }

    [Fact]
    public void Production_SecretWithoutClientId_DoesNotOfferOfficerGoogle()
    {
        var values = ConfiguredProviders();
        values.Remove("ClientEnv:googleClientId");

        var set = Evaluate(values, Environments.Production);

        Assert.False(set.Officers.Google);
    }

    [Fact]
    public void PlaceholderCredentials_DoNotCountAsConfigured()
    {
        var values = ConfiguredProviders();
        values["ClientEnv:googleClientId"] = "<GOOGLE-CLIENT-ID>";

        var set = Evaluate(values, Environments.Production);

        Assert.False(set.Officers.Google);
        Assert.True(set.Voters.Google);
    }

    [Fact]
    public void LegacyTelegramSwitch_EnablesOfficerTelegram_WhenNewKeyIsOmitted()
    {
        var values = ConfiguredProviders();
        values["ClientEnv:enableTelegramLogin"] = "true";

        var set = Evaluate(values, Environments.Production);

        Assert.True(set.Officers.Telegram);
    }

    [Fact]
    public void ExplicitOfficerTelegramFalse_WinsOverLegacySwitch()
    {
        var values = ConfiguredProviders();
        values["ClientEnv:enableTelegramLogin"] = "true";
        values["SignIn:Officers:Telegram"] = "false";

        var set = Evaluate(values, Environments.Production);

        Assert.False(set.Officers.Telegram);
        Assert.True(set.Voters.Telegram);
    }

    [Fact]
    public void PhoneCodeOff_HidesEveryTransport()
    {
        var values = ConfiguredProviders();
        values["SignIn:Voters:PhoneCode"] = "false";

        var set = Evaluate(values, Environments.Production);

        Assert.False(set.Voters.Sms);
        Assert.False(set.Voters.Voice);
        Assert.False(set.Voters.WhatsApp);
        Assert.True(set.Voters.EmailCode);
    }

    [Fact]
    public void AllowsVoterRequest_RejectsWrongPairs_AndKeepsTheOtherType()
    {
        var voters = new VoterSignInCapabilities
        {
            EmailCode = true,
            PhoneCode = true,
            Sms = true,
            Voice = false,
            WhatsApp = true,
            DirectCode = false
        };

        Assert.False(SignInCapabilities.AllowsVoterRequest(voters, "E", "sms"));
        Assert.False(SignInCapabilities.AllowsVoterRequest(voters, "P", "email"));
        Assert.False(SignInCapabilities.AllowsVoterRequest(voters, "P", "voice"));
        Assert.False(SignInCapabilities.AllowsVoterRequest(voters, "C", "email"));
        Assert.False(SignInCapabilities.AllowsVoterRequest(voters, "X", "email"));
        Assert.True(SignInCapabilities.AllowsVoterRequest(voters, "E", "email"));
        Assert.True(SignInCapabilities.AllowsVoterRequest(voters, "P", "sms"));
        Assert.True(SignInCapabilities.AllowsVoterRequest(voters, "P", "whatsapp"));
    }

    [Fact]
    public void Testing_DoesNotRequireProviderSecrets()
    {
        var set = Evaluate(new Dictionary<string, string?>(), "Testing");

        Assert.True(set.Officers.Google);
        Assert.True(set.Voters.Sms);
        Assert.False(set.Officers.Telegram);
    }

    private static SignInCapabilitySet Evaluate(Dictionary<string, string?> values, string environment)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var host = new Mock<IHostEnvironment>();
        host.Setup(item => item.EnvironmentName).Returns(environment);
        return SignInCapabilities.Evaluate(configuration, host.Object);
    }

    private static Dictionary<string, string?> ConfiguredProviders() => new()
    {
        ["ClientEnv:googleClientId"] = "officer-client",
        ["GoogleClientSecret"] = "officer-secret",
        ["Google:ClientId"] = "voter-client",
        ["ClientEnv:facebookAppId"] = "fb-app",
        ["ClientEnv:kakaoApiJsKey"] = "kakao-js",
        ["Telegram:BotToken"] = "telegram-token",
        ["ClientEnv:telegramBotUsername"] = "tallyj_bot",
        ["Twilio:AccountSid"] = "AC123",
        ["Twilio:AuthToken"] = "twilio-token",
        ["Twilio:FromNumber"] = "+15550001111",
        ["GreenApi:IdInstance"] = "1100",
        ["GreenApi:ApiToken"] = "green-token"
    };
}
