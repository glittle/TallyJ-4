namespace Backend.DTOs.Public;

/// <summary>
/// Data transfer object representing information for the public home page.
/// </summary>
public class PublicHomeDto
{
    /// <summary>
    /// The name of the application.
    /// </summary>
    public string ApplicationName { get; set; } = "TallyJ 4";

    /// <summary>
    /// The version of the application.
    /// </summary>
    public string Version { get; set; } = "4.0.0";

    /// <summary>
    /// A description of the application.
    /// </summary>
    public string Description { get; set; } = "Election management and online voting system";

    /// <summary>
    /// The number of elections available for public access.
    /// </summary>
    public int AvailableElectionsCount { get; set; }

    /// <summary>
    /// The current server time.
    /// </summary>
    public DateTimeOffset ServerTime { get; set; }
}



