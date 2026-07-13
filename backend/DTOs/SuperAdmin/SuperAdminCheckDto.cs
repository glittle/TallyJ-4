using System.Text.Json.Serialization;

namespace Backend.DTOs.SuperAdmin;

/// <summary>
/// Data transfer object for super admin status check response.
/// <see cref="IsSuperAdmin"/> is omitted when false.
/// </summary>
public class SuperAdminCheckDto
{
    /// <summary>
    /// Present only when the authenticated user has super admin privileges.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool IsSuperAdmin { get; set; }
}



