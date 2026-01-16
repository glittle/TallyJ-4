using Microsoft.Extensions.Logging;
using Moq;
using TallyJ4.DTOs.Elections;
using TallyJ4.EF.Models;
using TallyJ4.Services;

namespace TallyJ4.Tests.UnitTests;

public class ElectionServiceTests : ServiceTestBase
{
    private readonly ElectionService _service;
    private readonly Mock<ILogger<ElectionService>> _loggerMock;

    public ElectionServiceTests()
    {
        _loggerMock = new Mock<ILogger<ElectionService>>();
        _service = new ElectionService(Context, Mapper, _loggerMock.Object);
    }

    [Fact]
    public async Task CreateElectionAsync_CreatesElectionSuccessfully()
    {
        var createDto = new CreateElectionDto
        {
            Name = "Test Election",
            DateOfElection = DateTime.UtcNow.AddDays(30),
            ElectionType = "Standard",
            NumberToElect = 5
        };

        var result = await _service.CreateElectionAsync(createDto);

        Assert.NotNull(result);
        Assert.Equal("Test Election", result.Name);
        Assert.Equal(5, result.NumberToElect);
        Assert.Equal("Setup", result.TallyStatus);
        Assert.NotEqual(Guid.Empty, result.ElectionGuid);

        var electionInDb = Context.Elections.FirstOrDefault(e => e.ElectionGuid == result.ElectionGuid);
        Assert.NotNull(electionInDb);
        Assert.Equal("Test Election", electionInDb.Name);
    }

    [Fact]
    public async Task GetElectionByGuidAsync_WithValidGuid_ReturnsElection()
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Test Election",
            ElectionType = "Standard",
            NumberToElect = 3,
            TallyStatus = "Setup",
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        await Context.SaveChangesAsync();

        var result = await _service.GetElectionByGuidAsync(election.ElectionGuid);

        Assert.NotNull(result);
        Assert.Equal(election.ElectionGuid, result.ElectionGuid);
        Assert.Equal("Test Election", result.Name);
        Assert.Equal(3, result.NumberToElect);
    }

    [Fact]
    public async Task GetElectionByGuidAsync_WithInvalidGuid_ReturnsNull()
    {
        var result = await _service.GetElectionByGuidAsync(Guid.NewGuid());

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateElectionAsync_WithValidGuid_UpdatesElection()
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Original Name",
            ElectionType = "Standard",
            NumberToElect = 3,
            TallyStatus = "Setup",
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        await Context.SaveChangesAsync();

        var updateDto = new UpdateElectionDto
        {
            Name = "Updated Name",
            NumberToElect = 7,
            DateOfElection = DateTime.UtcNow.AddDays(20),
            TallyStatus = "InProgress"
        };

        var result = await _service.UpdateElectionAsync(election.ElectionGuid, updateDto);

        Assert.NotNull(result);
        Assert.Equal("Updated Name", result.Name);
        Assert.Equal(7, result.NumberToElect);
        Assert.Equal("InProgress", result.TallyStatus);
    }

    [Fact]
    public async Task UpdateElectionAsync_WithInvalidGuid_ReturnsNull()
    {
        var updateDto = new UpdateElectionDto
        {
            Name = "Updated Name",
            NumberToElect = 5
        };

        var result = await _service.UpdateElectionAsync(Guid.NewGuid(), updateDto);

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteElectionAsync_WithValidGuid_DeletesElection()
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = "Election to Delete",
            ElectionType = "Standard",
            NumberToElect = 3,
            TallyStatus = "Setup",
            DateOfElection = DateTime.UtcNow.AddDays(10),
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        await Context.SaveChangesAsync();

        var result = await _service.DeleteElectionAsync(election.ElectionGuid);

        Assert.True(result);

        var deletedElection = Context.Elections.FirstOrDefault(e => e.ElectionGuid == election.ElectionGuid);
        Assert.Null(deletedElection);
    }

    [Fact]
    public async Task DeleteElectionAsync_WithInvalidGuid_ReturnsFalse()
    {
        var result = await _service.DeleteElectionAsync(Guid.NewGuid());

        Assert.False(result);
    }

    [Fact]
    public async Task GetElectionsAsync_ReturnsPaginatedResults()
    {
        for (int i = 0; i < 15; i++)
        {
            var election = new Election
            {
                ElectionGuid = Guid.NewGuid(),
                Name = $"Election {i}",
                ElectionType = "Standard",
                NumberToElect = 3,
                TallyStatus = i % 2 == 0 ? "Setup" : "InProgress",
                DateOfElection = DateTime.UtcNow.AddDays(i),
                RowVersion = new byte[8]
            };
            Context.Elections.Add(election);
        }
        await Context.SaveChangesAsync();

        var result = await _service.GetElectionsAsync(pageNumber: 1, pageSize: 10);

        Assert.NotNull(result);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(10, result.PageSize);
        Assert.Equal(15, result.TotalCount);
        Assert.Equal(2, result.TotalPages);
        Assert.Equal(10, result.Items.Count);
    }

    [Fact]
    public async Task GetElectionsAsync_WithStatusFilter_ReturnsFilteredResults()
    {
        for (int i = 0; i < 10; i++)
        {
            var election = new Election
            {
                ElectionGuid = Guid.NewGuid(),
                Name = $"Election {i}",
                ElectionType = "Standard",
                NumberToElect = 3,
                TallyStatus = i % 2 == 0 ? "Setup" : "InProgress",
                DateOfElection = DateTime.UtcNow.AddDays(i),
                RowVersion = new byte[8]
            };
            Context.Elections.Add(election);
        }
        await Context.SaveChangesAsync();

        var result = await _service.GetElectionsAsync(pageNumber: 1, pageSize: 10, status: "Setup");

        Assert.NotNull(result);
        Assert.Equal(5, result.TotalCount);
        Assert.All(result.Items, e => Assert.Equal("Setup", e.TallyStatus));
    }
}
