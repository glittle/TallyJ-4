namespace Backend.DTOs.Tellers;

/// <summary>
/// Data transfer object for updating an existing teller.
/// </summary>
public class UpdateTellerDto
{
    /// <summary>
    /// The name of the teller.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The code of the computer the teller is using.
    /// </summary>
    public string? UsingComputerCode { get; set; }

    /// <summary>
    /// Indicates whether this teller is the head teller.
    /// </summary>
    public bool IsHeadTeller { get; set; }
}



