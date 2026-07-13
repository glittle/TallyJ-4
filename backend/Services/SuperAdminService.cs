using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.DTOs.Security;
using Backend.DTOs.SuperAdmin;
using Backend.Identity;
using Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service implementation for super admin functionality providing system-wide election management and monitoring.
/// </summary>
public class SuperAdminService : ISuperAdminService
{
    private readonly MainDbContext _context;
    private readonly UserManager<AppUser> _userManager;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly ILogger<SuperAdminService> _logger;

    public SuperAdminService(
        MainDbContext context,
        UserManager<AppUser> userManager,
        ISecurityAuditService securityAuditService,
        ILogger<SuperAdminService> logger)
    {
        _context = context;
        _userManager = userManager;
        _securityAuditService = securityAuditService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a summary of system-wide election statistics for the super admin dashboard.
    /// </summary>
    /// <returns>A task containing the super admin summary data.</returns>
    public async Task<SuperAdminSummaryDto> GetSummaryAsync()
    {
        var totalCount = await _context.Elections.CountAsync();

        var openCount = await _context.Elections
            .Where(e => e.ElectionStage != ElectionStage.ProcessingBallots
                        && e.DateOfElection <= DateTimeOffset.UtcNow)
            .CountAsync();

        var upcomingCount = await _context.Elections
            .Where(e => e.DateOfElection > DateTimeOffset.UtcNow
                        && e.ElectionStage != ElectionStage.ProcessingBallots)
            .CountAsync();

        var completedCount = await _context.Elections
            .Where(e => e.ElectionStage == ElectionStage.ProcessingBallots)
            .CountAsync();

        var archivedCount = 0;

        _logger.LogInformation(
            "SuperAdmin summary: {Total} total, {Open} open, {Upcoming} upcoming, {Completed} completed, {Archived} archived",
            totalCount, openCount, upcomingCount, completedCount, archivedCount);

        return new SuperAdminSummaryDto
        {
            TotalElections = totalCount,
            OpenElections = openCount,
            UpcomingElections = upcomingCount,
            CompletedElections = completedCount,
            ArchivedElections = archivedCount
        };
    }

    /// <summary>
    /// Gets a paginated list of elections based on the provided filter criteria.
    /// </summary>
    /// <param name="filter">The filter criteria for querying elections.</param>
    /// <returns>A task containing paginated election data.</returns>
    public async Task<PaginatedResponse<SuperAdminElectionDto>> GetElectionsAsync(SuperAdminElectionFilterDto filter)
    {
        var query = _context.Elections
            .AsSplitQuery()
            .Include(e => e.People)
            .Include(e => e.Locations)
                .ThenInclude(l => l.Ballots)
            .Include(e => e.JoinElectionUsers)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var search = filter.Search.Trim();
            query = query.Where(e =>
                e.Name.Contains(search) ||
                (e.Convenor != null && e.Convenor.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (Enum.TryParse<ElectionStage>(filter.Status, out var stageFilter))
            {
                query = query.Where(e => e.ElectionStage == stageFilter);
            }
            else
            {
                _logger.LogWarning("GetElectionsAsync: Invalid status filter '{Status}' provided", filter.Status);
                return PaginatedResponse<SuperAdminElectionDto>.Create([], filter.Page, filter.PageSize, 0);
            }
        }

        if (filter.ElectionType.HasValue)
        {
            var filterTypeString = filter.ElectionType.Value.ToString();
            query = query.Where(e => e.ElectionType == filterTypeString);
        }

        var totalCount = await query.CountAsync();

        query = ApplySort(query, filter.SortBy, filter.SortDirection);

        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 100);

        var elections = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var ownerUserIds = elections
            .SelectMany(e => e.JoinElectionUsers)
            .Where(jeu => jeu.Role == "Owner")
            .Select(jeu => jeu.UserId.ToString())
            .Distinct()
            .ToList();

        var ownerEmails = await _context.Users
            .Where(u => ownerUserIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Email);

        var items = elections.Select(e =>
        {
            var ownerJeu = e.JoinElectionUsers.FirstOrDefault(jeu => jeu.Role == "Owner");
            string? ownerEmail = null;
            if (ownerJeu != null)
            {
                ownerEmails.TryGetValue(ownerJeu.UserId.ToString(), out ownerEmail);
            }

            return new SuperAdminElectionDto
            {
                ElectionGuid = e.ElectionGuid,
                Name = e.Name,
                Convenor = e.Convenor,
                DateOfElection = e.DateOfElection,
                ElectionStage = e.ElectionStage,
                ElectionType = ElectionTypeEnum.ParseCode(e.ElectionType),
                VoterCount = e.People.Count(p => p.CanVote == true),
                BallotCount = e.Locations.SelectMany(l => l.Ballots).Count(),
                LocationCount = e.Locations.Count,
                OwnerEmail = ownerEmail
            };
        }).ToList();

        return PaginatedResponse<SuperAdminElectionDto>.Create(items, page, pageSize, totalCount);
    }

    /// <summary>
    /// Gets detailed information about a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A task containing detailed election information, or null if not found.</returns>
    public async Task<SuperAdminElectionDetailDto?> GetElectionDetailAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .Include(e => e.People)
            .Include(e => e.Locations)
                .ThenInclude(l => l.Ballots)
            .Include(e => e.JoinElectionUsers)
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return null;
        }

        var userIds = election.JoinElectionUsers
            .Select(jeu => jeu.UserId.ToString())
            .Distinct()
            .ToList();

        var users = await _context.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u);

        var voterCount = election.People.Count(p => p.CanVote == true);
        var ballotCount = election.Locations.SelectMany(l => l.Ballots).Count();

        double percentComplete = 0;
        if (voterCount > 0)
        {
            percentComplete = Math.Round((double)ballotCount / voterCount * 100, 2);
            if (percentComplete > 100) percentComplete = 100;
        }

        var ownerJeu = election.JoinElectionUsers.FirstOrDefault(jeu => jeu.Role == "Owner");
        string? ownerEmail = null;
        if (ownerJeu != null && users.TryGetValue(ownerJeu.UserId.ToString(), out var ownerUser))
        {
            ownerEmail = ownerUser.Email;
        }

        var owners = election.JoinElectionUsers
            .Select(jeu =>
            {
                users.TryGetValue(jeu.UserId.ToString(), out var user);
                return new SuperAdminElectionOwnerDto
                {
                    Email = user?.Email,
                    DisplayName = user?.DisplayName ?? user?.UserName,
                    Role = jeu.Role
                };
            })
            .ToList();

        return new SuperAdminElectionDetailDto
        {
            ElectionGuid = election.ElectionGuid,
            Name = election.Name,
            Convenor = election.Convenor,
            DateOfElection = election.DateOfElection,
            ElectionStage = election.ElectionStage,
            ElectionType = ElectionTypeEnum.ParseCode(election.ElectionType),
            VoterCount = voterCount,
            BallotCount = ballotCount,
            LocationCount = election.Locations.Count,
            OwnerEmail = ownerEmail,
            NumberToElect = election.NumberToElect,
            ElectionMode = ElectionModeEnum.ParseCode(election.ElectionMode),
            PercentComplete = percentComplete,
            Owners = owners
        };
    }

    public async Task<PaginatedResponse<SuperAdminUserDto>> GetUsersAsync(SuperAdminUserFilterDto filter)
    {
        var page = filter.Page < 1 ? 1 : filter.Page;
        var pageSize = filter.PageSize < 1 ? 25 : Math.Min(filter.PageSize, 100);

        var query = _context.Users.AsQueryable();
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(u =>
                (u.Email != null && u.Email.ToLower().Contains(term)) ||
                (u.DisplayName != null && u.DisplayName.ToLower().Contains(term)) ||
                (u.UserName != null && u.UserName.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(u => u.Email)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new SuperAdminUserDto
            {
                Id = u.Id,
                Email = u.Email,
                DisplayName = u.DisplayName,
                AuthMethod = u.AuthMethod,
                EmailConfirmed = u.EmailConfirmed,
                PendingEmail = u.PendingEmail,
                LockoutEnd = u.LockoutEnd
            })
            .ToListAsync();

        return new PaginatedResponse<SuperAdminUserDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = page,
            PageSize = pageSize
        };
    }

    public async Task<SuperAdminUserDetailDto?> GetUserDetailAsync(string userId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        var history = await _context.UserEmailChangeLogs
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.ChangedAt)
            .Select(l => new SuperAdminEmailChangeEntryDto
            {
                OldEmail = l.OldEmail,
                NewEmail = l.NewEmail,
                ChangedAt = l.ChangedAt,
                Source = l.Source,
                ChangedByUserId = l.ChangedByUserId
            })
            .ToListAsync();

        return new SuperAdminUserDetailDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AuthMethod = user.AuthMethod,
            EmailConfirmed = user.EmailConfirmed,
            PendingEmail = user.PendingEmail,
            LockoutEnd = user.LockoutEnd,
            EmailHistory = history
        };
    }

    public async Task<SuperAdminUserDetailDto?> UpdateUserAsync(
        string userId,
        SuperAdminUpdateUserDto dto,
        string adminUserId)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return null;
        }

        if (dto.DisplayName != null)
        {
            var trimmed = dto.DisplayName.Trim();
            user.DisplayName = string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }

        if (!string.IsNullOrWhiteSpace(dto.Email) &&
            !string.Equals(dto.Email.Trim(), user.Email, StringComparison.OrdinalIgnoreCase))
        {
            var newEmail = dto.Email.Trim();
            var existing = await _userManager.FindByEmailAsync(newEmail);
            if (existing != null && existing.Id != userId)
            {
                throw new InvalidOperationException("Email already in use");
            }

            var oldEmail = user.Email ?? "";
            var syncUserName = string.Equals(user.UserName, oldEmail, StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrEmpty(user.UserName);

            user.Email = newEmail;
            user.NormalizedEmail = _userManager.NormalizeEmail(newEmail);
            user.EmailConfirmed = true;
            if (syncUserName)
            {
                user.UserName = newEmail;
                user.NormalizedUserName = _userManager.NormalizeName(newEmail);
            }

            // Clear any pending self-service change
            user.PendingEmail = null;
            user.PendingEmailCode = null;
            user.PendingEmailToken = null;
            user.PendingEmailExpiry = null;

            _context.UserEmailChangeLogs.Add(new UserEmailChangeLog
            {
                UserId = user.Id,
                OldEmail = oldEmail,
                NewEmail = newEmail,
                ChangedAt = DateTimeOffset.UtcNow,
                ChangedByUserId = adminUserId,
                Source = "SuperAdmin"
            });

            await _securityAuditService.LogSecurityEventAsync(new CreateSecurityAuditLogDto
            {
                EventType = SecurityEventType.EmailChanged,
                UserId = user.Id,
                Details = "SuperAdmin changed user email",
                Severity = SecurityEventSeverity.Warning,
                Metadata = new Dictionary<string, string>
                {
                    ["source"] = "SuperAdmin",
                    ["adminUserId"] = adminUserId
                }
            });
        }

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to update user: {errors}");
        }

        await _context.SaveChangesAsync();
        return await GetUserDetailAsync(userId);
    }

    private static IQueryable<Election> ApplySort(
        IQueryable<Election> query,
        string sortBy,
        string sortDirection)
    {
        var isDescending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);

        return sortBy?.ToLowerInvariant() switch
        {
            "name" => isDescending
                ? query.OrderByDescending(e => e.Name)
                : query.OrderBy(e => e.Name),
            "convenor" => isDescending
                ? query.OrderByDescending(e => e.Convenor)
                : query.OrderBy(e => e.Convenor),
            "electionstage" or "stage" or "status" => isDescending
                ? query.OrderByDescending(e => e.ElectionStage)
                : query.OrderBy(e => e.ElectionStage),
            "electiontype" or "type" => isDescending
                ? query.OrderByDescending(e => e.ElectionType)
                : query.OrderBy(e => e.ElectionType),
            "votecount" or "voters" => isDescending
                ? query.OrderByDescending(e => e.People.Count(p => p.CanVote == true))
                : query.OrderBy(e => e.People.Count(p => p.CanVote == true)),
            "ballotcount" or "ballots" => isDescending
                ? query.OrderByDescending(e => e.Locations.Sum(l => l.Ballots.Count))
                : query.OrderBy(e => e.Locations.Sum(l => l.Ballots.Count)),
            _ => isDescending
                ? query.OrderByDescending(e => e.DateOfElection ?? DateTimeOffset.MinValue)
                : query.OrderBy(e => e.DateOfElection ?? DateTimeOffset.MinValue)
        };
    }
}



