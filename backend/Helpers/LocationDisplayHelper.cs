using Backend.Entities;
using Backend.Enumerations;

namespace Backend.Helpers;

/// <summary>
/// Display name for a location. Reserved Online / Imported rows are identified by
/// <see cref="LocationType"/>, never by the stored name.
/// </summary>
public static class LocationDisplayHelper
{
    public const string TypeOnlineKey = "locations.typeOnline";
    public const string TypeImportedKey = "locations.typeImported";

    public static bool IsOnlineLocationType(string? locationTypeCode) =>
        string.Equals(locationTypeCode, nameof(LocationType.Online), StringComparison.OrdinalIgnoreCase);

    public static bool IsImportedLocationType(string? locationTypeCode) =>
        string.Equals(locationTypeCode, nameof(LocationType.Imported), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Online and Imported are system-managed rows: i18n label, sort-only edit, no delete.
    /// </summary>
    public static bool IsReservedLocationType(string? locationTypeCode) =>
        IsOnlineLocationType(locationTypeCode) || IsImportedLocationType(locationTypeCode);

    public static bool IsOnlineLocation(Location location) =>
        IsOnlineLocationType(location.LocationTypeCode);

    public static bool IsImportedLocation(Location location) =>
        IsImportedLocationType(location.LocationTypeCode);

    public static bool IsReservedLocation(Location location) =>
        IsReservedLocationType(location.LocationTypeCode);

    public static string FormatName(
        string? storedName,
        string? locationTypeCode,
        Func<string, string> localize)
    {
        if (IsOnlineLocationType(locationTypeCode))
        {
            return localize(TypeOnlineKey);
        }

        if (IsImportedLocationType(locationTypeCode))
        {
            return localize(TypeImportedKey);
        }

        return storedName?.Trim() ?? string.Empty;
    }

    public static string FormatName(Location location, Func<string, string> localize) =>
        FormatName(location.Name, location.LocationTypeCode, localize);
}
