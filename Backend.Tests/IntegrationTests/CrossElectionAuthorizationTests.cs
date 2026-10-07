using System.Net;
using Backend.Context;
using Backend.DTOs.Ballots;
using Backend.DTOs.Elections;
using Backend.DTOs.Locations;
using Backend.DTOs.People;
using Backend.DTOs.Results;
using Backend.DTOs.Tellers;
using Backend.DTOs.Votes;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Models;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

/// <summary>
/// Cross-election authorization: a signed-in account, guest teller, or
/// online voter must not read or change an election they do not belong to.
/// </summary>
public class CrossElectionAuthorizationTests : IntegrationTestBase
{
    public CrossElectionAuthorizationTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task OwnerOfB_IsDeniedOnElectionA_ReadsAndWrites()
    {
        var seeded = await CreateTwoOwnedElectionsAsync();
        SetAuthToken(seeded.OwnerBToken);

        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/People/{seeded.ElectionA}/getPeople")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync($"/api/People/{seeded.PersonA}/getPerson")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostJsonAsync("/api/People/createPerson", new CreatePersonDto
        {
            ElectionGuid = seeded.ElectionA,
            LastName = "Intruder"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PutJsonAsync($"/api/People/{seeded.PersonA}/updatePerson", new UpdatePersonDto
        {
            LastName = "Changed"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/Ballots/{seeded.ElectionA}/ballots")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync($"/api/Ballots/{seeded.BallotA}/ballot")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostJsonAsync("/api/Ballots/createBallot", new CreateBallotDto
        {
            ElectionGuid = seeded.ElectionA,
            LocationGuid = seeded.LocationA,
            ComputerCode = "A"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/Votes/{seeded.ElectionA}/getVotesByElection")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync($"/api/Votes/{seeded.BallotA}/getVotesByBallot")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync($"/api/Votes/{seeded.VoteA}/getVote")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostJsonAsync("/api/Votes/createVote", new CreateVoteDto
        {
            BallotGuid = seeded.BallotA,
            PersonGuid = seeded.PersonA
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/Results/election/{seeded.ElectionA}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostJsonAsync($"/api/Results/election/{seeded.ElectionA}/calculate", new { })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/{seeded.ElectionA}/locations/getLocations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostJsonAsync($"/api/{seeded.ElectionA}/locations/createLocation", new CreateLocationDto
        {
            ElectionGuid = seeded.ElectionA,
            Name = "Side Room"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/{seeded.ElectionA}/tellers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostJsonAsync($"/api/{seeded.ElectionA}/tellers/createTeller", new CreateTellerDto
        {
            ElectionGuid = seeded.ElectionA,
            Name = "Intruder"
        })).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/Reports/{seeded.ElectionA}/available")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostJsonAsync($"/api/report-exports/{seeded.ElectionA}", new ExportRequest
        {
            Format = "csv",
            ElectionId = seeded.ElectionA
        })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/Import/exportElectionToJson/{seeded.ElectionA}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PostJsonAsync("/api/Dashboard/moreInfoStatic", seeded.ElectionA)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync($"/api/security-audit-logs?electionGuid={seeded.ElectionA}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync($"/api/security-audit-logs/{seeded.AuditForA}")).StatusCode);
    }

    [Fact]
    public async Task GuestTellerForA_IsLimitedToOwnElectionOperationalActions()
    {
        var seeded = await CreateTwoOwnedElectionsAsync();
        using var scope = Factory.Services.CreateScope();
        var tokens = scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        SetAuthToken(tokens.GenerateTellerToken(seeded.ElectionA));

        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/People/{seeded.ElectionA}/getPeople")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/Ballots/{seeded.ElectionA}/ballots")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/Results/election/{seeded.ElectionA}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/{seeded.ElectionA}/locations/getLocations")).StatusCode);

        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/People/{seeded.ElectionB}/getPeople")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/Ballots/{seeded.ElectionB}/ballots")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/{seeded.ElectionA}/tellers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/{seeded.ElectionB}/tellers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await PostJsonAsync($"/api/Results/election/{seeded.ElectionA}/calculate", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/PeopleImport/{seeded.ElectionA}/files")).StatusCode);
    }

    [Fact]
    public async Task OnlineVoter_IsDeniedOnTellerApis_AndCanReadOwnSession()
    {
        var seeded = await CreateTwoOwnedElectionsAsync();
        SetAuthToken(GenerateVoterToken("voter-cross-election"));

        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/People/{seeded.ElectionA}/getPeople")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/Ballots/{seeded.ElectionA}/ballots")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/Votes/{seeded.ElectionA}/getVotesByElection")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/Results/election/{seeded.ElectionA}")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/{seeded.ElectionA}/locations/getLocations")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync($"/api/{seeded.ElectionA}/tellers")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync("/api/security-audit-logs")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync("/api/eligibility/eligibility-reasons")).StatusCode);

        var me = await GetAsync("/api/online-voting/me");
        Assert.Equal(HttpStatusCode.OK, me.StatusCode);
    }

    [Fact]
    public async Task NonSuperAdmin_IsDeniedOnSiteWideAuditLogs()
    {
        var seeded = await CreateTwoOwnedElectionsAsync();
        SetAuthToken(seeded.OwnerAToken);

        Assert.Equal(HttpStatusCode.Forbidden, (await GetAsync("/api/security-audit-logs")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync($"/api/security-audit-logs/{seeded.SiteAudit}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/security-audit-logs?electionGuid={seeded.ElectionA}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await GetAsync($"/api/security-audit-logs?electionGuid={seeded.ElectionB}")).StatusCode);
    }

    [Fact]
    public async Task Owner_AndSuperAdmin_StillSucceed()
    {
        var seeded = await CreateTwoOwnedElectionsAsync();
        SetAuthToken(seeded.OwnerAToken);

        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/People/{seeded.ElectionA}/getPeople")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await PostJsonAsync("/api/People/createPerson", new CreatePersonDto
        {
            ElectionGuid = seeded.ElectionA,
            LastName = "OwnerAdded"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/Ballots/{seeded.ElectionA}/ballots")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/Votes/{seeded.ElectionA}/getVotesByElection")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/Results/election/{seeded.ElectionA}")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/{seeded.ElectionA}/locations/getLocations")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await PostJsonAsync($"/api/{seeded.ElectionA}/locations/createLocation", new CreateLocationDto
        {
            ElectionGuid = seeded.ElectionA,
            Name = "Annex"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/{seeded.ElectionA}/tellers")).StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await PostJsonAsync($"/api/{seeded.ElectionA}/tellers/createTeller", new CreateTellerDto
        {
            ElectionGuid = seeded.ElectionA,
            Name = "Second Teller"
        })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/Reports/{seeded.ElectionA}/available")).StatusCode);

        var export = await PostJsonAsync($"/api/report-exports/{seeded.ElectionA}", new ExportRequest
        {
            Format = "csv",
            ElectionId = seeded.ElectionA
        });
        Assert.NotEqual(HttpStatusCode.Forbidden, export.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, export.StatusCode);

        var superToken = await GetAuthTokenAsync("admin@tallyj.test", "TestPass123!");
        SetAuthToken(superToken);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/People/{seeded.ElectionA}/getPeople")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/{seeded.ElectionA}/tellers")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync("/api/security-audit-logs")).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await GetAsync($"/api/security-audit-logs/{seeded.SiteAudit}")).StatusCode);
    }

    private async Task<SeededElections> CreateTwoOwnedElectionsAsync()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var ownerAToken = await GetAuthTokenAsync($"owner-a-{suffix}@tallyj.test", "TestPass123!");
        SetAuthToken(ownerAToken);
        var electionA = await CreateElectionAsync($"Election A {suffix}");

        var ownerBToken = await GetAuthTokenAsync($"owner-b-{suffix}@tallyj.test", "TestPass123!");
        SetAuthToken(ownerBToken);
        var electionB = await CreateElectionAsync($"Election B {suffix}");

        var personA = Guid.NewGuid();
        var locationA = Guid.NewGuid();
        var ballotA = Guid.NewGuid();
        int voteA;
        int auditForA;
        int siteAudit;

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Locations.Add(new Location
            {
                LocationGuid = locationA,
                ElectionGuid = electionA,
                Name = "Main Hall"
            });
            db.People.Add(new Person
            {
                PersonGuid = personA,
                ElectionGuid = electionA,
                FirstName = "Ada",
                LastName = "Lovelace",
                Email = "ada@example.test",
                Phone = "555-0100",
                RowVersion = new byte[8]
            });
            db.Ballots.Add(new Ballot
            {
                BallotGuid = ballotA,
                LocationGuid = locationA,
                StatusCode = BallotStatus.Ok,
                ComputerCode = "A",
                RowVersion = new byte[8]
            });
            var vote = new Vote
            {
                BallotGuid = ballotA,
                PersonGuid = personA,
                PositionOnBallot = 1,
                VoteStatus = VoteStatus.Ok,
                RowVersion = new byte[8]
            };
            db.Votes.Add(vote);
            db.Tellers.Add(new Teller
            {
                ElectionGuid = electionA,
                Name = "Head",
                RowVersion = new byte[8]
            });
            var electionLog = new SecurityAuditLog
            {
                Timestamp = DateTimeOffset.UtcNow,
                EventType = SecurityEventType.AccessDenied,
                ElectionGuid = electionA,
                Details = "election-scoped",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Info
            };
            var siteLog = new SecurityAuditLog
            {
                Timestamp = DateTimeOffset.UtcNow,
                EventType = SecurityEventType.LoginSuccess,
                Details = "site-wide",
                IsSuspicious = false,
                Severity = SecurityEventSeverity.Info
            };
            db.SecurityAuditLogs.Add(electionLog);
            db.SecurityAuditLogs.Add(siteLog);
            await db.SaveChangesAsync();
            voteA = vote.RowId;
            auditForA = electionLog.Id;
            siteAudit = siteLog.Id;
        }

        return new SeededElections(ownerAToken, ownerBToken, electionA, electionB, personA, locationA, ballotA, voteA, auditForA, siteAudit);
    }

    private async Task<Guid> CreateElectionAsync(string name)
    {
        var response = await PostJsonAsync("/api/elections/createElection", new CreateElectionDto
        {
            Name = name,
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3,
            ShowAsTest = true
        });
        response.EnsureSuccessStatusCode();
        var created = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(response);
        return created!.Data!.ElectionGuid;
    }

    private sealed record SeededElections(
        string OwnerAToken,
        string OwnerBToken,
        Guid ElectionA,
        Guid ElectionB,
        Guid PersonA,
        Guid LocationA,
        Guid BallotA,
        int VoteA,
        int AuditForA,
        int SiteAudit);
}
