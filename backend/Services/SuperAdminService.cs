using Microsoft.EntityFrameworkCore;
using Backend.Domain.Context;
using Backend.Domain.Enumerations;
using Backend.DTOs.SuperAdmin;
using Backend.Models;

namespace Backend.Services;

public class SuperAdminService : ISuperAdminService
{
    private readonly MainDbContext _context;
    private readonly ILogger<SuperAdminService> _logger;

    public SuperAdminService(MainDbContext context, ILogger<SuperAdminService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SuperAdminSummaryDto> GetSummaryAsync()
    {
        var totalCount = await _context.Elections.CountAsync();

        var openCount = await _context.Elections
            .Where(e => e.TallyStatus != null
                        && e.TallyStatus != "Complete"
                        && e.TallyStatus != "Archived"
                        && e.DateOfElection <= DateTime.UtcNow)
            .CountAsync();

        var upcomingCount = await _context.Elections
            .Where(e => e.DateOfElection > DateTime.UtcNow
                        && (e.TallyStatus == null || (e.TallyStatus != "Complete" && e.TallyStatus != "Archived")))
            .CountAsync();

        var completedCount = await _context.Elections
            .Where(e => e.TallyStatus == "Complete")
            .CountAsync();

        var archivedCount = await _context.Elections
            .Where(e => e.TallyStatus == "Archived")
            .CountAsync();

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

    public async Task<PaginatedResponse<SuperAdminElectionDto>> GetElectionsAsync(SuperAdminElectionFilterDto filter)
    {
        var query = _context.Elections
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
            query = query.Where(e => e.TallyStatus == filter.Status);
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
                TallyStatus = e.TallyStatus,
                ElectionType = ElectionTypeEnum.ParseCode(e.ElectionType),
                VoterCount = e.People.Count(p => p.CanVote == true),
                BallotCount = e.Locations.SelectMany(l => l.Ballots).Count(),
                LocationCount = e.Locations.Count,
                OwnerEmail = ownerEmail
            };
        }).ToList();

        return PaginatedResponse<SuperAdminElectionDto>.Create(items, page, pageSize, totalCount);
    }

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
            TallyStatus = election.TallyStatus,
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

    private static IQueryable<Domain.Entities.Election> ApplySort(
        IQueryable<Domain.Entities.Election> query,
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
            "tallystatus" or "status" => isDescending
                ? query.OrderByDescending(e => e.TallyStatus)
                : query.OrderBy(e => e.TallyStatus),
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
                ? query.OrderByDescending(e => e.DateOfElection ?? DateTime.MinValue)
                : query.OrderBy(e => e.DateOfElection ?? DateTime.MinValue)
        };
    }
}



