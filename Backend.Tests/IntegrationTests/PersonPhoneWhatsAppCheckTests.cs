using System.Net;
using Backend.Context;
using Backend.DTOs.Elections;
using Backend.DTOs.People;
using Backend.Enumerations;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Person-detail WhatsApp check. Anonymous is 401. Tests do not call a live GreenAPI account.
/// </summary>
public class PersonPhoneWhatsAppCheckTests : IntegrationTestBase
{
    public PersonPhoneWhatsAppCheckTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CheckWhatsApp_Anonymous_Unauthorized()
    {
        var response = await PostJsonAsync(
            $"/api/People/{Guid.NewGuid()}/checkWhatsApp",
            new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CheckWhatsApp_NotConfigured_DoesNotPersist()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        const string phone = "+14168972720";
        var personGuid = await SeedPersonWithPhoneAsync(phone);

        var response = await PostJsonAsync($"/api/People/{personGuid}/checkWhatsApp", new { });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await DeserializeResponseAsync<ApiResponse<PersonPhoneOnlineVoterDto>>(response);
        Assert.Equal(PeopleMessageKeys.PhoneWhatsAppNotConfigured, body!.Message);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var row = await db.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal("P", row.VoterIdType);
        Assert.Null(row.WhatsAppStatus);
    }

    [Fact]
    public async Task CheckWhatsAppSelected_Anonymous_Unauthorized()
    {
        var response = await PostJsonAsync(
            $"/api/People/{Guid.NewGuid()}/checkWhatsAppSelected",
            new CheckSelectedWhatsAppDto { PersonGuids = [Guid.NewGuid()] });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CheckWhatsAppSelected_OverMax_BadRequest()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var dto = new CheckSelectedWhatsAppDto
        {
            PersonGuids = Enumerable.Range(0, CheckSelectedWhatsAppDto.MaxSelectedPeople + 1)
                .Select(_ => Guid.NewGuid())
                .ToList()
        };
        var response = await PostJsonAsync($"/api/People/{Guid.NewGuid()}/checkWhatsAppSelected", dto);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CheckWhatsAppSelected_NotConfigured_DoesNotPersist()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        const string phone = "+14168972721";
        var (electionGuid, personGuid) = await SeedElectionPersonWithPhoneAsync(phone);

        var response = await PostJsonAsync(
            $"/api/People/{electionGuid}/checkWhatsAppSelected",
            new CheckSelectedWhatsAppDto { PersonGuids = [personGuid] });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var body = await DeserializeResponseAsync<ApiResponse<CheckSelectedWhatsAppResultDto>>(response);
        Assert.Equal(PeopleMessageKeys.PhoneWhatsAppNotConfigured, body!.Message);

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
        var row = await db.OnlineVoters.SingleAsync(ov => ov.VoterId == phone);
        Assert.Equal("P", row.VoterIdType);
        Assert.Null(row.WhatsAppStatus);
    }

    private async Task<Guid> SeedPersonWithPhoneAsync(string phone)
    {
        var (_, personGuid) = await SeedElectionPersonWithPhoneAsync(phone);
        return personGuid;
    }

    private async Task<(Guid ElectionGuid, Guid PersonGuid)> SeedElectionPersonWithPhoneAsync(string phone)
    {
        var createElection = await PostJsonAsync("/api/elections/createElection", new CreateElectionDto
        {
            Name = $"WhatsApp check {Guid.NewGuid():N}",
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
        return (election.Data.ElectionGuid, person!.Data!.PersonGuid);
    }
}
