using Backend;
using Backend.Authorization;
using Backend.Configuration;
using Backend.Context;
using Backend.DTOs.Owners;
using Backend.DTOs.Security;
using Backend.Entities;
using Backend.Identity;
using Backend.Services.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using PhoneNumbers;

namespace Backend.Services;

/// <summary>
/// Per-owner restricted mode, review requests, and super-admin decisions.
/// </summary>
public class OwnerReviewService : IOwnerReviewService
{
    public const string ThrottledKey = "elections.ownerRestricted.throttled";
    public const string NoRecipientsKey = "elections.ownerRestricted.noRecipients";
    public const string EmailFailedKey = "elections.ownerRestricted.requestFailed";
    public const string AlreadyApprovedKey = "elections.ownerRestricted.alreadyApproved";
    public const string NotFoundKey = "elections.ownerRestricted.notFound";
    public const string NoteTooLongKey = "elections.ownerRestricted.noteTooLong";
    public const string NotRejectedKey = "elections.ownerRestricted.notRejected";
    public const string RejectedKey = "elections.ownerRestricted.rejected";

    private const int NoteMaxLength = 2000;

    private readonly MainDbContext _context;
    private readonly IEmailSender _emailSender;
    private readonly ISecurityAuditService _audit;
    private readonly ICodeSendClock _clock;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly SuperAdminSettings _superAdmin;
    private readonly ILogger<OwnerReviewService> _logger;
    private readonly PhoneNumberUtil _phones = PhoneNumberUtil.GetInstance();

    /// <summary>
    /// Initializes the review service.
    /// </summary>
    public OwnerReviewService(
        MainDbContext context,
        IEmailSender emailSender,
        ISecurityAuditService audit,
        ICodeSendClock clock,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        IOptions<SuperAdminSettings> superAdmin,
        ILogger<OwnerReviewService> logger)
    {
        _context = context;
        _emailSender = emailSender;
        _audit = audit;
        _clock = clock;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _superAdmin = superAdmin.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<OwnerRestrictionDto> GetForUserAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var id))
        {
            return new OwnerRestrictionDto();
        }

        var row = await _context.OwnerSendControls
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == id, cancellationToken);
        return ToDto(row);
    }

    /// <inheritdoc />
    public async Task<OwnerReviewActionResult> RequestReviewAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(userId, out var id))
        {
            return OwnerReviewActionResult.Fail(NotFoundKey);
        }

        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == id.ToString(), cancellationToken);
        if (user == null)
        {
            return OwnerReviewActionResult.Fail(NotFoundKey);
        }

        var row = await _context.OwnerSendControls
            .FirstOrDefaultAsync(item => item.UserId == id, cancellationToken);
        if (row == null)
        {
            row = new OwnerSendControl { UserId = id };
            _context.OwnerSendControls.Add(row);
        }

        if (row.RestrictionStatus == OwnerRestrictionStatus.Approved)
        {
            return OwnerReviewActionResult.Fail(AlreadyApprovedKey);
        }

        if (row.RestrictionStatus == OwnerRestrictionStatus.Rejected)
        {
            return OwnerReviewActionResult.Fail(RejectedKey);
        }

        var now = _clock.UtcNow;
        if (row.ReviewRequestedAt is DateTimeOffset requested
            && requested.AddHours(OwnerRestrictionStatus.ReviewRequestThrottleHours) > now)
        {
            return OwnerReviewActionResult.Fail(
                ThrottledKey,
                requested.AddHours(OwnerRestrictionStatus.ReviewRequestThrottleHours));
        }

        var recipients = SuperAdminRecipients();
        if (recipients.Count == 0)
        {
            return OwnerReviewActionResult.Fail(NoRecipientsKey);
        }

        var detail = await BuildDetailAsync(user, row, cancellationToken);
        try
        {
            await SendReviewRequestAsync(detail, recipients, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Review request email failed for owner {OwnerUserId}", id);
            return OwnerReviewActionResult.Fail(EmailFailedKey);
        }

        row.RestrictionStatus = OwnerRestrictionStatus.ReviewRequested;
        row.PaidSendsApproved = false;
        row.ReviewRequestedAt = now;
        row.StatusChangedAt = now;
        row.ReviewNote = null;
        await _context.SaveChangesAsync(cancellationToken);
        await AuditAsync(
            SecurityEventType.OwnerReviewRequested,
            id.ToString(),
            user.Email,
            "Owner requested a review");

        return OwnerReviewActionResult.Ok(ToDto(row));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<OwnerReviewListItemDto>> ListAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _context.OwnerSendControls.AsNoTracking()
            .Where(row =>
                row.RestrictionStatus == OwnerRestrictionStatus.Restricted
                || row.RestrictionStatus == OwnerRestrictionStatus.ReviewRequested
                || row.RestrictionStatus == OwnerRestrictionStatus.Rejected);
        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(row => row.RestrictionStatus == status);
        }

        var rows = await query.ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return Array.Empty<OwnerReviewListItemDto>();
        }

        var ids = rows.Select(row => row.UserId).ToList();
        var idStrings = ids.Select(id => id.ToString()).ToList();
        var users = await _context.Users.AsNoTracking()
            .Where(user => idStrings.Contains(user.Id))
            .Select(user => new { user.Id, user.Email, user.DisplayName, user.UserName, user.AuthMethod })
            .ToListAsync(cancellationToken);
        var usersById = users.ToDictionary(user => user.Id, StringComparer.OrdinalIgnoreCase);
        var counts = await _context.JoinElectionUsers.AsNoTracking()
            .Where(link => ids.Contains(link.UserId) && (link.Role == "Owner" || link.Role == "Admin"))
            .GroupBy(link => link.UserId)
            .Select(group => new { group.Key, Count = group.Select(link => link.ElectionGuid).Distinct().Count() })
            .ToListAsync(cancellationToken);
        var countById = counts.ToDictionary(item => item.Key, item => item.Count);

        return rows
            .Select(row =>
            {
                usersById.TryGetValue(row.UserId.ToString(), out var user);
                countById.TryGetValue(row.UserId, out var electionCount);
                return new OwnerReviewListItemDto
                {
                    UserId = row.UserId,
                    Email = user?.Email,
                    DisplayName = user?.DisplayName ?? user?.UserName,
                    AuthMethod = user?.AuthMethod,
                    Status = row.RestrictionStatus,
                    ReviewRequestedAt = row.ReviewRequestedAt,
                    RejectedAt = row.RejectedAt,
                    ElectionCount = electionCount
                };
            })
            .OrderByDescending(item => item.Status == OwnerRestrictionStatus.ReviewRequested)
            .ThenBy(item => item.Email)
            .ToList();
    }

    /// <inheritdoc />
    public async Task<OwnerReviewDetailDto?> GetDetailAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == userId.ToString(), cancellationToken);
        if (user == null)
        {
            return null;
        }

        var row = await _context.OwnerSendControls.AsNoTracking()
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken)
            ?? new OwnerSendControl { UserId = userId };
        return await BuildDetailAsync(user, row, cancellationToken);
    }

    /// <inheritdoc />
    public Task<OwnerReviewActionResult> ApproveAsync(
        Guid userId,
        string adminUserId,
        CancellationToken cancellationToken = default)
        => DecideAsync(userId, adminUserId, OwnerRestrictionStatus.Approved, null, cancellationToken);

    /// <inheritdoc />
    public Task<OwnerReviewActionResult> RejectAsync(
        Guid userId,
        string adminUserId,
        string? note,
        CancellationToken cancellationToken = default)
        => DecideAsync(userId, adminUserId, OwnerRestrictionStatus.Rejected, note, cancellationToken);

    /// <inheritdoc />
    public Task<OwnerReviewActionResult> HoldAsync(
        Guid userId,
        string adminUserId,
        string? note,
        CancellationToken cancellationToken = default)
        => DecideAsync(userId, adminUserId, OwnerRestrictionStatus.Restricted, note, cancellationToken);

    /// <inheritdoc />
    public async Task<OwnerReviewActionResult> DeleteElectionsNowAsync(
        Guid userId,
        string adminUserId,
        CancellationToken cancellationToken = default)
    {
        var row = await _context.OwnerSendControls
            .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (row == null || row.RestrictionStatus != OwnerRestrictionStatus.Rejected)
        {
            return OwnerReviewActionResult.Fail(NotRejectedKey);
        }

        var deleted = await DeleteOwnerElectionsAsync(userId, adminUserId, "Super admin deleted rejected owner elections", cancellationToken);
        _logger.LogInformation(
            "Super admin {AdminUserId} deleted {Count} elections for rejected owner {OwnerUserId}",
            adminUserId,
            deleted,
            userId);
        return OwnerReviewActionResult.Ok();
    }

    /// <inheritdoc />
    public async Task<int> DeleteExpiredRejectedElectionsAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = _clock.UtcNow.AddDays(-OwnerRestrictionStatus.RejectionRetentionDays);
        var due = await _context.OwnerSendControls.AsNoTracking()
            .Where(row =>
                row.RestrictionStatus == OwnerRestrictionStatus.Rejected
                && row.RejectedAt != null
                && row.RejectedAt <= cutoff)
            .Select(row => row.UserId)
            .ToListAsync(cancellationToken);

        var deleted = 0;
        foreach (var userId in due)
        {
            deleted += await DeleteOwnerElectionsAsync(
                userId,
                null,
                "Deleted elections 30 days after the owner was rejected",
                cancellationToken);
        }

        return deleted;
    }

    private async Task<OwnerReviewActionResult> DecideAsync(
        Guid userId,
        string adminUserId,
        string status,
        string? note,
        CancellationToken cancellationToken)
    {
        var trimmed = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
        if (trimmed != null && trimmed.Length > NoteMaxLength)
        {
            return OwnerReviewActionResult.Fail(NoteTooLongKey);
        }

        var user = await _context.Users.FirstOrDefaultAsync(item => item.Id == userId.ToString(), cancellationToken);
        if (user == null)
        {
            return OwnerReviewActionResult.Fail(NotFoundKey);
        }

        var row = await _context.OwnerSendControls.FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
        if (row == null)
        {
            row = new OwnerSendControl { UserId = userId };
            _context.OwnerSendControls.Add(row);
        }

        var now = _clock.UtcNow;
        var previous = row.RestrictionStatus;
        row.RestrictionStatus = status;
        row.StatusChangedAt = now;
        row.ReviewedAt = now;
        row.ReviewedByUserId = adminUserId;
        row.ReviewNote = trimmed;
        row.PaidSendsApproved = status == OwnerRestrictionStatus.Approved;

        if (status == OwnerRestrictionStatus.Approved)
        {
            row.ApprovedAt = now;
            row.ApprovedByUserId = adminUserId;
            row.RejectedAt = null;
            if (previous == OwnerRestrictionStatus.Rejected)
            {
                row.SendsFrozen = false;
            }
        }
        else
        {
            row.PaidSendsApproved = false;
        }

        if (status == OwnerRestrictionStatus.Rejected)
        {
            row.RejectedAt = now;
            row.SendsFrozen = true;
            await FreezeAndHideElectionsAsync(userId, cancellationToken);
        }
        else if (previous == OwnerRestrictionStatus.Rejected && status == OwnerRestrictionStatus.Approved)
        {
            await UnfreezeElectionsAsync(userId, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var eventType = status switch
        {
            OwnerRestrictionStatus.Approved => SecurityEventType.OwnerApproved,
            OwnerRestrictionStatus.Rejected => SecurityEventType.OwnerRejected,
            _ => SecurityEventType.OwnerReviewHeld
        };
        await AuditAsync(eventType, adminUserId, user.Email, status + " owner " + userId + (trimmed == null ? "" : " note: " + trimmed));
        if (status == OwnerRestrictionStatus.Approved)
        {
            await AuditAsync(SecurityEventType.PaidSendOwnerApproved, adminUserId, user.Email, "Owner approval includes paid sends for " + userId);
        }

        try
        {
            await SendOutcomeAsync(user, status, trimmed, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Owner outcome email failed for {OwnerUserId}", userId);
        }

        return OwnerReviewActionResult.Ok(ToDto(row));
    }

    private async Task FreezeAndHideElectionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var electionIds = await OwnedElectionIdsAsync(userId, cancellationToken);
        if (electionIds.Count == 0)
        {
            return;
        }

        var elections = await _context.Elections
            .Where(election => electionIds.Contains(election.ElectionGuid))
            .ToListAsync(cancellationToken);
        foreach (var election in elections)
        {
            election.UseOnlineVoting = false;
            election.OnlineWhenOpen = null;
            election.OnlineWhenClose = null;
            election.ListedForPublicAsOf = null;
        }

        var controls = await _context.ElectionSendControls
            .Where(row => electionIds.Contains(row.ElectionGuid))
            .ToListAsync(cancellationToken);
        foreach (var electionId in electionIds)
        {
            var control = controls.FirstOrDefault(row => row.ElectionGuid == electionId);
            if (control == null)
            {
                control = new ElectionSendControl { ElectionGuid = electionId };
                _context.ElectionSendControls.Add(control);
            }

            control.SendsFrozen = true;
        }
    }

    private async Task UnfreezeElectionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var electionIds = await OwnedElectionIdsAsync(userId, cancellationToken);
        var controls = await _context.ElectionSendControls
            .Where(row => electionIds.Contains(row.ElectionGuid))
            .ToListAsync(cancellationToken);
        foreach (var control in controls)
        {
            control.SendsFrozen = false;
        }
    }

    private async Task<int> DeleteOwnerElectionsAsync(
        Guid userId,
        string? actorUserId,
        string details,
        CancellationToken cancellationToken)
    {
        var electionIds = await OwnedElectionIdsAsync(userId, cancellationToken);
        if (electionIds.Count == 0)
        {
            return 0;
        }

        var coOwners = await _context.JoinElectionUsers.AsNoTracking()
            .Where(link =>
                electionIds.Contains(link.ElectionGuid)
                && link.UserId != userId
                && (link.Role == "Owner" || link.Role == "Admin"))
            .Select(link => new { link.ElectionGuid, link.UserId })
            .ToListAsync(cancellationToken);
        var otherIds = coOwners.Select(link => link.UserId).Distinct().ToList();
        var otherStatuses = otherIds.Count == 0
            ? new Dictionary<Guid, string>()
            : await _context.OwnerSendControls.AsNoTracking()
                .Where(row => otherIds.Contains(row.UserId))
                .ToDictionaryAsync(row => row.UserId, row => row.RestrictionStatus, cancellationToken);

        var shared = coOwners
            .Where(link =>
                !otherStatuses.TryGetValue(link.UserId, out var status)
                || status != OwnerRestrictionStatus.Rejected)
            .Select(link => link.ElectionGuid)
            .ToHashSet();

        var toDelete = electionIds.Where(id => !shared.Contains(id)).ToList();
        if (toDelete.Count == 0)
        {
            return 0;
        }

        var elections = await _context.Elections
            .Where(election => toDelete.Contains(election.ElectionGuid))
            .ToListAsync(cancellationToken);
        _context.Elections.RemoveRange(elections);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var election in elections)
        {
            await AuditAsync(
                SecurityEventType.OwnerElectionsDeleted,
                actorUserId ?? userId.ToString(),
                null,
                details + " election " + election.ElectionGuid,
                election.ElectionGuid);
        }

        return elections.Count;
    }

    private async Task<List<Guid>> OwnedElectionIdsAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await _context.JoinElectionUsers.AsNoTracking()
            .Where(link => link.UserId == userId && (link.Role == "Owner" || link.Role == "Admin"))
            .Select(link => link.ElectionGuid)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    private async Task<OwnerReviewDetailDto> BuildDetailAsync(
        AppUser user,
        OwnerSendControl row,
        CancellationToken cancellationToken)
    {
        var signedUp = await _context.SecurityAuditLogs.AsNoTracking()
            .Where(log =>
                log.UserId == user.Id
                && (log.EventType == SecurityEventType.AccountCreated
                    || log.EventType == SecurityEventType.OAuthLoginSuccess))
            .OrderBy(log => log.Timestamp)
            .Select(log => (DateTimeOffset?)log.Timestamp)
            .FirstOrDefaultAsync(cancellationToken);

        var detail = new OwnerReviewDetailDto
        {
            UserId = row.UserId,
            Email = user.Email,
            DisplayName = user.DisplayName ?? user.UserName,
            AuthMethod = user.AuthMethod,
            SignedUpAt = signedUp,
            Status = row.RestrictionStatus,
            Note = row.ReviewNote,
            ReviewRequestedAt = row.ReviewRequestedAt,
            ReviewedAt = row.ReviewedAt,
            ReviewedByUserId = row.ReviewedByUserId,
            RejectedAt = row.RejectedAt,
            DeleteElectionsAt = row.RejectedAt?.AddDays(OwnerRestrictionStatus.RejectionRetentionDays),
            Elections = await BuildElectionsAsync(row.UserId, cancellationToken)
        };
        return detail;
    }

    private async Task<List<OwnerReviewElectionDto>> BuildElectionsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var electionIds = await OwnedElectionIdsAsync(userId, cancellationToken);
        if (electionIds.Count == 0)
        {
            return new List<OwnerReviewElectionDto>();
        }

        var elections = await _context.Elections.AsNoTracking()
            .Where(election => electionIds.Contains(election.ElectionGuid))
            .Select(election => new
            {
                election.ElectionGuid,
                election.Name,
                election.ElectionType,
                election.DateOfElection,
                election.OnlineWhenOpen,
                election.OnlineWhenClose,
                election.ExpectedPhoneRegions
            })
            .ToListAsync(cancellationToken);

        var people = await _context.People.AsNoTracking()
            .Where(person => electionIds.Contains(person.ElectionGuid))
            .Select(person => new
            {
                person.ElectionGuid,
                person.FirstName,
                person.LastName,
                person.Email,
                person.Phone
            })
            .ToListAsync(cancellationToken);

        var flags = await _context.VoterContactFlags.AsNoTracking()
            .Where(flag => electionIds.Contains(flag.ElectionGuid) && flag.Active)
            .Select(flag => new { flag.ElectionGuid, flag.MaskedValue, flag.Reason })
            .ToListAsync(cancellationToken);

        return elections
            .OrderBy(election => election.Name)
            .Select(election =>
            {
                var rows = people.Where(person => person.ElectionGuid == election.ElectionGuid).ToList();
                var regionHint = FirstRegion(election.ExpectedPhoneRegions);
                var domains = rows
                    .Select(person => EmailDomain(person.Email))
                    .Where(domain => domain != null)
                    .GroupBy(domain => domain!, StringComparer.OrdinalIgnoreCase)
                    .Select(group => new OwnerReviewCountDto { Key = group.Key, Count = group.Count() })
                    .OrderByDescending(item => item.Count)
                    .ThenBy(item => item.Key)
                    .ToList();
                var countries = rows
                    .Select(person => PhoneCountry(person.Phone, regionHint))
                    .Where(country => country != null)
                    .GroupBy(country => country!)
                    .Select(group => new OwnerReviewCountDto { Key = group.Key, Count = group.Count() })
                    .OrderByDescending(item => item.Count)
                    .ThenBy(item => item.Key)
                    .ToList();
                var electionFlags = flags.Where(flag => flag.ElectionGuid == election.ElectionGuid).ToList();
                return new OwnerReviewElectionDto
                {
                    ElectionGuid = election.ElectionGuid,
                    Name = election.Name,
                    ElectionType = election.ElectionType,
                    DateOfElection = election.DateOfElection,
                    OnlineWhenOpen = election.OnlineWhenOpen,
                    OnlineWhenClose = election.OnlineWhenClose,
                    PeopleCount = rows.Count,
                    EmailDomains = domains,
                    PhoneCountries = countries,
                    FlaggedContactCount = electionFlags.Count,
                    FlaggedSample = electionFlags
                        .Take(OwnerRestrictionStatus.SampleSize)
                        .Select(flag => flag.MaskedValue + " (" + flag.Reason + ")")
                        .ToList(),
                    PeopleSample = rows
                        .Take(OwnerRestrictionStatus.SampleSize)
                        .Select(person => (person.FirstName + " " + person.LastName).Trim())
                        .Where(name => name.Length > 0)
                        .ToList()
                };
            })
            .ToList();
    }

    private async Task SendReviewRequestAsync(
        OwnerReviewDetailDto detail,
        IReadOnlyList<string> recipients,
        CancellationToken cancellationToken)
    {
        var link = FrontendUrlResolver.Build(
            _configuration,
            _hostEnvironment,
            "/super-admin/owner-reviews/" + detail.UserId.ToString("D"));
        var subject = OwnerRestrictionStatus.ReviewRequestSubjectPrefix + " " + (detail.Email ?? detail.UserId.ToString());
        var body = BuildReviewBody(detail, link);
        await SendAsync(recipients, subject, body, cancellationToken);
    }

    private async Task SendOutcomeAsync(
        AppUser user,
        string status,
        string? note,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(user.Email))
        {
            return;
        }

        var subject = status switch
        {
            OwnerRestrictionStatus.Approved => "[TallyJ] Your account is approved",
            OwnerRestrictionStatus.Rejected => "[TallyJ] Your account was declined",
            _ => "[TallyJ] Your account is still restricted"
        };
        var body = status switch
        {
            OwnerRestrictionStatus.Approved =>
                "Your TallyJ account is approved. You can send voter codes and open online voting.",
            OwnerRestrictionStatus.Rejected =>
                "Your TallyJ account was declined. Its elections are frozen and hidden from voters. "
                + "They will be deleted 30 days after this decision unless a site administrator deletes them sooner.",
            _ =>
                "Your TallyJ account is still restricted. You can set up elections and enter ballots. "
                + "Voter codes and online voting stay unavailable. You can request another review 24 hours after your last request."
        };
        if (!string.IsNullOrWhiteSpace(note))
        {
            body += Environment.NewLine + Environment.NewLine + "Note from the reviewer:" + Environment.NewLine + note;
        }

        await SendAsync(new[] { user.Email }, subject, body, cancellationToken);
    }

    internal static string BuildReviewBody(OwnerReviewDetailDto detail, string reviewUrl)
    {
        var lines = new List<string>
        {
            "A TallyJ owner asked for a review.",
            "Email: " + (detail.Email ?? "(none)"),
            "Name: " + (detail.DisplayName ?? "(none)"),
            "Signed up: " + FormatTime(detail.SignedUpAt),
            "Sign-in method: " + (detail.AuthMethod ?? "(none)"),
            "Status: " + detail.Status,
            "Review page: " + reviewUrl,
            ""
        };

        if (detail.Elections.Count == 0)
        {
            lines.Add("Elections: none yet.");
            return string.Join(Environment.NewLine, lines);
        }

        foreach (var election in detail.Elections)
        {
            lines.Add("Election: " + election.Name + " (" + election.ElectionGuid + ")");
            lines.Add("Type: " + (election.ElectionType ?? "(none)"));
            lines.Add("Election date: " + FormatTime(election.DateOfElection));
            lines.Add("Online window: " + FormatTime(election.OnlineWhenOpen) + " to " + FormatTime(election.OnlineWhenClose));
            lines.Add("People: " + election.PeopleCount);
            lines.Add("Email domains: " + FormatCounts(election.EmailDomains));
            lines.Add("Phone countries: " + FormatCounts(election.PhoneCountries));
            lines.Add("Flagged contacts: " + election.FlaggedContactCount);
            lines.Add("Flagged sample: " + (election.FlaggedSample.Count == 0 ? "(none)" : string.Join("; ", election.FlaggedSample)));
            lines.Add("People sample: " + (election.PeopleSample.Count == 0 ? "(none)" : string.Join("; ", election.PeopleSample)));
            lines.Add("");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private async Task SendAsync(
        IReadOnlyList<string> recipients,
        string subject,
        string body,
        CancellationToken cancellationToken)
    {
        var fromAddress = _configuration["Email:FromAddress"];
        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            fromAddress = "noreply@tallyj.com";
        }

        var fromName = _configuration["Email:FromName"];
        if (string.IsNullOrWhiteSpace(fromName))
        {
            fromName = "TallyJ4";
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        foreach (var recipient in recipients)
        {
            if (!MailboxAddress.TryParse(recipient, out var mailbox))
            {
                _logger.LogWarning("Skipped a review recipient that is not an email address");
                continue;
            }

            message.To.Add(mailbox);
        }

        if (message.To.Count == 0)
        {
            throw new InvalidOperationException("No valid review email recipients.");
        }

        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };
        cancellationToken.ThrowIfCancellationRequested();
        await _emailSender.SendAsync(message);
    }

    private List<string> SuperAdminRecipients()
        => _superAdmin.Emails
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(email => email.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private OwnerRestrictionDto ToDto(OwnerSendControl? row)
    {
        if (row == null || string.IsNullOrEmpty(row.RestrictionStatus))
        {
            return new OwnerRestrictionDto();
        }

        var blocks = OwnerRestrictionStatus.BlocksSends(row.RestrictionStatus);
        DateTimeOffset? next = null;
        var canRequest = false;
        if (row.RestrictionStatus is OwnerRestrictionStatus.Restricted or OwnerRestrictionStatus.ReviewRequested)
        {
            if (row.ReviewRequestedAt is DateTimeOffset requested)
            {
                var allowedAt = requested.AddHours(OwnerRestrictionStatus.ReviewRequestThrottleHours);
                if (allowedAt > _clock.UtcNow)
                {
                    next = allowedAt;
                }
            }

            canRequest = next == null;
        }

        return new OwnerRestrictionDto
        {
            Status = row.RestrictionStatus,
            Note = row.ReviewNote,
            ReviewRequestedAt = row.ReviewRequestedAt,
            NextRequestAt = next,
            CanRequestReview = canRequest,
            BlocksSends = blocks
        };
    }

    private async Task AuditAsync(
        SecurityEventType eventType,
        string userId,
        string? email,
        string details,
        Guid? electionGuid = null)
    {
        await _audit.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = eventType,
            UserId = userId,
            Email = email,
            ElectionGuid = electionGuid,
            Details = details,
            IsSuspicious = false,
            Severity = SecurityEventSeverity.Info
        });
    }

    private string? PhoneCountry(string? phone, string regionHint)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        try
        {
            var parsed = _phones.Parse(phone, regionHint);
            var region = _phones.GetRegionCodeForNumber(parsed);
            return string.IsNullOrEmpty(region) ? "unparsed" : region;
        }
        catch (NumberParseException)
        {
            return "unparsed";
        }
    }

    private static string? EmailDomain(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var at = email.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1)
        {
            return null;
        }

        return email[(at + 1)..].Trim().ToLowerInvariant();
    }

    private static string FirstRegion(string? expected)
    {
        if (string.IsNullOrWhiteSpace(expected))
        {
            return "CA";
        }

        var first = expected.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault();
        return string.IsNullOrWhiteSpace(first) ? "CA" : first;
    }

    private static string FormatCounts(IReadOnlyList<OwnerReviewCountDto> counts)
    {
        if (counts.Count == 0)
        {
            return "(none)";
        }

        return string.Join(", ", counts.Select(item => item.Key + " " + item.Count));
    }

    private static string FormatTime(DateTimeOffset? value)
        => value.HasValue ? value.Value.UtcDateTime.ToString("u") : "(none)";
}
