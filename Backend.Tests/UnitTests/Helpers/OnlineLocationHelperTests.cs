using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class OnlineLocationHelperTests : ServiceTestBase
{
    private static readonly Guid ElectionGuid = Guid.NewGuid();

    public OnlineLocationHelperTests()
    {
        Context.Elections.Add(new Election
        {
            ElectionGuid = ElectionGuid,
            Name = "Test",
            NumberToElect = 3,
            ElectionType = "Loc",
            RowVersion = new byte[8]
        });
        Context.SaveChanges();
    }

    [Fact]
    public async Task EnsureExistsAsync_CreatesTypedLocation_Once()
    {
        var first = await OnlineLocationHelper.EnsureExistsAsync(Context, ElectionGuid);
        var second = await OnlineLocationHelper.EnsureExistsAsync(Context, ElectionGuid);

        Assert.Equal(LocationType.Online, first.LocationTypeEnum);
        Assert.Equal(first.LocationGuid, second.LocationGuid);
        Assert.Equal(1, Context.Locations.Count(l => l.ElectionGuid == ElectionGuid));
    }

    [Fact]
    public async Task RemoveIfUnusedAsync_DeletesWhenEmpty()
    {
        await OnlineLocationHelper.EnsureExistsAsync(Context, ElectionGuid);

        await OnlineLocationHelper.RemoveIfUnusedAsync(Context, ElectionGuid);

        Assert.Empty(Context.Locations.Where(l => l.ElectionGuid == ElectionGuid));
    }

    [Fact]
    public async Task RemoveIfUnusedAsync_KeepsLocationWhenItHasBallots()
    {
        var location = await OnlineLocationHelper.EnsureExistsAsync(Context, ElectionGuid);
        Context.Ballots.Add(new Ballot
        {
            BallotGuid = Guid.NewGuid(),
            LocationGuid = location.LocationGuid,
            ComputerCode = ComputerCodeHelper.Online,
            BallotNumAtComputer = 1,
            StatusCode = BallotStatus.Ok,
            RowVersion = new byte[8]
        });
        await Context.SaveChangesAsync();

        await OnlineLocationHelper.RemoveIfUnusedAsync(Context, ElectionGuid);

        Assert.Single(Context.Locations.Where(l => l.ElectionGuid == ElectionGuid));
    }

    [Fact]
    public async Task RemoveIfUnusedAsync_KeepsLocationWhenItHasComputers()
    {
        var location = await OnlineLocationHelper.EnsureExistsAsync(Context, ElectionGuid);
        Context.Computers.Add(new Computer
        {
            ElectionGuid = ElectionGuid,
            LocationGuid = location.LocationGuid,
            ComputerGuid = Guid.NewGuid(),
            ComputerCode = "A1"
        });
        await Context.SaveChangesAsync();

        await OnlineLocationHelper.RemoveIfUnusedAsync(Context, ElectionGuid);
        await OnlineLocationHelper.SyncAsync(Context, ElectionGuid, useOnlineVoting: false);

        Assert.Single(Context.Locations.Where(l => l.ElectionGuid == ElectionGuid));
        Assert.Empty(Context.Ballots.Where(b => b.LocationGuid == location.LocationGuid));
    }

    [Fact]
    public async Task SyncAsync_Enabled_Creates_Disabled_RemovesWhenEmpty()
    {
        await OnlineLocationHelper.SyncAsync(Context, ElectionGuid, useOnlineVoting: true);
        Assert.Single(Context.Locations);

        await OnlineLocationHelper.SyncAsync(Context, ElectionGuid, useOnlineVoting: false);
        Assert.Empty(Context.Locations);
    }
}
