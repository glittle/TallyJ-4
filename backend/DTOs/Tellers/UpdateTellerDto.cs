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
}