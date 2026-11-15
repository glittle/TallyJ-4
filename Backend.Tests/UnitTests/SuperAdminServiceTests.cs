using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Backend.Entities;
using Backend.Enumerations;
using Backend.DTOs.SuperAdmin;
using Backend.Identity;
using Backend.Services;

namespace Backend.Tests.UnitTests;

public class SuperAdminServiceTests : ServiceTestBase
{
    private readonly SuperAdminService _service;

    public SuperAdminServiceTests()
    {
        var logger = new Mock<ILogger<SuperAdminService>>();
        var userManager = new Mock<UserManager<AppUser>>(
            Mock.Of<IUserStore<AppUser>>(),
            null!, null!, null!, null!, null!, null!, null!, null!);
        var securityAudit = new Mock<ISecurityAuditService>();
        _service = new SuperAdminService(
            Context,
            userManager.Object,
            securityAudit.Object,
            logger.Object);
    }

    private async Task SeedElections()
    {
        var elections = new[]
        {
            new Election
            {
                ElectionGuid = Guid.NewGuid(),
                Name = "Setup Past",
                ElectionStage = ElectionStage.SettingUp,
                DateOfElection = DateTime.UtcNow.AddDays(-1),
                RowVersion = new byte[8]
            },
            new Election
            {
                ElectionGuid = Guid.NewGuid(),
                Name = "Upcoming Election",
                ElectionStage = ElectionStage.SettingUp,
                DateOfElection = DateTime.UtcNow.AddDays(30),
                RowVersion = new byte[8]
            },
            new Election
            {
                ElectionGuid = Guid.NewGuid(),
                Name = "Gathering Election",
                ElectionStage = ElectionStage.GatheringBallots,
                DateOfElection = DateTime.UtcNow.AddDays(10),
                RowVersion = new byte[8]
            },
            new Election
            {
                ElectionGuid = Guid.NewGuid(),
                Name = "Processing Election",
                ElectionStage = ElectionStage.ProcessingBallots,
                DateOfElection = DateTime.UtcNow.AddDays(-60),
                RowVersion = new byte[8]
            },
            new Election
            {
                ElectionGuid = Guid.NewGuid(),
                Name = "Alpha Finalized",
                ElectionStage = ElectionStage.Finalized,
                DateOfElection = DateTime.UtcNow.AddDays(5),
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

        Assert.Equal(5, summary.TotalElections);
        Assert.Equal(2, summary.OpenElections);
        Assert.Equal(2, summary.UpcomingElections);
        Assert.Equal(1, summary.CompletedElections);
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

        Assert.Equal(5, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(2, result.PageSize);
    }

    [Fact]
    public async Task GetElectionsAsync_FilterByStatus()
    {
        await SeedElections();

        var filter = new SuperAdminElectionFilterDto { Status = "ProcessingBallots" };
        var result = await _service.GetElectionsAsync(filter);

        Assert.Single(result.Items);
        Assert.Equal(ElectionStage.ProcessingBallots, result.Items[0].ElectionStage);
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

        Assert.Equal("Alpha Finalized", result.Items[0].Name);
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
            ElectionStage = ElectionStage.SettingUp,
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



