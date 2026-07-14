using System.Text.Json.Serialization;

namespace Backend.DTOs.Auth;

/// <summary>
/// Current authenticated user profile returned by <c>GET /api/Auth/me</c>.
/// </summary>
public class CurrentUserDto
{
    public string? Email { get; set; }

    public string? Name { get; set; }

    public string? AuthMethod { get; set; }

    /// <summary>
    /// Included only when <c>true</c>. Omitted for non–super-admin users so the capability
    /// is not advertised in the normal /me response.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsSuperAdmin { get; set; }
}
