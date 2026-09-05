using Microsoft.Extensions.Logging;
using Moq;
using Backend.DTOs.Locations;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Services;

namespace Backend.Tests.UnitTests.Services;

public class LocationServiceTests : ServiceTestBase
{
    private readonly LocationService _service;
    private static readonly Guid ElectionGuid = Guid.NewGuid();

    public LocationServiceTests()
    {
        _service = new LocationService(Context, new Mock<ILogger<LocationService>>().Object);
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
    public async Task UpdateLocationAsync_Online_IgnoresNameContactAndCoordinates_AppliesSortOrder()
    {
        var locationGuid = Guid.NewGuid();
        Context.Locations.Add(new Location
        {
            LocationGuid = locationGuid,
            ElectionGuid = ElectionGuid,
            Name = "Online",
            ContactInfo = null,
            Long = null,
            Lat = null,
            SortOrder = 999,
            LocationTypeCode = nameof(LocationType.Online),
            LocationTallyStatus = LocationTallyStatus.NotStarted,
            BallotsCollected = 0
        });
        await Context.SaveChangesAsync();

        var updated = await _service.UpdateLocationAsync(locationGuid, new UpdateLocationDto
        {
            Name = "Renamed in Persian",
            ContactInfo = "do not store",
            Longitude = "-122.4",
            Latitude = "37.7",
            SortOrder = 12
        });

        Assert.NotNull(updated);
        Assert.Equal("Online", updated.Name);
        Assert.Null(updated.ContactInfo);
        Assert.Null(updated.Longitude);
        Assert.Null(updated.Latitude);
        Assert.Equal(12, updated.SortOrder);

        var stored = Context.Locations.Single(l => l.LocationGuid == locationGuid);
        Assert.Equal("Online", stored.Name);
        Assert.Null(stored.ContactInfo);
        Assert.Null(stored.Long);
        Assert.Null(stored.Lat);
        Assert.Equal(12, stored.SortOrder);
        Assert.Equal(nameof(LocationType.Online), stored.LocationTypeCode);
    }

    [Fact]
    public async Task UpdateLocationAsync_Paper_AppliesNameAndContact()
    {
        var locationGuid = Guid.NewGuid();
        Context.Locations.Add(new Location
        {
            LocationGuid = locationGuid,
            ElectionGuid = ElectionGuid,
            Name = "Hall A",
            SortOrder = 1,
            LocationTypeCode = nameof(LocationType.Manual),
            LocationTallyStatus = LocationTallyStatus.NotStarted,
            BallotsCollected = 0
        });
        await Context.SaveChangesAsync();

        var updated = await _service.UpdateLocationAsync(locationGuid, new UpdateLocationDto
        {
            Name = "Main Hall",
            ContactInfo = "555-0100",
            SortOrder = 2
        });

        Assert.NotNull(updated);
        Assert.Equal("Main Hall", updated.Name);
        Assert.Equal("555-0100", updated.ContactInfo);
        Assert.Equal(2, updated.SortOrder);
    }

    [Fact]
    public async Task UpdateLocationAsync_Paper_WithoutName_DoesNotClearStoredName()
    {
        var locationGuid = Guid.NewGuid();
        Context.Locations.Add(new Location
        {
            LocationGuid = locationGuid,
            ElectionGuid = ElectionGuid,
            Name = "Hall A",
            ContactInfo = "keep me",
            SortOrder = 1,
            LocationTypeCode = nameof(LocationType.Manual),
            LocationTallyStatus = LocationTallyStatus.NotStarted,
            BallotsCollected = 0
        });
        await Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateLocationAsync(locationGuid, new UpdateLocationDto
            {
                Name = null,
                SortOrder = 5
            }));

        Assert.Contains("name is required", ex.Message, StringComparison.OrdinalIgnoreCase);

        var stored = Context.Locations.Single(l => l.LocationGuid == locationGuid);
        Assert.Equal("Hall A", stored.Name);
        Assert.Equal("keep me", stored.ContactInfo);
        Assert.Equal(1, stored.SortOrder);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task UpdateLocationAsync_Paper_WhitespaceName_DoesNotClearStoredName(string emptyName)
    {
        var locationGuid = Guid.NewGuid();
        Context.Locations.Add(new Location
        {
            LocationGuid = locationGuid,
            ElectionGuid = ElectionGuid,
            Name = "Hall A",
            SortOrder = 1,
            LocationTypeCode = nameof(LocationType.Manual),
            LocationTallyStatus = LocationTallyStatus.NotStarted,
            BallotsCollected = 0
        });
        await Context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.UpdateLocationAsync(locationGuid, new UpdateLocationDto
            {
                Name = emptyName,
                SortOrder = 5
            }));

        var stored = Context.Locations.Single(l => l.LocationGuid == locationGuid);
        Assert.Equal("Hall A", stored.Name);
        Assert.Equal(1, stored.SortOrder);
    }

    [Fact]
    public async Task CreateLocationAsync_DoesNotCreateOnlineType_EvenWhenNamedOnline()
    {
        var created = await _service.CreateLocationAsync(new CreateLocationDto
        {
            ElectionGuid = ElectionGuid,
            Name = "Online",
            SortOrder = 1
        });

        Assert.Null(created.LocationType);
        var stored = Context.Locations.Single(l => l.LocationGuid == created.LocationGuid);
        Assert.Null(stored.LocationTypeCode);
        Assert.NotEqual(LocationType.Online, stored.LocationTypeEnum);
    }

    [Fact]
    public async Task DeleteLocationAsync_Online_Throws()
    {
        var locationGuid = Guid.NewGuid();
        Context.Locations.Add(new Location
        {
            LocationGuid = locationGuid,
            ElectionGuid = ElectionGuid,
            Name = "Online",
            LocationTypeCode = nameof(LocationType.Online),
            LocationTallyStatus = LocationTallyStatus.NotStarted,
            BallotsCollected = 0
        });
        await Context.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.DeleteLocationAsync(locationGuid));

        Assert.Contains("cannot be deleted", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Single(Context.Locations.Where(l => l.LocationGuid == locationGuid));
    }
}
