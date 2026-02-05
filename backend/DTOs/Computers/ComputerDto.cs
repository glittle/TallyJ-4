namespace TallyJ4.DTOs.Computers;

/// <summary>
/// Data transfer object representing a registered computer.
/// </summary>
public class ComputerDto
{
    /// <summary>
    /// The unique identifier for the computer.
    /// </summary>
    public Guid ComputerGuid { get; set; }
    
    /// <summary>
    /// The unique identifier of the election this computer is registered for.
    /// </summary>
    public Guid ElectionGuid { get; set; }
    
    /// <summary>
    /// The unique identifier of the location where this computer is located.
    /// </summary>
    public Guid LocationGuid { get; set; }
    
    /// <summary>
    /// The code assigned to this computer.
    /// </summary>
    public string ComputerCode { get; set; } = null!;
    
    /// <summary>
    /// Information about the browser being used on this computer.
    /// </summary>
    public string? BrowserInfo { get; set; }
    
    /// <summary>
    /// The IP address of this computer.
    /// </summary>
    public string? IpAddress { get; set; }
    
    /// <summary>
    /// The timestamp of the last activity from this computer.
    /// </summary>
    public DateTime? LastActivity { get; set; }
    
    /// <summary>
    /// The timestamp when this computer was registered.
    /// </summary>
    public DateTime? RegisteredAt { get; set; }
    
    /// <summary>
    /// Indicates whether this computer is currently active.
    /// </summary>
    public bool? IsActive { get; set; }
}
