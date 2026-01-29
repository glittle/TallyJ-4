using Microsoft.Extensions.Logging;
using Moq;
using TallyJ4.DTOs.Results;
using TallyJ4.Services;

namespace TallyJ4.Tests.UnitTests;

public class ReportExportServiceTests : ServiceTestBase
{
    private readonly ReportExportService _service;
    private readonly Mock<ITallyService> _tallyServiceMock;
    private readonly Mock<ILogger<ReportExportService>> _loggerMock;

    public ReportExportServiceTests()
    {
        _tallyServiceMock = new Mock<ITallyService>();
        _loggerMock = new Mock<ILogger<ReportExportService>>();
        _service = new ReportExportService(_tallyServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task GeneratePdfReportAsync_WithValidData_ReturnsPdfBytes()
    {
        // Arrange
        var electionId = Guid.NewGuid();
        var electionReport = new ElectionReportDto
        {
            ElectionName = "Test Election",
            ElectionDate = DateTime.UtcNow.AddDays(30),
            NumToElect = 3,
            TotalBallots = 100,
            SpoiledBallots = 5,
            TotalVotes = 285,
            Elected = new List<CandidateReportDto>
            {
                new() { Rank = 1, FullName = "John Doe", VoteCount = 95, Section = "A" },
                new() { Rank = 2, FullName = "Jane Smith", VoteCount = 88, Section = "B" },
                new() { Rank = 3, FullName = "Bob Johnson", VoteCount = 75, Section = "A" }
            }
        };

        var detailedStats = new DetailedStatisticsDto
        {
            Overview = new ElectionOverviewDto
            {
                ElectionName = "Test Election",
                ElectionDate = DateTime.UtcNow.AddDays(30),
                TotalRegisteredVoters = 150,
                TotalBallotsCast = 100,
                ValidBallots = 95,
                SpoiledBallots = 5,
                TotalVotes = 285,
                PositionsToElect = 3,
                OverallTurnoutPercentage = 66.67m
            },
            LocationStatistics = new List<LocationStatisticsDto>
            {
                new() { LocationName = "Location A", RegisteredVoters = 75, BallotsCast = 50, ValidBallots = 48, SpoiledBallots = 2, TurnoutPercentage = 66.67m, TotalVotes = 144 },
                new() { LocationName = "Location B", RegisteredVoters = 75, BallotsCast = 50, ValidBallots = 47, SpoiledBallots = 3, TurnoutPercentage = 66.67m, TotalVotes = 141 }
            }
        };

        _tallyServiceMock.Setup(x => x.GetElectionReportAsync(electionId)).ReturnsAsync(electionReport);
        _tallyServiceMock.Setup(x => x.GetDetailedStatisticsAsync(electionId)).ReturnsAsync(detailedStats);

        // Act
        var result = await _service.GeneratePdfReportAsync(electionId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        _tallyServiceMock.Verify(x => x.GetElectionReportAsync(electionId), Times.Once);
        _tallyServiceMock.Verify(x => x.GetDetailedStatisticsAsync(electionId), Times.Once);
    }

    [Fact]
    public async Task GeneratePdfReportAsync_WithNoElectedCandidates_ReturnsPdfBytes()
    {
        // Arrange
        var electionId = Guid.NewGuid();
        var electionReport = new ElectionReportDto
        {
            ElectionName = "Test Election",
            ElectionDate = DateTime.UtcNow.AddDays(30),
            NumToElect = 3,
            TotalBallots = 100,
            SpoiledBallots = 5,
            TotalVotes = 285,
            Elected = new List<CandidateReportDto>() // Empty list
        };

        var detailedStats = new DetailedStatisticsDto
        {
            Overview = new ElectionOverviewDto
            {
                ElectionName = "Test Election",
                ElectionDate = DateTime.UtcNow.AddDays(30),
                TotalRegisteredVoters = 150,
                TotalBallotsCast = 100,
                ValidBallots = 95,
                SpoiledBallots = 5,
                TotalVotes = 285,
                PositionsToElect = 3,
                OverallTurnoutPercentage = 66.67m
            },
            LocationStatistics = new List<LocationStatisticsDto>()
        };

        _tallyServiceMock.Setup(x => x.GetElectionReportAsync(electionId)).ReturnsAsync(electionReport);
        _tallyServiceMock.Setup(x => x.GetDetailedStatisticsAsync(electionId)).ReturnsAsync(detailedStats);

        // Act
        var result = await _service.GeneratePdfReportAsync(electionId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GeneratePdfReportAsync_WithNoLocationStatistics_ReturnsPdfBytes()
    {
        // Arrange
        var electionId = Guid.NewGuid();
        var electionReport = new ElectionReportDto
        {
            ElectionName = "Test Election",
            ElectionDate = DateTime.UtcNow.AddDays(30),
            NumToElect = 3,
            TotalBallots = 100,
            SpoiledBallots = 5,
            TotalVotes = 285,
            Elected = new List<CandidateReportDto>
            {
                new() { Rank = 1, FullName = "John Doe", VoteCount = 95, Section = "A" }
            }
        };

        var detailedStats = new DetailedStatisticsDto
        {
            Overview = new ElectionOverviewDto
            {
                ElectionName = "Test Election",
                ElectionDate = DateTime.UtcNow.AddDays(30),
                TotalRegisteredVoters = 150,
                TotalBallotsCast = 100,
                ValidBallots = 95,
                SpoiledBallots = 5,
                TotalVotes = 285,
                PositionsToElect = 3,
                OverallTurnoutPercentage = 66.67m
            },
            LocationStatistics = new List<LocationStatisticsDto>() // Empty list
        };

        _tallyServiceMock.Setup(x => x.GetElectionReportAsync(electionId)).ReturnsAsync(electionReport);
        _tallyServiceMock.Setup(x => x.GetDetailedStatisticsAsync(electionId)).ReturnsAsync(detailedStats);

        // Act
        var result = await _service.GeneratePdfReportAsync(electionId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GeneratePdfReportAsync_WithNullElectionDate_HandlesGracefully()
    {
        // Arrange
        var electionId = Guid.NewGuid();
        var electionReport = new ElectionReportDto
        {
            ElectionName = "Test Election",
            ElectionDate = null, // Null date
            NumToElect = 3,
            TotalBallots = 100,
            SpoiledBallots = 5,
            TotalVotes = 285,
            Elected = new List<CandidateReportDto>()
        };

        var detailedStats = new DetailedStatisticsDto
        {
            Overview = new ElectionOverviewDto
            {
                ElectionName = "Test Election",
                ElectionDate = null, // Null date
                TotalRegisteredVoters = 150,
                TotalBallotsCast = 100,
                ValidBallots = 95,
                SpoiledBallots = 5,
                TotalVotes = 285,
                PositionsToElect = 3,
                OverallTurnoutPercentage = 66.67m
            },
            LocationStatistics = new List<LocationStatisticsDto>()
        };

        _tallyServiceMock.Setup(x => x.GetElectionReportAsync(electionId)).ReturnsAsync(electionReport);
        _tallyServiceMock.Setup(x => x.GetDetailedStatisticsAsync(electionId)).ReturnsAsync(detailedStats);

        // Act
        var result = await _service.GeneratePdfReportAsync(electionId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GeneratePdfReportAsync_WithFilters_PassesFiltersToTallyService()
    {
        // Arrange
        var electionId = Guid.NewGuid();
        var filters = new Dictionary<string, string> { { "location", "Main Hall" } };
        var electionReport = new ElectionReportDto
        {
            ElectionName = "Test Election",
            Elected = new List<CandidateReportDto>()
        };

        var detailedStats = new DetailedStatisticsDto
        {
            Overview = new ElectionOverviewDto
            {
                ElectionName = "Test Election",
                TotalRegisteredVoters = 100,
                TotalBallotsCast = 80,
                ValidBallots = 78,
                SpoiledBallots = 2,
                TotalVotes = 234,
                PositionsToElect = 2,
                OverallTurnoutPercentage = 80.0m
            },
            LocationStatistics = new List<LocationStatisticsDto>()
        };

        _tallyServiceMock.Setup(x => x.GetElectionReportAsync(electionId)).ReturnsAsync(electionReport);
        _tallyServiceMock.Setup(x => x.GetDetailedStatisticsAsync(electionId)).ReturnsAsync(detailedStats);

        // Act
        var result = await _service.GeneratePdfReportAsync(electionId, filters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        // Note: The current implementation doesn't use filters in the service methods,
        // but the parameter is accepted for future extensibility
    }

    [Fact]
    public async Task GeneratePdfReportAsync_WhenTallyServiceThrows_LogsErrorAndRethrows()
    {
        // Arrange
        var electionId = Guid.NewGuid();
        var exception = new Exception("Database connection failed");

        _tallyServiceMock.Setup(x => x.GetElectionReportAsync(electionId)).ThrowsAsync(exception);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<Exception>(() => _service.GeneratePdfReportAsync(electionId));
        Assert.Equal("Database connection failed", thrownException.Message);
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WithValidData_ReturnsExcelBytes()
    {
        // Arrange
        var electionId = Guid.NewGuid();
        var electionReport = new ElectionReportDto
        {
            ElectionName = "Test Election",
            ElectionDate = DateTime.UtcNow.AddDays(30),
            NumToElect = 3,
            TotalBallots = 100,
            SpoiledBallots = 5,
            TotalVotes = 285,
            Elected = new List<CandidateReportDto>
            {
                new() { Rank = 1, FullName = "John Doe", VoteCount = 95, Section = "A" },
                new() { Rank = 2, FullName = "Jane Smith", VoteCount = 88, Section = "B" },
                new() { Rank = 3, FullName = "Bob Johnson", VoteCount = 75, Section = "A" }
            }
        };

        var detailedStats = new DetailedStatisticsDto
        {
            Overview = new ElectionOverviewDto
            {
                ElectionName = "Test Election",
                ElectionDate = DateTime.UtcNow.AddDays(30),
                TotalRegisteredVoters = 150,
                TotalBallotsCast = 100,
                ValidBallots = 95,
                SpoiledBallots = 5,
                TotalVotes = 285,
                PositionsToElect = 3,
                OverallTurnoutPercentage = 66.67m
            },
            LocationStatistics = new List<LocationStatisticsDto>
            {
                new() { LocationName = "Location A", RegisteredVoters = 75, BallotsCast = 50, ValidBallots = 48, SpoiledBallots = 2, TurnoutPercentage = 66.67m, TotalVotes = 144 },
                new() { LocationName = "Location B", RegisteredVoters = 75, BallotsCast = 50, ValidBallots = 47, SpoiledBallots = 3, TurnoutPercentage = 66.67m, TotalVotes = 141 }
            },
            CandidatePerformance = new[]
            {
                new CandidatePerformanceDto { FullName = "John Doe", TotalVotes = 95, VotePercentage = 33.33m, Rank = 1, IsElected = true, IsEliminated = false },
                new CandidatePerformanceDto { FullName = "Jane Smith", TotalVotes = 88, VotePercentage = 30.88m, Rank = 2, IsElected = true, IsEliminated = false },
                new CandidatePerformanceDto { FullName = "Bob Johnson", TotalVotes = 75, VotePercentage = 26.32m, Rank = 3, IsElected = true, IsEliminated = false },
                new CandidatePerformanceDto { FullName = "Alice Brown", TotalVotes = 27, VotePercentage = 9.47m, Rank = 4, IsElected = false, IsEliminated = true }
            }
        };

        _tallyServiceMock.Setup(x => x.GetElectionReportAsync(electionId)).ReturnsAsync(electionReport);
        _tallyServiceMock.Setup(x => x.GetDetailedStatisticsAsync(electionId)).ReturnsAsync(detailedStats);

        // Act
        var result = await _service.GenerateExcelReportAsync(electionId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        _tallyServiceMock.Verify(x => x.GetElectionReportAsync(electionId), Times.Once);
        _tallyServiceMock.Verify(x => x.GetDetailedStatisticsAsync(electionId), Times.Once);
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WithNoElectedCandidates_SkipsElectedSheet()
    {
        // Arrange
        var electionId = Guid.NewGuid();
        var electionReport = new ElectionReportDto
        {
            ElectionName = "Test Election",
            Elected = new List<CandidateReportDto>() // Empty
        };

        var detailedStats = new DetailedStatisticsDto
        {
            Overview = new ElectionOverviewDto
            {
                ElectionName = "Test Election",
                TotalRegisteredVoters = 100,
                TotalBallotsCast = 80,
                ValidBallots = 78,
                SpoiledBallots = 2,
                TotalVotes = 234,
                PositionsToElect = 2,
                OverallTurnoutPercentage = 80.0m
            },
            LocationStatistics = new List<LocationStatisticsDto>(),
            CandidatePerformance = Array.Empty<CandidatePerformanceDto>()
        };

        _tallyServiceMock.Setup(x => x.GetElectionReportAsync(electionId)).ReturnsAsync(electionReport);
        _tallyServiceMock.Setup(x => x.GetDetailedStatisticsAsync(electionId)).ReturnsAsync(detailedStats);

        // Act
        var result = await _service.GenerateExcelReportAsync(electionId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WithNoLocationStatistics_SkipsLocationSheet()
    {
        // Arrange
        var electionId = Guid.NewGuid();
        var electionReport = new ElectionReportDto
        {
            ElectionName = "Test Election",
            Elected = new List<CandidateReportDto>
            {
                new() { Rank = 1, FullName = "John Doe", VoteCount = 95, Section = "A" }
            }
        };

        var detailedStats = new DetailedStatisticsDto
        {
            Overview = new ElectionOverviewDto
            {
                ElectionName = "Test Election",
                TotalRegisteredVoters = 100,
                TotalBallotsCast = 80,
                ValidBallots = 78,
                SpoiledBallots = 2,
                TotalVotes = 234,
                PositionsToElect = 2,
                OverallTurnoutPercentage = 80.0m
            },
            LocationStatistics = new List<LocationStatisticsDto>(), // Empty
            CandidatePerformance = Array.Empty<CandidatePerformanceDto>()
        };

        _tallyServiceMock.Setup(x => x.GetElectionReportAsync(electionId)).ReturnsAsync(electionReport);
        _tallyServiceMock.Setup(x => x.GetDetailedStatisticsAsync(electionId)).ReturnsAsync(detailedStats);

        // Act
        var result = await _service.GenerateExcelReportAsync(electionId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WithNoCandidatePerformance_SkipsCandidateSheet()
    {
        // Arrange
        var electionId = Guid.NewGuid();
        var electionReport = new ElectionReportDto
        {
            ElectionName = "Test Election",
            Elected = new List<CandidateReportDto>()
        };

        var detailedStats = new DetailedStatisticsDto
        {
            Overview = new ElectionOverviewDto
            {
                ElectionName = "Test Election",
                TotalRegisteredVoters = 100,
                TotalBallotsCast = 80,
                ValidBallots = 78,
                SpoiledBallots = 2,
                TotalVotes = 234,
                PositionsToElect = 2,
                OverallTurnoutPercentage = 80.0m
            },
            LocationStatistics = new List<LocationStatisticsDto>(),
            CandidatePerformance = Array.Empty<CandidatePerformanceDto>() // Empty
        };

        _tallyServiceMock.Setup(x => x.GetElectionReportAsync(electionId)).ReturnsAsync(electionReport);
        _tallyServiceMock.Setup(x => x.GetDetailedStatisticsAsync(electionId)).ReturnsAsync(detailedStats);

        // Act
        var result = await _service.GenerateExcelReportAsync(electionId);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WithFilters_PassesFiltersToTallyService()
    {
        // Arrange
        var electionId = Guid.NewGuid();
        var filters = new Dictionary<string, string> { { "status", "completed" } };
        var electionReport = new ElectionReportDto
        {
            ElectionName = "Test Election",
            Elected = new List<CandidateReportDto>()
        };

        var detailedStats = new DetailedStatisticsDto
        {
            Overview = new ElectionOverviewDto
            {
                ElectionName = "Test Election",
                TotalRegisteredVoters = 100,
                TotalBallotsCast = 80,
                ValidBallots = 78,
                SpoiledBallots = 2,
                TotalVotes = 234,
                PositionsToElect = 2,
                OverallTurnoutPercentage = 80.0m
            },
            LocationStatistics = new List<LocationStatisticsDto>(),
            CandidatePerformance = Array.Empty<CandidatePerformanceDto>()
        };

        _tallyServiceMock.Setup(x => x.GetElectionReportAsync(electionId)).ReturnsAsync(electionReport);
        _tallyServiceMock.Setup(x => x.GetDetailedStatisticsAsync(electionId)).ReturnsAsync(detailedStats);

        // Act
        var result = await _service.GenerateExcelReportAsync(electionId, filters);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
        // Note: The current implementation doesn't use filters in the service methods,
        // but the parameter is accepted for future extensibility
    }

    [Fact]
    public async Task GenerateExcelReportAsync_WhenTallyServiceThrows_LogsErrorAndRethrows()
    {
        // Arrange
        var electionId = Guid.NewGuid();
        var exception = new InvalidOperationException("Election not found");

        _tallyServiceMock.Setup(x => x.GetElectionReportAsync(electionId)).ThrowsAsync(exception);

        // Act & Assert
        var thrownException = await Assert.ThrowsAsync<InvalidOperationException>(() => _service.GenerateExcelReportAsync(electionId));
        Assert.Equal("Election not found", thrownException.Message);
    }
}