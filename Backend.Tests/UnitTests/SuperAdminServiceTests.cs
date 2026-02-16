using Microsoft.Extensions.Logging;
using Moq;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.DTOs.SuperAdmin;
using Backend.Services;

namespace Backend.Tests.UnitTests;

public class SuperAdminServiceTests : ServiceTestBase
{
    private readonly SuperAdminService _service;

    public SuperAdminServiceTests()
    {
        var logger = new Mock<ILogger<SuperAdminService>>();
        _service = new SuperAdminService(Context, logger.Object);
    }

    private async Task SeedElections()
    {
        var elections = new[]
        {
            new Election
            {
                ElectionGuid = Guid.NewGuid(),
                Name = "Open Election",
                TallyStatus = "Setup",
                DateOfElection = DateTime.UtcNow.AddDays(-1),
                RowVersion = new byte[8]
            },
            new Election
            {
                ElectionGuid = Guid.NewGuid(),
                Name = "Upcoming Election",
                TallyStatus = "Setup",
                DateOfElection = DateTime.UtcNow.AddDays(30),
                RowVersion = new byte[8]
            },
            new Election
            {
                ElectionGuid = Guid.NewGuid(),
                Name = "Completed Election",
                TallyStatus = "Complete",
                DateOfElection = DateTime.UtcNow.AddDays(-60),
                RowVersion = new byte[8]
            },
            new Election
            {
                ElectionGuid = Guid.NewGuid(),
                Name = "Archived Election",
                TallyStatus = "Archived",
                DateOfElection = DateTime.UtcNow.AddDays(-120),
                RowVersion = new byte[8]
            }
        };

        Context.Elections.AddRange(elections);
        await Context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetSummaryAsync_ReturnsCounts()
    {
        await SeedElections();

        var summary = await _service.GetSummaryAsync();

        Assert.Equal(4, summary.TotalElections);
        Assert.Equal(1, summary.OpenElections);
        Assert.Equal(1, summary.UpcomingElections);
        Assert.Equal(1, summary.CompletedElections);
        Assert.Equal(1, summary.ArchivedElections);
    }

    [Fact]
    public async Task GetSummaryAsync_EmptyDb_ReturnsZeros()
    {
        var summary = await _service.GetSummaryAsync();

        Assert.Equal(0, summary.TotalElections);
        Assert.Equal(0, summary.OpenElections);
    }

    [Fact]
    public async Task GetElectionsAsync_ReturnsPaginated()
    {
        await SeedElections();

        var filter = new SuperAdminElectionFilterDto { Page = 1, PageSize = 2 };
        var result = await _service.GetElectionsAsync(filter);

        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetElectionsAsync_FilterByStatus()
    {
        await SeedElections();

        var filter = new SuperAdminElectionFilterDto { Status = "Complete" };
        var result = await _service.GetElectionsAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal("Completed Election", result.Items[0].Name);
    }

    [Fact]
    public async Task GetElectionsAsync_SearchByName()
    {
        await SeedElections();

        var filter = new SuperAdminElectionFilterDto { Search = "Upcoming" };
        var result = await _service.GetElectionsAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal("Upcoming Election", result.Items[0].Name);
    }

    [Fact]
    public async Task GetElectionsAsync_SortByNameAsc()
    {
        await SeedElections();

        var filter = new SuperAdminElectionFilterDto { SortBy = "name", SortDirection = "asc" };
        var result = await _service.GetElectionsAsync(filter);

        Assert.Equal("Archived Election", result.Items[0].Name);
    }

    [Fact]
    public async Task GetElectionDetailAsync_ExistingElection_ReturnsDetail()
    {
        var guid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = guid,
            Name = "Detail Test",
            NumberToElect = 9,
            ElectionMode = "N",
            TallyStatus = "Setup",
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        var detail = await _service.GetElectionDetailAsync(guid);

        Assert.NotNull(detail);
        Assert.Equal("Detail Test", detail!.Name);
        Assert.Equal(9, detail.NumberToElect);
        Assert.Equal(ElectionModeCode.N, detail.ElectionMode);
    }

    [Fact]
    public async Task GetElectionDetailAsync_NonExistent_ReturnsNull()
    {
        var result = await _service.GetElectionDetailAsync(Guid.NewGuid());
        Assert.Null(result);
    }
}



