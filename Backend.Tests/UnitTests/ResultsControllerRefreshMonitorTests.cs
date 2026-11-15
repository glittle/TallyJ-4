using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Backend.Controllers;
using Backend.DTOs.Results;
using Backend.Services;

namespace Backend.Tests.UnitTests;

public class ResultsControllerRefreshMonitorTests
{
    private readonly Mock<ITallyService> _tally = new();
    private readonly Mock<ISignalRNotificationService> _signalR = new();
    private readonly ResultsController _controller;
    private readonly Guid _electionGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
    private readonly MonitorInfoDto _monitorInfo;

    public ResultsControllerRefreshMonitorTests()
    {
        _monitorInfo = new MonitorInfoDto { ElectionGuid = _electionGuid };
        _tally.Setup(s => s.GetMonitorInfoAsync(_electionGuid)).ReturnsAsync(_monitorInfo);
        _controller = new ResultsController(
            _tally.Object,
            _signalR.Object,
            Mock.Of<ILogger<ResultsController>>());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Unknown")]
    public async Task RefreshMonitor_WithoutRealCode_BroadcastsWithoutStamping(string? computerCode)
    {
        var result = await _controller.RefreshMonitor(_electionGuid, computerCode);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(_monitorInfo, ok.Value);
        _tally.Verify(
            s => s.RefreshComputerContactAsync(It.IsAny<Guid>(), It.IsAny<string>()),
            Times.Never);
        _tally.Verify(s => s.GetMonitorInfoAsync(_electionGuid), Times.Once);
        _signalR.Verify(s => s.SendMonitorUpdateAsync(_monitorInfo), Times.Once);
    }

    [Fact]
    public async Task RefreshMonitor_WithRealCode_StampsThenBroadcasts()
    {
        var result = await _controller.RefreshMonitor(_electionGuid, "a");

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Same(_monitorInfo, ok.Value);
        _tally.Verify(s => s.RefreshComputerContactAsync(_electionGuid, "a"), Times.Once);
        _signalR.Verify(s => s.SendMonitorUpdateAsync(_monitorInfo), Times.Once);
    }
}
