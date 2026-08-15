using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Xunit;

namespace Backend.Tests.UnitTests.Helpers;

public class SpecialBallotNumberingTests : ServiceTestBase
{
    [Fact]
    public async Task RepairOnlineAndGetNextAsync_RenumbersWwZerosAndReturnsNext()
    {
        var locationGuid = Guid.NewGuid();
        Context.Locations.Add(new Location
        {
            LocationGuid = locationGuid,
            ElectionGuid = Guid.NewGuid(),
            Name = "Online"
        });
        Context.Ballots.AddRange(
            new Ballot
            {
                LocationGuid = locationGuid,
                BallotGuid = Guid.NewGuid(),
                ComputerCode = "WW",
                BallotNumAtComputer = 0,
                StatusCode = BallotStatus.Ok,
                DateCreated = DateTimeOffset.UtcNow.AddMinutes(-2),
                RowVersion = new byte[8]
            },
            new Ballot
            {
                LocationGuid = locationGuid,
                BallotGuid = Guid.NewGuid(),
                ComputerCode = "WW",
                BallotNumAtComputer = 0,
                StatusCode = BallotStatus.Ok,
                DateCreated = DateTimeOffset.UtcNow.AddMinutes(-1),
                RowVersion = new byte[8]
            });
        await Context.SaveChangesAsync();

        var next = await SpecialBallotNumbering.RepairOnlineAndGetNextAsync(Context, locationGuid);

        var repaired = Context.Ballots.OrderBy(b => b.BallotNumAtComputer).ToList();
        Assert.Equal(3, next);
        Assert.All(repaired, b => Assert.Equal(ComputerCodeHelper.Online, b.ComputerCode));
        Assert.Equal(new[] { 1, 2 }, repaired.Select(b => b.BallotNumAtComputer));
        Assert.Equal("OL1", repaired[0].BallotCode);
        Assert.Equal("OL2", repaired[1].BallotCode);
    }
}
