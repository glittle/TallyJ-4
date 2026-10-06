using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Backend.Context;
using Backend.Controllers;
using Backend.DTOs.Auth;
using Backend.DTOs.Elections;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Middleware;
using Backend.Models;
using Backend.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Guest teller login: per-IP rate limit, per-election lockout, and passcode rules.
/// </summary>
public class TellerLoginProtectionTests : IntegrationTestBase
{
    private int _ipHost = 1;

    public TellerLoginProtectionTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task TellerLogin_SameIp_SixthAttempt_Returns429_AndDoesNotCountTheRejectedAttempt()
    {
        ResetRateLimit();
        var electionGuid = await CreateOpenElectionAsync("secret-code", "Rate Limited Teller Election");
        const string ip = "203.0.113.80";

        HttpResponseMessage? last = null;
        for (var i = 0; i < 6; i++)
        {
            last = await PostTellerLoginAsync(electionGuid, "wrong-code", ip);
        }

        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        var body = await last.Content.ReadAsStringAsync();
        body.Should().Contain(RateLimitingMiddleware.TooManyRequestsKey);

        var failures = await ReadLockoutAsync(electionGuid);
        failures.Should().NotBeNull();
        failures!.ConsecutiveFailures.Should().Be(RateLimitingMiddleware.TellerLoginIpMaxRequests);
        failures.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task TellerLogin_TenthFailure_LocksElection_AcrossIps_WritesOneAuditRow()
    {
        ResetRateLimit();
        var electionGuid = await CreateOpenElectionAsync("secret-code", "Locked Teller Election");

        HttpResponseMessage? lastFailure = null;
        for (var i = 0; i < 10; i++)
        {
            lastFailure = await PostTellerLoginAsync(electionGuid, "wrong-code", NextIp());
        }

        lastFailure!.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await lastFailure.Content.ReadAsStringAsync()).Should().Contain(AuthController.TellerLoginLockedKey);

        var lockout = await ReadLockoutAsync(electionGuid);
        lockout.Should().NotBeNull();
        lockout!.ConsecutiveFailures.Should().Be(10);
        lockout.LockedUntil.Should().BeAfter(DateTimeOffset.UtcNow);

        var correctFromNewIp = await PostTellerLoginAsync(electionGuid, "secret-code", NextIp());
        correctFromNewIp.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await correctFromNewIp.Content.ReadAsStringAsync()).Should().Contain(AuthController.TellerLoginLockedKey);

        ResetRateLimit();
        var afterRateLimitReset = await PostTellerLoginAsync(electionGuid, "secret-code", NextIp());
        (await afterRateLimitReset.Content.ReadAsStringAsync()).Should().Contain(AuthController.TellerLoginLockedKey);

        var auditCount = await CountLockoutAuditsAsync(electionGuid);
        auditCount.Should().Be(1);
    }

    [Fact]
    public async Task TellerLogin_SuccessfulLogin_ResetsConsecutiveFailures()
    {
        ResetRateLimit();
        var electionGuid = await CreateOpenElectionAsync("secret-code", "Reset Teller Election");

        for (var i = 0; i < 9; i++)
        {
            var failed = await PostTellerLoginAsync(electionGuid, "wrong-code", NextIp());
            (await failed.Content.ReadAsStringAsync()).Should().Contain(AuthController.InvalidElectionOrPasscodeKey);
        }

        var success = await PostTellerLoginAsync(electionGuid, "secret-code", NextIp());
        success.StatusCode.Should().Be(HttpStatusCode.OK);
        (await ReadLockoutAsync(electionGuid))!.ConsecutiveFailures.Should().Be(0);

        for (var i = 0; i < 9; i++)
        {
            var failed = await PostTellerLoginAsync(electionGuid, "wrong-code", NextIp());
            (await failed.Content.ReadAsStringAsync()).Should().Contain(AuthController.InvalidElectionOrPasscodeKey);
        }

        (await ReadLockoutAsync(electionGuid))!.LockedUntil.Should().BeNull();

        var locks = await PostTellerLoginAsync(electionGuid, "wrong-code", NextIp());
        (await locks.Content.ReadAsStringAsync()).Should().Contain(AuthController.TellerLoginLockedKey);
    }

    [Fact]
    public async Task TellerLogin_ExpiredLock_StartsTheFailureCountAgain()
    {
        ResetRateLimit();
        var electionGuid = await CreateOpenElectionAsync("secret-code", "Expired Lock Election");
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            context.TellerLoginLockouts.Add(new TellerLoginLockout
            {
                ElectionGuid = electionGuid,
                ConsecutiveFailures = 10,
                LockedUntil = DateTimeOffset.UtcNow.AddMinutes(-1)
            });
            await context.SaveChangesAsync();
        }

        var response = await PostTellerLoginAsync(electionGuid, "wrong-code", NextIp());
        (await response.Content.ReadAsStringAsync()).Should().Contain(AuthController.InvalidElectionOrPasscodeKey);

        var row = await ReadLockoutAsync(electionGuid);
        row!.ConsecutiveFailures.Should().Be(1);
        row.LockedUntil.Should().BeNull();
    }

    [Fact]
    public async Task TellerLogin_UnknownElectionAndWrongPasscode_ReturnTheSameBody()
    {
        ResetRateLimit();
        var electionGuid = await CreateOpenElectionAsync("secret-code", "Uniform Reply Election");
        var closedGuid = await CreateElectionAsync("secret-code", "Closed Uniform Election", listedForPublicAsOf: null);

        var missing = await PostTellerLoginAsync(Guid.NewGuid(), "whatever", NextIp());
        var wrong = await PostTellerLoginAsync(electionGuid, "wrong-code", NextIp());
        var wrongOnClosed = await PostTellerLoginAsync(closedGuid, "wrong-code", NextIp());

        var missingBody = await missing.Content.ReadAsStringAsync();
        var wrongBody = await wrong.Content.ReadAsStringAsync();
        var wrongOnClosedBody = await wrongOnClosed.Content.ReadAsStringAsync();

        missing.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        wrong.StatusCode.Should().Be(missing.StatusCode);
        wrongOnClosed.StatusCode.Should().Be(missing.StatusCode);
        wrongBody.Should().Be(missingBody);
        wrongOnClosedBody.Should().Be(missingBody);
        missingBody.Should().Contain(AuthController.InvalidElectionOrPasscodeKey);

        var correctOnClosed = await PostTellerLoginAsync(closedGuid, "secret-code", NextIp());
        var correctOnClosedBody = await correctOnClosed.Content.ReadAsStringAsync();
        correctOnClosedBody.Should().NotBe(missingBody);
        correctOnClosedBody.Should().Contain("not currently open for teller access");
    }

    [Fact]
    public async Task TellerLogin_OwnerAccount_CanUpdateElectionDuringLockout()
    {
        ResetRateLimit();
        Client.DefaultRequestHeaders.Authorization = null;
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createResponse = await PostJsonAsync("/api/elections/createElection", new CreateElectionDto
        {
            Name = "Owner During Lockout",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ApiResponse<ElectionDto>>(JsonOptions);
        var electionGuid = created!.Data!.ElectionGuid;

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var election = await context.Elections.SingleAsync(item => item.ElectionGuid == electionGuid);
            election.ElectionPasscode = "secret-code";
            election.ListedForPublicAsOf = DateTimeOffset.UtcNow.AddMinutes(-5);
            await context.SaveChangesAsync();
        }

        Factory.Services.GetRequiredService<IComputerAssignmentService>()
            .AssignCode(electionGuid, $"main-{electionGuid}", $"conn-{electionGuid}", isMainTeller: true);

        Client.DefaultRequestHeaders.Authorization = null;
        for (var i = 0; i < 10; i++)
        {
            await PostTellerLoginAsync(electionGuid, "wrong-code", NextIp());
        }

        var locked = await PostTellerLoginAsync(electionGuid, "secret-code", NextIp());
        (await locked.Content.ReadAsStringAsync()).Should().Contain(AuthController.TellerLoginLockedKey);

        Client.DefaultRequestHeaders.Authorization = null;
        var ownerToken = await GetAuthTokenAsync();
        SetAuthToken(ownerToken);

        var update = await PutJsonAsync($"/api/elections/{electionGuid}/updateElection", new UpdateElectionDto
        {
            Name = "Owner Still Working",
            NumberToElect = 4,
            DateOfElection = DateTime.UtcNow.AddDays(40)
        });
        update.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await GetAsync($"/api/elections/{electionGuid}/election");
        fetched.StatusCode.Should().Be(HttpStatusCode.OK);
        var electionBody = await fetched.Content.ReadAsStringAsync();
        electionBody.Should().Contain("Owner Still Working");
        electionBody.Should().Contain("tellerLoginLockedUntil");
    }

    [Fact]
    public async Task CreateElection_ShortPasscode_IsRejected_AndLegacyShortPasscodeStillLogsIn()
    {
        ResetRateLimit();
        Client.DefaultRequestHeaders.Authorization = null;
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var tooShort = await PostJsonAsync("/api/elections/createElection", new CreateElectionDto
        {
            Name = "Too Short Passcode",
            DateOfElection = DateTime.UtcNow.AddDays(10),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3,
            ElectionPasscode = "short"
        });
        tooShort.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await tooShort.Content.ReadAsStringAsync()).Should().Contain(TellerPasscodeRules.MinLengthMessageKey);

        var created = await PostJsonAsync("/api/elections/createElection", new CreateElectionDto
        {
            Name = "Legacy Short Passcode",
            DateOfElection = DateTime.UtcNow.AddDays(10),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        });
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdBody = await created.Content.ReadFromJsonAsync<ApiResponse<ElectionDto>>(JsonOptions);
        var electionGuid = createdBody!.Data!.ElectionGuid;

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var election = await context.Elections.SingleAsync(item => item.ElectionGuid == electionGuid);
            election.ElectionPasscode = "abc";
            election.ListedForPublicAsOf = DateTimeOffset.UtcNow.AddMinutes(-5);
            await context.SaveChangesAsync();
        }

        Factory.Services.GetRequiredService<IComputerAssignmentService>()
            .AssignCode(electionGuid, $"main-{electionGuid}", $"conn-{electionGuid}", isMainTeller: true);

        Client.DefaultRequestHeaders.Authorization = null;
        var login = await PostTellerLoginAsync(electionGuid, "abc", NextIp());
        login.StatusCode.Should().Be(HttpStatusCode.OK);

        SetAuthToken(token);
        var keepShort = await PutJsonAsync($"/api/elections/{electionGuid}/updateElection", new UpdateElectionDto
        {
            Name = "Legacy Short Passcode",
            ElectionPasscode = "abc",
            NumberToElect = 3,
            DateOfElection = DateTime.UtcNow.AddDays(10)
        });
        keepShort.StatusCode.Should().Be(HttpStatusCode.OK);

        var changeShort = await PutJsonAsync($"/api/elections/{electionGuid}/updateElection", new UpdateElectionDto
        {
            Name = "Legacy Short Passcode",
            ElectionPasscode = "no",
            NumberToElect = 3,
            DateOfElection = DateTime.UtcNow.AddDays(10)
        });
        changeShort.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await changeShort.Content.ReadAsStringAsync()).Should().Contain(TellerPasscodeRules.MinLengthMessageKey);

        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var election = await context.Elections.SingleAsync(item => item.ElectionGuid == electionGuid);
            election.ElectionPasscode.Should().Be("abc");
        }
    }

    private string NextIp()
    {
        var host = _ipHost++;
        return $"203.0.113.{host}";
    }

    private async Task<Guid> CreateOpenElectionAsync(string passcode, string name)
    {
        return await CreateElectionAsync(passcode, name, DateTimeOffset.UtcNow.AddMinutes(-5));
    }

    private async Task<Guid> CreateElectionAsync(string passcode, string name, DateTimeOffset? listedForPublicAsOf)
    {
        var electionGuid = Guid.NewGuid();
        using (var scope = Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            context.Elections.Add(new Election
            {
                ElectionGuid = electionGuid,
                Name = name,
                ElectionType = ElectionTypeEnum.LSA.Code,
                ElectionMode = ElectionModeEnum.Normal.Code,
                NumberToElect = 9,
                DateOfElection = DateTime.UtcNow.AddDays(1),
                ElectionStage = ElectionStage.SettingUp,
                ElectionPasscode = passcode,
                ListedForPublicAsOf = listedForPublicAsOf,
                OwnerLoginId = "admin@tallyj.test",
                ShowAsTest = true,
                RowVersion = new byte[8]
            });
            await context.SaveChangesAsync();
        }

        if (listedForPublicAsOf != null)
        {
            Factory.Services.GetRequiredService<IComputerAssignmentService>()
                .AssignCode(electionGuid, $"main-{electionGuid}", $"conn-{electionGuid}", isMainTeller: true);
        }

        return electionGuid;
    }

    private async Task<HttpResponseMessage> PostTellerLoginAsync(Guid electionGuid, string accessCode, string forwardedFor)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/teller-login")
        {
            Content = new StringContent(
                JsonSerializer.Serialize(new TellerLoginRequest
                {
                    ElectionGuid = electionGuid,
                    AccessCode = accessCode
                }),
                Encoding.UTF8,
                "application/json")
        };
        request.Headers.TryAddWithoutValidation("X-Forwarded-For", forwardedFor);
        if (Client.DefaultRequestHeaders.Authorization is AuthenticationHeaderValue authorization)
        {
            request.Headers.Authorization = authorization;
        }

        return await Client.SendAsync(request);
    }

    private async Task<TellerLoginLockout?> ReadLockoutAsync(Guid electionGuid)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        return await context.TellerLoginLockouts.AsNoTracking()
            .SingleOrDefaultAsync(row => row.ElectionGuid == electionGuid);
    }

    private async Task<int> CountLockoutAuditsAsync(Guid electionGuid)
    {
        using var scope = Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        return await context.SecurityAuditLogs.CountAsync(row =>
            row.ElectionGuid == electionGuid &&
            row.EventType == Backend.SecurityEventType.TellerLoginLocked);
    }
}
