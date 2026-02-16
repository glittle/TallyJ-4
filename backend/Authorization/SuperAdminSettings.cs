namespace Backend.Authorization;

public class SuperAdminSettings
{
    public const string SectionName = "SuperAdmin";

    public string[] Emails { get; set; } = Array.Empty<string>();
}



