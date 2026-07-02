namespace Backend.DTOs.OnlineVoting;

/// <summary>
/// A name added to the voter's personal pool (list + random selection mode).
/// </summary>
public class OnlinePoolEntryDto
{
    public string FullName { get; set; } = null!;

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? OtherInfo { get; set; }
}