using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;

namespace Backend.Tests.UnitTests.Helpers;

public class LocationDisplayHelperTests
{
    [Fact]
    public void FormatName_UsesLocalizer_ForOnlineTypeRegardlessOfStoredName()
    {
        var location = new Location
        {
            Name = "Hall A",
            LocationTypeCode = nameof(LocationType.Online)
        };

        var label = LocationDisplayHelper.FormatName(location, _ => "Online");

        Assert.Equal("Online", label);
    }

    [Fact]
    public void FormatName_UsesStoredName_ForPaperLocation()
    {
        var location = new Location
        {
            Name = "Main Hall",
            LocationTypeCode = nameof(LocationType.Manual)
        };

        var label = LocationDisplayHelper.FormatName(location, _ => "Online");

        Assert.Equal("Main Hall", label);
    }

    [Fact]
    public void IsOnlineLocation_UsesTypeNotName()
    {
        var namedOnline = new Location
        {
            Name = "Online",
            LocationTypeCode = nameof(LocationType.Manual)
        };
        var typedOnline = new Location
        {
            Name = "Hall A",
            LocationTypeCode = nameof(LocationType.Online)
        };

        Assert.False(LocationDisplayHelper.IsOnlineLocation(namedOnline));
        Assert.True(LocationDisplayHelper.IsOnlineLocation(typedOnline));
    }
}
