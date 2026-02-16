namespace Backend.DTOs.SuperAdmin;

/// <summary>
/// Data transfer object for super admin status check response.
/// </summary>
public class SuperAdminCheckDto
{
    /// <summary>
    /// Indicates whether the authenticated user has super admin privileges.
    /// </summary>
    public bool IsSuperAdmin { get; set; }
}



