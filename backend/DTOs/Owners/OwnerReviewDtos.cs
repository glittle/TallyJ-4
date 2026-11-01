namespace Backend.DTOs.Owners;

/// <summary>
/// Restriction state shown to the signed-in owner.
/// </summary>
public class OwnerRestrictionDto
{
    /// <summary>
    /// <see cref="Entities.OwnerRestrictionStatus"/>, or empty when this account has no row.
    /// </summary>
    public string Status { get; set; } = "";

    /// <summary>
    /// Hold or rejection note from the super admin.
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// When the owner last asked for a review.
    /// </summary>
    public DateTimeOffset? ReviewRequestedAt { get; set; }

    /// <summary>
    /// Earliest time another request email may be sent. Null when a request is allowed now.
    /// </summary>
    public DateTimeOffset? NextRequestAt { get; set; }

    /// <summary>
    /// True when the request button should submit.
    /// </summary>
    public bool CanRequestReview { get; set; }

    /// <summary>
    /// True when voter codes and online voting are blocked for this account.
    /// </summary>
    public bool BlocksSends { get; set; }
}

/// <summary>
/// One owner on the super-admin review list.
/// </summary>
public class OwnerReviewListItemDto
{
    public Guid UserId { get; set; }

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public string Status { get; set; } = "";

    public string? AuthMethod { get; set; }

    public DateTimeOffset? ReviewRequestedAt { get; set; }

    public DateTimeOffset? RejectedAt { get; set; }

    public int ElectionCount { get; set; }
}

/// <summary>
/// Owner plus every election a super admin needs in order to approve, hold, or reject.
/// </summary>
public class OwnerReviewDetailDto
{
    public Guid UserId { get; set; }

    public string? Email { get; set; }

    public string? DisplayName { get; set; }

    public string? AuthMethod { get; set; }

    public DateTimeOffset? SignedUpAt { get; set; }

    public string Status { get; set; } = "";

    public string? Note { get; set; }

    public DateTimeOffset? ReviewRequestedAt { get; set; }

    public DateTimeOffset? ReviewedAt { get; set; }

    public string? ReviewedByUserId { get; set; }

    public DateTimeOffset? RejectedAt { get; set; }

    /// <summary>
    /// When the cleanup job will delete this owner's elections. Null until rejected.
    /// </summary>
    public DateTimeOffset? DeleteElectionsAt { get; set; }

    public List<OwnerReviewElectionDto> Elections { get; set; } = new();
}

/// <summary>
/// One election on the owner review.
/// </summary>
public class OwnerReviewElectionDto
{
    public Guid ElectionGuid { get; set; }

    public string Name { get; set; } = "";

    public string? ElectionType { get; set; }

    public DateTimeOffset? DateOfElection { get; set; }

    public DateTimeOffset? OnlineWhenOpen { get; set; }

    public DateTimeOffset? OnlineWhenClose { get; set; }

    public int PeopleCount { get; set; }

    public List<OwnerReviewCountDto> EmailDomains { get; set; } = new();

    public List<OwnerReviewCountDto> PhoneCountries { get; set; } = new();

    public int FlaggedContactCount { get; set; }

    public List<string> FlaggedSample { get; set; } = new();

    public List<string> PeopleSample { get; set; } = new();
}

/// <summary>
/// A counted label, such as one email domain or one phone country.
/// </summary>
public class OwnerReviewCountDto
{
    public string Key { get; set; } = "";

    public int Count { get; set; }
}

/// <summary>
/// Optional note for Hold or Reject.
/// </summary>
public class OwnerReviewNoteDto
{
    /// <summary>
    /// Shown to the owner in the outcome email and on their banner. At most 2000 characters.
    /// </summary>
    public string? Note { get; set; }
}
