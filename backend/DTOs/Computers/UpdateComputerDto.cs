namespace Backend.DTOs.Computers;

/// <summary>
/// Data transfer object for updating an existing computer.
/// </summary>
public class UpdateComputerDto
{
    /// <summary>
    /// Information about the browser being used on this computer.
    /// </summary>
    public string? BrowserInfo { get; set; }

    /// <summary>
    /// The IP address of this computer.
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Indicates whether this computer is currently active.
    /// </summary>
    public bool? IsActive { get; set; }
}



