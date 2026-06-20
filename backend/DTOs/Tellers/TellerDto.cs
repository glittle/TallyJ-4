namespace Backend.DTOs.Tellers;

/// <summary>
/// Data transfer object representing a teller.
/// </summary>
public class TellerDto
{
    /// <summary>
    /// The unique row identifier of the teller.
    /// </summary>
    public int RowId { get; set; }

    /// <summary>
    /// The unique identifier of the election this teller belongs to.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The name of the teller.
    /// </summary>
    public string Name { get; set; } = null!;
}