using System.Net;
using Backend.Context;
using Backend.DTOs.Elections;
using Backend.DTOs.People;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Manual teller set of OnlineVoter.SmsStatus from person detail.
/// Same [Authorize] as other People writes — anonymous is 401.
/// </summary>
public class PersonPhoneSmsStatusTests : IntegrationTestBase
{
    public PersonPhoneSmsStatusTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task SetPhoneSmsStatus_Anonymous_Unauthorized()
    {
        var response = await PutJsonAsync(
            $"/api/People/{Guid.NewGuid()}/setPhoneSmsStatus",
            new SetPersonPhoneSmsStatusDto { SmsStatus = "OK" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SetPhoneSmsStatus_OkThenReason_UpdatesPRow()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        const string phone = "+14168972695";
        var personGuid = await SeedPersonWithPhoneAsync(phone);

        var ok = await PutJsonAsync(
            $"/api/People/{personGuid}/setPhoneSmsStatus",
            new SetPersonPhoneSmsStatusDto { SmsStatus = "ok" });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var okBody = await DeserializeResponseAsync<ApiResponse<PersonPhoneOnlineVoterDto>>(ok);
        Assert.Equal(OnlineVoterSmsStatus.Ok, okBody!.Data!.SmsStatus);

        var blocked = await PutJsonAsync(
            $"/api/People/{personGuid}/setPhoneSmsStatus",
            new SetPersonPhoneSmsStatusDto { SmsStatus = "landline" });
        Assert.Equal(HttpStatusCode.OK, blocked.StatusCode);
        var blockedBody = await DeserializeResponseAsync<ApiResponse<PersonPhoneOnlineVoterDto>>(blocked);
        Assert.Equal("landline", blockedBody!.Data!.SmsStatus);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var row = await db.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal("P", row.VoterIdType);
        Assert.Equal("landline", row.SmsStatus);
        Assert.False(OnlineVoterSmsStatus.AllowsPaidSend(row.SmsStatus));
    }

    private async Task<Guid> SeedPersonWithPhoneAsync(string phone)
    {
        var createElection = await PostJsonAsync("/api/elections/createElection", new CreateElectionDto
        {
            Name = $"SmsStatus set {Guid.NewGuid():N}",
            DateOfElection = DateTime.UtcNow.AddDays(1),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        });
        createElection.EnsureSuccessStatusCode();
        var election = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createElection);

        var createPerson = await PostJsonAsync("/api/People/createPerson", new CreatePersonDto
        {
            ElectionGuid = election!.Data!.ElectionGuid,
            LastName = "Smith",
            FirstName = "Pat",
            Phone = phone
        });
        createPerson.EnsureSuccessStatusCode();
        var person = await DeserializeResponseAsync<ApiResponse<PersonDto>>(createPerson);
        return person!.Data!.PersonGuid;
    }
}
