namespace Backend.DTOs.SuperAdmin;

public class SuperAdminUserDto
{
    public string Id { get; set; } = null!;
    public string? Email { get; set; }
    public string? DisplayName { get; set; }
    public string AuthMethod { get; set; } = "Local";
    public bool EmailConfirmed { get; set; }
    public string? PendingEmail { get; set; }
    public DateTimeOffset? LockoutEnd { get; set; }
}

public class SuperAdminUserDetailDto : SuperAdminUserDto
{
    /// <summary>
    /// Email change history newest first (chain of prior addresses).
    /// </summary>
    public List<SuperAdminEmailChangeEntryDto> EmailHistory { get; set; } = new();
}

public class SuperAdminEmailChangeEntryDto
{
    public string OldEmail { get; set; } = null!;
    public string NewEmail { get; set; } = null!;
    public DateTimeOffset ChangedAt { get; set; }
    public string Source { get; set; } = null!;
    public string? ChangedByUserId { get; set; }
}

public class SuperAdminUserFilterDto
{
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}

public class SuperAdminUpdateUserDto
{
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
}
