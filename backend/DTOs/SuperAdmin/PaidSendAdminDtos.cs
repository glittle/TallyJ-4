namespace Backend.DTOs.SuperAdmin;

/// <summary>
/// Super-admin view of paid-send approvals, cap hits, and freezes.
/// </summary>
public class PaidSendAdminOverviewDto
{
    /// <summary>
    /// Owners and admins who have an election and are not approved for paid sends.
    /// </summary>
    public List<PendingPaidSendOwnerDto> PendingOwners { get; set; } = new();

    /// <summary>
    /// Elections and owners that are currently at their cap.
    /// </summary>
    public List<PaidSendCapHitDto> CapHits { get; set; } = new();

    /// <summary>
    /// Elections whose login codes are frozen.
    /// </summary>
    public List<FrozenSendElectionDto> FrozenElections { get; set; } = new();

    /// <summary>
    /// Owners whose login codes are frozen.
    /// </summary>
    public List<FrozenSendOwnerDto> FrozenOwners { get; set; } = new();

    /// <summary>
    /// Elections flagged by the voter-list checks. Online voting is suspended.
    /// </summary>
    public List<FlaggedElectionDto> FlaggedElections { get; set; } = new();
}

/// <summary>
/// An owner waiting for paid-send approval.
/// </summary>
public class PendingPaidSendOwnerDto
{
    /// <summary>
    /// Login account id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Account email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Account display name.
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// Elections where this account is owner or admin.
    /// </summary>
    public int ElectionCount { get; set; }
}

/// <summary>
/// A cap that is currently stopping paid sends.
/// </summary>
public class PaidSendCapHitDto
{
    /// <summary>
    /// election or owner-daily.
    /// </summary>
    public string Scope { get; set; } = null!;

    /// <summary>
    /// Election that hit its allowance, when the scope is the election.
    /// </summary>
    public Guid? ElectionGuid { get; set; }

    /// <summary>
    /// Election name.
    /// </summary>
    public string? ElectionName { get; set; }

    /// <summary>
    /// Owner who hit today's cap, when the scope is the owner.
    /// </summary>
    public Guid? OwnerUserId { get; set; }

    /// <summary>
    /// Owner email.
    /// </summary>
    public string? OwnerEmail { get; set; }

    /// <summary>
    /// Sends already counted.
    /// </summary>
    public int Used { get; set; }

    /// <summary>
    /// Cap that stopped sending.
    /// </summary>
    public int Cap { get; set; }
}

/// <summary>
/// An election with login codes frozen.
/// </summary>
public class FrozenSendElectionDto
{
    /// <summary>
    /// Election id.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// Election name.
    /// </summary>
    public string Name { get; set; } = null!;
}

/// <summary>
/// An owner with login codes frozen.
/// </summary>
public class FrozenSendOwnerDto
{
    /// <summary>
    /// Login account id.
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Account email.
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Account display name.
    /// </summary>
    public string? DisplayName { get; set; }
}

/// <summary>
/// An election flagged by voter-list checks.
/// </summary>
public class FlaggedElectionDto
{
    /// <summary>
    /// Election id.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// Election name.
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// When the election was flagged.
    /// </summary>
    public DateTimeOffset? FlaggedAt { get; set; }

    /// <summary>
    /// Active flagged rows the super admin can review.
    /// </summary>
    public List<FlaggedVoterContactDto> Rows { get; set; } = new();
}

/// <summary>
/// One flagged phone or email. The value is masked.
/// </summary>
public class FlaggedVoterContactDto
{
    /// <summary>
    /// Import row number when the value came from a file. Null for a manual edit.
    /// </summary>
    public int? RowNumber { get; set; }

    /// <summary>
    /// Masked phone or email.
    /// </summary>
    public string MaskedValue { get; set; } = null!;

    /// <summary>
    /// invalid-phone, unexpected-country, consecutive-run, disposable-domain, or no-mx.
    /// </summary>
    public string Reason { get; set; } = null!;
}

/// <summary>
/// New election paid-send allowance. Must be higher than the allowance in effect.
/// </summary>
public class RaiseElectionAllowanceDto
{
    /// <summary>
    /// New allowance.
    /// </summary>
    public int Allowance { get; set; }
}

/// <summary>
/// New owner daily paid-send cap. Must be higher than the cap in effect.
/// </summary>
public class RaiseOwnerDailyCapDto
{
    /// <summary>
    /// New daily cap.
    /// </summary>
    public int DailyCap { get; set; }
}
