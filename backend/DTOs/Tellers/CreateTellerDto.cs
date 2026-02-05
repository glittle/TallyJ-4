namespace TallyJ4.DTOs.Tellers;

/// <summary>
/// Data transfer object for creating a new teller.
/// </summary>
public class CreateTellerDto
{
    /// <summary>
    /// The unique identifier of the election to create the teller for.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the teller.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// The code of the computer the teller will be using.
    /// </summary>
    public string? UsingComputerCode { get; set; }

    /// <summary>
    /// Indicates whether this teller is the head teller.
    /// </summary>
    public bool IsHeadTeller { get; set; }
}
