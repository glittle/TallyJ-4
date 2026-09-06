namespace Backend.DTOs.Locations;

/// <summary>
/// Data transfer object for updating an existing location.
/// </summary>
public class UpdateLocationDto
{
    /// <summary>
    /// The name of the location. Omitted for the reserved Online location.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Contact information for the location.
    /// </summary>
    public string? ContactInfo { get; set; }

    /// <summary>
    /// Longitude coordinate of the location.
    /// </summary>
    public string? Longitude { get; set; }

    /// <summary>
    /// Latitude coordinate of the location.
    /// </summary>
    public string? Latitude { get; set; }

    /// <summary>
    /// The sort order for displaying locations.
    /// </summary>
    public int? SortOrder { get; set; }
}



