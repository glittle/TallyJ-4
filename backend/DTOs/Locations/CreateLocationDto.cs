namespace Backend.DTOs.Locations;

/// <summary>
/// Data transfer object for creating a new location.
/// </summary>
public class CreateLocationDto
{
    /// <summary>
    /// The unique identifier of the election this location belongs to.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the location.
    /// </summary>
    public string Name { get; set; } = null!;

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



