using System.Net;
using Backend.DTOs.Elections;
using Backend.Domain.Enumerations;
using Backend.Models;

namespace Backend.Tests.IntegrationTests;

public class ElectionsControllerTests : IntegrationTestBase
{
    public ElectionsControllerTests(CustomWebApplicationFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetElections_WithoutAuth_ReturnsUnauthorized()
    {
        var response = await GetAsync("/api/elections/getMyElections");

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected Unauthorized but got {response.StatusCode}. Response content: {content}");
        }

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetElections_WithAuth_ReturnsOk()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var response = await GetAsync("/api/elections/getMyElections");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected OK but got {response.StatusCode}. Response content: {content}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task GetElections_ReturnsPaginatedResponse()
    {
        var token = await GetAuthTokenAsync();
        SetAuthToken(token);

        var response = await GetAsync("/api/elections/getMyElections?pageNumber=1&pageSize=10");
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

        if (response.StatusCode != HttpStatusCode.Created)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected Created but got {response.StatusCode}. Response content: {content}");
        }

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

        if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected BadRequest but got {response.StatusCode}. Response content: {content}");
        }

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

        var response = await GetAsync($"/api/elections/{electionGuid}/electionDetails");

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected OK but got {response.StatusCode}. Response content: {content}");
        }

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

        var response = await GetAsync($"/api/elections/{Guid.NewGuid()}/electionDetails");

        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected NotFound but got {response.StatusCode}. Response content: {content}");
        }

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
            TallyStatus = "Processing"
        };

        var response = await PutJsonAsync($"/api/elections/{electionGuid}/updateElection", updateDto);

        if (response.StatusCode != HttpStatusCode.OK)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected OK but got {response.StatusCode}. Response content: {content}");
        }

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await DeserializeResponseAsync<ApiResponse<ElectionDto>>(response);
        Assert.NotNull(result);
        Assert.True(result.Success);
        Assert.Equal("Updated Name", result.Data!.Name);
        Assert.Equal(7, result.Data.NumberToElect);
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

        if (response.StatusCode != HttpStatusCode.NoContent)
        {
            var content = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected NoContent but got {response.StatusCode}. Response content: {content}");
        }

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var getResponse = await GetAsync($"/api/elections/{electionGuid}/election");

        if (getResponse.StatusCode != HttpStatusCode.NotFound)
        {
            var content = await getResponse.Content.ReadAsStringAsync();
            Assert.Fail($"Expected NotFound but got {getResponse.StatusCode}. Response content: {content}");
        }

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}



