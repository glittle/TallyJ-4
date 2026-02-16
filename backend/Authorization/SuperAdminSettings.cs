namespace Backend.Authorization;

/// <summary>
/// Configuration settings for super admin functionality.
/// </summary>
public class SuperAdminSettings
{
    /// <summary>
    /// The configuration section name for super admin settings.
    /// </summary>
    public const string SectionName = "SuperAdmin";

    /// <summary>
    /// Array of email addresses that have super admin privileges.
    /// </summary>
    public string[] Emails { get; set; } = Array.Empty<string>();
}



