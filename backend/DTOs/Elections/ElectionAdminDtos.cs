namespace Backend.DTOs.Elections;

/// <summary>
/// One owner or co-admin of an election. Co-admins are stored as role <c>Admin</c>,
/// the same role <c>CreateElection</c> writes for the creator.
/// </summary>
public class ElectionAdminDto
{
    public Guid UserId { get; set; }

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    /// <summary>
    /// <c>Owner</c> or <c>Admin</c>.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// False for an <c>Owner</c> row and for the last remaining owner or admin.
    /// </summary>
    public bool CanRemove { get; set; }
}

/// <summary>
/// Email of an existing officer account to add as a co-admin.
/// </summary>
public class AddElectionAdminDto
{
    public string Email { get; set; } = "";
}

/// <summary>
/// Phrase keys for election admin add and remove.
/// </summary>
public static class ElectionAdminMessages
{
    public const string NoAccount = "elections.admins.noAccount";

    public const string EmailNotConfirmed = "elections.admins.emailNotConfirmed";

    public const string AlreadyAdmin = "elections.admins.alreadyAdmin";

    public const string Added = "elections.admins.added";

    public const string OwnerCannotBeRemoved = "elections.admins.ownerCannotBeRemoved";

    public const string LastOwner = "elections.admins.lastOwner";

    public const string NotAnAdmin = "elections.admins.notAnAdmin";

    public const string Removed = "elections.admins.removed";

    public const string Forbidden = "error.forbidden";
}

/// <summary>
/// Result of listing an election's owners and co-admins.
/// </summary>
public class ElectionAdminsListResult
{
    public bool IsNotFound { get; init; }

    public bool IsForbidden { get; init; }

    public IReadOnlyList<ElectionAdminDto> Admins { get; init; } = [];

    public static ElectionAdminsListResult NotFound() => new() { IsNotFound = true };

    public static ElectionAdminsListResult Forbidden() => new() { IsForbidden = true };

    public static ElectionAdminsListResult Ok(IReadOnlyList<ElectionAdminDto> admins) =>
        new() { Admins = admins };
}

/// <summary>
/// Result of adding a co-admin. A person who is already an owner or admin is success.
/// </summary>
public class AddElectionAdminResult
{
    public bool IsNotFound { get; init; }

    public bool IsForbidden { get; init; }

    public bool IsNoAccount { get; init; }

    public bool IsEmailNotConfirmed { get; init; }

    public bool AlreadyAdmin { get; init; }

    public ElectionAdminDto? Admin { get; init; }

    public static AddElectionAdminResult NotFound() => new() { IsNotFound = true };

    public static AddElectionAdminResult Forbidden() => new() { IsForbidden = true };

    public static AddElectionAdminResult NoAccount() => new() { IsNoAccount = true };

    public static AddElectionAdminResult EmailNotConfirmed() => new() { IsEmailNotConfirmed = true };

    public static AddElectionAdminResult Ok(ElectionAdminDto admin, bool alreadyAdmin) =>
        new() { Admin = admin, AlreadyAdmin = alreadyAdmin };
}

/// <summary>
/// Result of removing a co-admin.
/// </summary>
public class RemoveElectionAdminResult
{
    public bool IsNotFound { get; init; }

    public bool IsForbidden { get; init; }

    public bool IsNotAnAdmin { get; init; }

    public bool IsOwner { get; init; }

    public bool IsLastOwner { get; init; }

    public static RemoveElectionAdminResult NotFound() => new() { IsNotFound = true };

    public static RemoveElectionAdminResult Forbidden() => new() { IsForbidden = true };

    public static RemoveElectionAdminResult NotAnAdmin() => new() { IsNotAnAdmin = true };

    public static RemoveElectionAdminResult Owner() => new() { IsOwner = true };

    public static RemoveElectionAdminResult LastOwner() => new() { IsLastOwner = true };

    public static RemoveElectionAdminResult Ok() => new();
}
