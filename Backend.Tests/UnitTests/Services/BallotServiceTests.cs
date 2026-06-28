using Microsoft.Extensions.Logging;
using Moq;
using Backend.DTOs.Ballots;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Services;

namespace Backend.Tests.UnitTests.Services;

public class BallotServiceTests : ServiceTestBase
{
    private readonly BallotService _service;
    private readonly Mock<ILogger<BallotService>> _loggerMock;

    private static readonly Guid ElectionGuid = Guid.NewGuid();
    private static readonly Guid LocationGuid = Guid.NewGuid();
    private static readonly Guid BallotGuid = Guid.NewGuid();

    public BallotServiceTests()
    {
        _loggerMock = new Mock<ILogger<BallotService>>();
        _service = new BallotService(Context, _loggerMock.Object);
        SeedElectionGraph();
    }

    private void SeedElectionGraph(BallotStatus status = BallotStatus.Review)
    {
        var election = new Election
        {
            RowId = 1,
            ElectionGuid = ElectionGuid,
            Name = "Test Election",
            NumberToElect = 3,
            ElectionType = "Loc",
            RowVersion = new byte[8]
        };
        var location = new Location
        {
            RowId = 1,
            LocationGuid = LocationGuid,
            ElectionGuid = ElectionGuid,
            Name = "Test Location"
        };
        var ballot = new Ballot
        {
            RowId = 1,
            BallotGuid = BallotGuid,
            LocationGuid = LocationGuid,
            StatusCode = status,
            ComputerCode = "A",
            BallotNumAtComputer = 1,
            RowVersion = new byte[8]
        };

        Context.Elections.Add(election);
        Context.Locations.Add(location);
        Context.Ballots.Add(ballot);
        Context.SaveChanges();
    }

    [Fact]
    public async Task UpdateBallotAsync_ReviewBallotWithoutClearNeedsReview_KeepsReview()
    {
        var result = await _service.UpdateBallotAsync(BallotGuid, new UpdateBallotDto
        {
            StatusCode = BallotStatus.Ok,
            Teller1 = "Alice"
        });

        Assert.NotNull(result);
        Assert.Equal(BallotStatus.Review, result.StatusCode);
        Assert.Equal("Alice", result.Teller1);
        Assert.Equal(BallotStatus.Review, Context.Ballots.Single().StatusCode);
    }

    [Fact]
    public async Task UpdateBallotAsync_ClearNeedsReview_ReEvaluatesStatus()
    {
        var result = await _service.UpdateBallotAsync(BallotGuid, new UpdateBallotDto
        {
            StatusCode = BallotStatus.Review,
            ClearNeedsReview = true
        });

        Assert.NotNull(result);
        Assert.Equal(BallotStatus.Empty, result.StatusCode);
        Assert.Equal(BallotStatus.Empty, Context.Ballots.Single().StatusCode);
    }

    [Fact]
    public async Task UpdateBallotAsync_ClearNeedsReview_OnNonReviewBallot_Throws()
    {
        Context.Ballots.Single().StatusCode = BallotStatus.Ok;
        await Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _service.UpdateBallotAsync(BallotGuid, new UpdateBallotDto
            {
                StatusCode = BallotStatus.Review,
                ClearNeedsReview = true
            }));
    }

    [Fact]
    public async Task UpdateBallotAsync_MarkNeedsReview_SetsReview()
    {
        Context.Ballots.Single().StatusCode = BallotStatus.Empty;
        await Context.SaveChangesAsync();

        var result = await _service.UpdateBallotAsync(BallotGuid, new UpdateBallotDto
        {
            StatusCode = BallotStatus.Review
        });

        Assert.NotNull(result);
        Assert.Equal(BallotStatus.Review, result.StatusCode);
    }
}