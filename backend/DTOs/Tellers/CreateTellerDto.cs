namespace Backend.DTOs.Tellers;

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
}