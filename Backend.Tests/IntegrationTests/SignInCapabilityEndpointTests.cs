using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Backend.DTOs.OnlineVoting;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Backend.Tests.IntegrationTests;

public class SignInCapabilityEndpointTests : IClassFixture<SignInRestrictedFactory>
{
    private readonly HttpClient _client;

    public SignInCapabilityEndpointTests(SignInRestrictedFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
    }

    [Fact]
    public async Task Capabilities_ReflectIndependentAudiences()
    {
        var response = await _client.GetAsync("/api/auth/sign-in-capabilities");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        json.GetProperty("officers").GetProperty("local").GetBoolean().Should().BeFalse();
        json.GetProperty("officers").GetProperty("google").GetBoolean().Should().BeFalse();
        json.GetProperty("voters").GetProperty("emailCode").GetBoolean().Should().BeFalse();
        json.GetProperty("voters").GetProperty("google").GetBoolean().Should().BeTrue();
        json.GetProperty("voters").GetProperty("directCode").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task DisabledOfficerLocal_RejectsLoginAndVerify_WithoutTouchingTellerPasscode()
    {
        var login = await PostAsync("/api/auth/login", new { email = "a@b.c", password = "whatever-password" });
        var verify = await PostAsync("/api/auth/verify2fa", new { email = "a@b.c", password = "whatever-password", code = "123456" });
        var teller = await PostAsync("/api/auth/teller-login", new { electionGuid = Guid.NewGuid(), passcode = "nope" });

        login.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await login.Content.ReadAsStringAsync()).Should().Contain("auth.errors.signInMethodDisabled");
        verify.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await verify.Content.ReadAsStringAsync()).Should().Contain("auth.errors.signInMethodDisabled");
        (await teller.Content.ReadAsStringAsync()).Should().NotContain("auth.errors.signInMethodDisabled");
    }

    [Fact]
    public async Task DisabledOfficerGoogle_DoesNotBlockVoterGoogle_AndEmailCodeStaysBlocked()
    {
        var officerGoogle = await _client.GetAsync("/api/auth/google/login");
        var voterGoogle = await PostAsync("/api/online-voting/googleAuth", new { credential = "not-a-token" });
        var emailCode = await PostAsync("/api/online-voting/requestCode", new RequestCodeDto
        {
            VoterId = "voter@example.com",
            VoterIdType = "E",
            DeliveryMethod = "email"
        });

        officerGoogle.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await officerGoogle.Content.ReadAsStringAsync()).Should().Contain("auth.errors.signInMethodDisabled");
        (await voterGoogle.Content.ReadAsStringAsync()).Should().NotContain("voting.auth.methodDisabled");
        emailCode.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await emailCode.Content.ReadAsStringAsync()).Should().Contain("voting.auth.methodDisabled");
    }

    private Task<HttpResponseMessage> PostAsync(string url, object body)
    {
        var json = JsonSerializer.Serialize(body);
        return _client.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
    }
}

public class SignInRestrictedFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SignIn:Officers:Local"] = "false",
                ["SignIn:Officers:Google"] = "false",
                ["SignIn:Voters:EmailCode"] = "false",
                ["SignIn:Voters:Google"] = "true"
            });
        });
    }
}
