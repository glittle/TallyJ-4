using Backend.Entities;
using Backend.Enumerations;

namespace Backend.Helpers;

/// <summary>
/// Display name for a location. The reserved Online row is identified by
/// <see cref="LocationType.Online"/>, never by the stored name.
/// </summary>
public static class LocationDisplayHelper
{
    public const string TypeOnlineKey = "locations.typeOnline";

    public static bool IsOnlineLocationType(string? locationTypeCode) =>
        string.Equals(locationTypeCode, nameof(LocationType.Online), StringComparison.OrdinalIgnoreCase);

    public static bool IsOnlineLocation(Location location) =>
        IsOnlineLocationType(location.LocationTypeCode);

    public static string FormatName(
        string? storedName,
        string? locationTypeCode,
        Func<string, string> localize)
    {
        if (IsOnlineLocationType(locationTypeCode))
        {
            return localize(TypeOnlineKey);
        }

        return storedName?.Trim() ?? string.Empty;
    }

    public static string FormatName(Location location, Func<string, string> localize) =>
        FormatName(location.Name, location.LocationTypeCode, localize);
}
