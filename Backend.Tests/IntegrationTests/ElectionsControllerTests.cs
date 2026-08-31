using System.Net;
using System.Net.Http.Json;
using Backend.Context;
using Backend.DTOs.Elections;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backend.Tests.IntegrationTests;

public class ElectionsControllerTests : IntegrationTestBase
{
    public ElectionsControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetElections_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await GetAsync("/api/elections/getElections");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetElections_WithAuth_ReturnsOk()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var response = await GetAsync("/api/elections/getElections");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetElections_ReturnsPaginatedResponse()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var response = await GetAsync("/api/elections/getElections?pageNumber=1&pageSize=10");
        response.EnsureSuccessStatusCode();

        var result = await DeserializeResponseAsync<PaginatedResponse<ElectionSummaryDto>>(response);

        Assert.NotNull(result);
        Assert.True(result.PageNumber >= 1);
        Assert.True(result.PageSize > 0);
    }

    [Fact]
    public async Task CreateElection_WithValidData_ReturnsCreated()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Test Election",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 5
        };

        var response = await PostJsonAsync("/api/elections/createElection", createDto);
        
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("Test Election", result.Data.Name);
        Assert.Equal(5, result.Data.NumberToElect);
    }

    [Fact]
    public async Task CreateElection_WithInvalidData_ReturnsBadRequest()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "",
            NumberToElect = -1
        };

        var response = await PostJsonAsync("/api/elections/createElection", createDto);
        
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetElectionById_WithValidGuid_ReturnsElection()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Test Election for Get",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        };

        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        
        // Add diagnostic info
        if (!createResponse.IsSuccessStatusCode)
        {
            var errorContent = await createResponse.Content.ReadAsStringAsync();
            throw new Exception($"Create election failed: {createResponse.StatusCode}, Content: {errorContent}");
        }
        
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        var response = await GetAsync($"/api/elections/{electionGuid}/election");
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal(electionGuid, result.Data!.ElectionGuid);
        Assert.Equal("Test Election for Get", result.Data.Name);
    }

    [Fact]
    public async Task GetElectionById_WithInvalidGuid_ReturnsNotFound()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var response = await GetAsync($"/api/elections/{Guid.NewGuid()}/election");
        
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetElectionStats_WithValidElection_ReturnsZeroCounts()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Stats Test Election",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        };

        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        var response = await GetAsync($"/api/elections/{electionGuid}/stats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeResponseAsync<ApiResponse<ElectionStatsDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(0, result.Data.VoterCount);
        Assert.Equal(0, result.Data.BallotCount);
        Assert.Equal(0, result.Data.LocationCount);
    }

    [Fact]
    public async Task GetElectionStats_WithInvalidGuid_ReturnsNotFound()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var response = await GetAsync($"/api/elections/{Guid.NewGuid()}/stats");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetElectionStatus_WithValidElection_ReturnsStatus()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Status Test Election",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        };

        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        var response = await GetAsync($"/api/elections/{electionGuid}/status");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeResponseAsync<ApiResponse<ElectionStatusDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(electionGuid, result.Data.ElectionGuid);
        Assert.Equal("Status Test Election", result.Data.Name);
        Assert.Equal(0, result.Data.RegisteredVoters);
        Assert.Equal(0, result.Data.BallotsSubmitted);
        Assert.True(result.Data.IsActive);
    }

    [Fact]
    public async Task GetElectionStatus_WithInvalidGuid_ReturnsNotFound()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var response = await GetAsync($"/api/elections/{Guid.NewGuid()}/status");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateElection_WithValidData_ReturnsOk()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Original Name",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        };

        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        var updateDto = new UpdateElectionDto
        {
            Name = "Updated Name",
            NumberToElect = 7,
            DateOfElection = DateTime.UtcNow.AddDays(60),
            ElectionStage = Backend.Enumerations.ElectionStage.ProcessingBallots
        };

        var response = await PutJsonAsync($"/api/elections/{electionGuid}/updateElection", updateDto);
        
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Updated Name", result.Data!.Name);
        Assert.Equal(7, result.Data.NumberToElect);
    }

    [Fact]
    public async Task ChangeElectionStage_SequentialAdvancement_ReturnsOk()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Stage Change Election",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        };

        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        foreach (var stage in new[]
                 {
                     ElectionStage.GatheringBallots,
                     ElectionStage.ProcessingBallots
                 })
        {
            var stageDto = new ChangeElectionStageDto { ElectionStage = stage };
            var response = await PutJsonAsync($"/api/elections/{electionGuid}/stage", stageDto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var result = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(response);
            Assert.NotNull(result);
            Assert.True(result.Success);
            Assert.Equal(stage, result.Data!.ElectionStage);
        }
    }

    [Fact]
    public async Task ChangeElectionStage_ToFinalizedWithoutReadiness_ReturnsBadRequest()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Finalize Without Readiness Election",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        };

        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        foreach (var stage in new[] { ElectionStage.GatheringBallots, ElectionStage.ProcessingBallots })
        {
            var stageDto = new ChangeElectionStageDto { ElectionStage = stage };
            var response = await PutJsonAsync($"/api/elections/{electionGuid}/stage", stageDto);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        var finalizeResponse = await PutJsonAsync(
            $"/api/elections/{electionGuid}/stage",
            new ChangeElectionStageDto { ElectionStage = ElectionStage.Finalized });

        Assert.Equal(HttpStatusCode.BadRequest, finalizeResponse.StatusCode);
    }

    [Fact]
    public async Task ChangeElectionStage_WithInvalidEnum_ReturnsBadRequest()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Invalid Stage Election",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        };

        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        var content = new StringContent(
            """{"electionStage":"NotARealStage"}""",
            System.Text.Encoding.UTF8,
            "application/json");
        var response = await Client.PutAsync($"/api/elections/{electionGuid}/stage", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateElection_WithOwnedElection_ReturnsCreatedTestCopy()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Original For Duplicate",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 5
        };

        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var sourceGuid = createResult!.Data!.ElectionGuid;

        var response = await PostJsonAsync(
            $"/api/elections/{sourceGuid}/duplicateElection",
            new DuplicateElectionDto { Name = "API Test Copy" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var result = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.NotEqual(sourceGuid, result.Data.ElectionGuid);
        Assert.Equal("API Test Copy", result.Data.Name);
        Assert.True(result.Data.ShowAsTest);
        Assert.Equal(ElectionStage.SettingUp, result.Data.ElectionStage);
    }

    [Fact]
    public async Task DuplicateElection_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await PostJsonAsync(
            $"/api/elections/{Guid.NewGuid()}/duplicateElection",
            new DuplicateElectionDto());

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DuplicateElection_WhenUserIsNotOwner_ReturnsForbidden()
    {
        var ownerToken = await GetAuthTokenAsync();
        SetAuthToken(ownerToken);

        var createDto = new CreateElectionDto
        {
            Name = "Owner Only Election",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        };
        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var sourceGuid = createResult!.Data!.ElectionGuid;

        var otherToken = await GetAuthTokenAsync("test@tallyj.com", "Tester1234!X");
        SetAuthToken(otherToken);

        var response = await PostJsonAsync(
            $"/api/elections/{sourceGuid}/duplicateElection",
            new DuplicateElectionDto());

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ResetElection_WithShowAsTestElection_ReturnsOkAndSettingUp()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "API Test Reset",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 5,
            ShowAsTest = true
        };

        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        var stageResponse = await PutJsonAsync(
            $"/api/elections/{electionGuid}/stage",
            new ChangeElectionStageDto { ElectionStage = ElectionStage.GatheringBallots });
        Assert.Equal(HttpStatusCode.OK, stageResponse.StatusCode);

        var response = await PostJsonAsync($"/api/elections/{electionGuid}/resetElection", new { });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal(electionGuid, result.Data.ElectionGuid);
        Assert.True(result.Data.ShowAsTest);
        Assert.Equal(ElectionStage.SettingUp, result.Data.ElectionStage);
        Assert.Equal("API Test Reset", result.Data.Name);
    }

    [Fact]
    public async Task ResetElection_WhenShowAsTestFalse_ReturnsBadRequest()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Live Election Reset Refused",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3,
            ShowAsTest = false
        };
        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        var response = await PostJsonAsync($"/api/elections/{electionGuid}/resetElection", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(response);
        Assert.False(result!.Success);
        Assert.Contains("test", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResetElection_WhenShowAsTestNull_ReturnsBadRequest()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Unset Test Flag Reset Refused",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        };
        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        var response = await PostJsonAsync($"/api/elections/{electionGuid}/resetElection", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var result = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(response);
        Assert.False(result!.Success);
    }

    [Fact]
    public async Task ResetElection_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await PostJsonAsync(
            $"/api/elections/{Guid.NewGuid()}/resetElection",
            new { });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ResetElection_WhenUserIsNotOwner_ReturnsForbidden()
    {
        var ownerToken = await GetAuthTokenAsync();
        SetAuthToken(ownerToken);

        var createDto = new CreateElectionDto
        {
            Name = "Owner Only Reset",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3,
            ShowAsTest = true
        };
        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        var otherToken = await GetAuthTokenAsync("test@tallyj.com", "Tester1234!X");
        SetAuthToken(otherToken);

        var response = await PostJsonAsync($"/api/elections/{electionGuid}/resetElection", new { });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task ResetElection_RemovesRuntimeRowsAndKeepsPeopleLocationsAndSettings()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Runtime Wipe Test",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 7,
            NumberExtra = 2,
            Convenor = "Local Assembly",
            ShowAsTest = true
        };
        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;
        var locationGuid = Guid.NewGuid();
        var personGuid = Guid.NewGuid();
        var ballotGuid = Guid.NewGuid();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            db.Locations.Add(new Location
            {
                LocationGuid = locationGuid,
                ElectionGuid = electionGuid,
                Name = "Main Hall",
                LocationTallyStatus = LocationTallyStatus.Complete,
                BallotsCollected = 4
            });
            db.People.Add(new Person
            {
                PersonGuid = personGuid,
                ElectionGuid = electionGuid,
                FirstName = "Ada",
                LastName = "Lovelace",
                CanVote = true,
                RegistrationTime = DateTimeOffset.UtcNow,
                VotingLocationGuid = locationGuid,
                VotingMethod = "P",
                EnvNum = 3,
                Teller1 = "Pat",
                HasOnlineBallot = true,
                RowVersion = new byte[8]
            });
            db.Ballots.Add(new Ballot
            {
                BallotGuid = ballotGuid,
                LocationGuid = locationGuid,
                StatusCode = BallotStatus.Ok,
                ComputerCode = "A1",
                BallotNumAtComputer = 1,
                RowVersion = new byte[8]
            });
            db.Results.Add(new Result
            {
                ElectionGuid = electionGuid,
                PersonGuid = personGuid,
                Rank = 1,
                Section = "E",
                VoteCount = 5
            });
            await db.SaveChangesAsync();
        }

        var response = await PostJsonAsync($"/api/elections/{electionGuid}/resetElection", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var election = await db.Elections.SingleAsync(e => e.ElectionGuid == electionGuid);
            Assert.True(election.ShowAsTest);
            Assert.Equal(ElectionStage.SettingUp, election.ElectionStage);
            Assert.Equal("Runtime Wipe Test", election.Name);
            Assert.Equal(7, election.NumberToElect);
            Assert.Equal(2, election.NumberExtra);
            Assert.Equal("Local Assembly", election.Convenor);

            var people = await db.People.Where(p => p.ElectionGuid == electionGuid).ToListAsync();
            Assert.Single(people);
            Assert.Equal(personGuid, people[0].PersonGuid);
            Assert.Equal("Ada", people[0].FirstName);
            Assert.Null(people[0].RegistrationTime);
            Assert.Null(people[0].HasOnlineBallot);

            var locations = await db.Locations.Where(l => l.ElectionGuid == electionGuid).ToListAsync();
            Assert.Single(locations);
            Assert.Equal(locationGuid, locations[0].LocationGuid);
            Assert.Equal("Main Hall", locations[0].Name);
            Assert.Null(locations[0].LocationTallyStatus);
            Assert.Null(locations[0].BallotsCollected);

            Assert.Equal(0, await db.Ballots.CountAsync(b => b.LocationGuid == locationGuid));
            Assert.Equal(0, await db.Results.CountAsync(r => r.ElectionGuid == electionGuid));
        }
    }

    [Fact]
    public async Task DeleteElection_WithValidGuid_ReturnsNoContent()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var createDto = new CreateElectionDto
        {
            Name = "Election to Delete",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = ElectionTypeCode.LSA,
            NumberToElect = 3
        };

        var createResponse = await PostJsonAsync("/api/elections/createElection", createDto);
        var createResult = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(createResponse);
        var electionGuid = createResult!.Data!.ElectionGuid;

        var response = await DeleteAsync($"/api/elections/{electionGuid}/deleteElection");
        
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await GetAsync($"/api/elections/{electionGuid}/election");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}



