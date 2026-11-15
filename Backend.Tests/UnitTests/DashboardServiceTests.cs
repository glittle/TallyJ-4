using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Services;

namespace Backend.Tests.UnitTests;

public class DashboardServiceTests : ServiceTestBase
{
    private readonly Guid _userId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private readonly DashboardService _service;

    public DashboardServiceTests()
    {
        var logger = new Mock<ILogger<DashboardService>>();
        var accessor = new Mock<IHttpContextAccessor>();
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, _userId.ToString())
            ], "test"))
        };
        accessor.Setup(a => a.HttpContext).Returns(http);
        _service = new DashboardService(Context, logger.Object, accessor.Object);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_CountsByStage_IgnoresOtherUsers()
    {
        var otherUser = Guid.NewGuid();
        await AddElection(ElectionStage.SettingUp, _userId, DateTimeOffset.UtcNow.AddDays(-2));
        await AddElection(ElectionStage.GatheringBallots, _userId, DateTimeOffset.UtcNow.AddDays(3));
        await AddElection(ElectionStage.ProcessingBallots, _userId, DateTimeOffset.UtcNow.AddDays(-1));
        await AddElection(ElectionStage.Finalized, _userId, DateTimeOffset.UtcNow.AddDays(1));
        await AddElection(ElectionStage.Finalized, otherUser, DateTimeOffset.UtcNow.AddDays(-10));

        var summary = await _service.GetDashboardSummaryAsync();

        Assert.Equal(2, summary.ActiveElectionCount);
        Assert.Equal(1, summary.CompletedElectionCount);
        Assert.Equal(4, summary.RecentElections.Count);
    }

    [Fact]
    public async Task GetDashboardSummaryAsync_NoUser_ReturnsZeros()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());
        var service = new DashboardService(
            Context,
            new Mock<ILogger<DashboardService>>().Object,
            accessor.Object);

        var summary = await service.GetDashboardSummaryAsync();

        Assert.Equal(0, summary.ActiveElectionCount);
        Assert.Equal(0, summary.CompletedElectionCount);
        Assert.Empty(summary.RecentElections);
    }

    private async Task AddElection(ElectionStage stage, Guid userId, DateTimeOffset date)
    {
        var electionGuid = Guid.NewGuid();
        Context.Elections.Add(new Election
        {
            ElectionGuid = electionGuid,
            Name = stage.ToString(),
            ElectionStage = stage,
            DateOfElection = date,
            RowVersion = new byte[8]
        });
        Context.JoinElectionUsers.Add(new JoinElectionUser
        {
            ElectionGuid = electionGuid,
            UserId = userId,
            Role = "Owner"
        });
        await Context.SaveChangesAsync();
    }
}
