namespace TallyJ4.DTOs.Locations;

/// <summary>
/// Data transfer object representing a voting location.
/// </summary>
public class LocationDto
{
    /// <summary>
    /// The unique identifier for the location.
    /// </summary>
    public Guid LocationGuid { get; set; }

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
    /// The current tally status at this location.
    /// </summary>
    public string? TallyStatus { get; set; }

    /// <summary>
    /// The sort order for displaying locations.
    /// </summary>
    public int? SortOrder { get; set; }

    /// <summary>
    /// The number of ballots collected at this location.
    /// </summary>
    public int? BallotsCollected { get; set; }
}
