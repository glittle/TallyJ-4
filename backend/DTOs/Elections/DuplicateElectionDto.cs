namespace Backend.DTOs.Elections;

/// <summary>
/// Optional name for a duplicated test election. When omitted or blank,
/// the service uses "Copy of {source name}".
/// </summary>
public class DuplicateElectionDto
{
    /// <summary>
    /// Name for the new election. Blank or omitted uses the "Copy of …" default.
    /// </summary>
    public string? Name { get; set; }
}
