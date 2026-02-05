namespace TallyJ4.DTOs.Computers;

/// <summary>
/// Data transfer object for registering a new computer.
/// </summary>
public class RegisterComputerDto
{
    /// <summary>
    /// The unique identifier of the election to register the computer for.
    /// </summary>
    public Guid ElectionGuid { get; set; }
    
    /// <summary>
    /// The unique identifier of the location where the computer is located.
    /// </summary>
    public Guid LocationGuid { get; set; }
    
    /// <summary>
    /// The code to assign to this computer.
    /// </summary>
    public string? ComputerCode { get; set; }
    
    /// <summary>
    /// Information about the browser being used on this computer.
    /// </summary>
    public string? BrowserInfo { get; set; }
    
    /// <summary>
    /// The IP address of this computer.
    /// </summary>
    public string? IpAddress { get; set; }
}
