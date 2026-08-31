namespace Backend.DTOs.SignalR;

/// <summary>
/// Election-scoped teller name list change for MainHub <c>tellersChanged</c>.
/// </summary>
public class TellerUpdateDto
{
    /// <summary>
    /// Election whose teller name list changed.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// Teller row id.
    /// </summary>
    public int RowId { get; set; }

    /// <summary>
    /// Teller display name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The action performed: <c>added</c>, <c>updated</c>, or <c>deleted</c>.
    /// </summary>
    public string Action { get; set; } = string.Empty;
}
