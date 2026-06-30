using System.Security.Claims;
using Backend.Context;
using Backend.Helpers;
using Backend.Entities;
using Backend.Enumerations;
using Backend.DTOs.Elections;
using Backend.DTOs.SignalR;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service for managing election operations including creation, retrieval, updates, and deletion.
/// Provides functionality to handle elections and their associated data.
/// </summary>
public class ElectionService : IElectionService
{
    private readonly MainDbContext _context;
    private readonly ILogger<ElectionService> _logger;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// Initializes a new instance of the ElectionService.
    /// </summary>
    public ElectionService(MainDbContext context, ILogger<ElectionService> logger, ISignalRNotificationService signalRNotificationService, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _signalRNotificationService = signalRNotificationService;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// Retrieves a paginated list of elections with optional status filtering. Glen reviewed.
    /// </summary>
    /// <param name="pageNumber">The page number to retrieve (1-based). Default is 1.</param>
    /// <param name="pageSize">The number of elections per page. Default is 10.</param>
    /// <param name="status">Optional status filter to apply to elections.</param>
    /// <returns>A paginated response containing election summary DTOs.</returns>
    public async Task<PaginatedResponse<ElectionSummaryDto>> GetElectionsAsync(int pageNumber = 1, int pageSize = 10, string? status = null)
    {
        var userIdString = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? _httpContextAccessor.HttpContext?.User.FindFirst("sub")?.Value;

        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
        {
            _logger.LogWarning("GetElectionsAsync: Could not parse user ID from claims");
            return PaginatedResponse<ElectionSummaryDto>.Create(new List<ElectionSummaryDto>(), pageNumber, pageSize, 0);
        }

        var query = _context.Elections
            .Where(e => _context.JoinElectionUsers.Any(jeu => jeu.ElectionGuid == e.ElectionGuid && jeu.UserId == userId));

        if (!string.IsNullOrWhiteSpace(status))
        {
            if (Enum.TryParse<ElectionStage>(status, out var stageFilter))
            {
                query = query.Where(e => e.ElectionStage == stageFilter);
            }
            else
            {
                _logger.LogWarning("GetElectionsAsync: Invalid status filter '{Status}' provided", status);
                return PaginatedResponse<ElectionSummaryDto>.Create([], pageNumber, pageSize, 0);
            }
        }

        var totalCount = await query.CountAsync();

        var elections = await query
            .OrderByDescending(e => e.DateOfElection)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(e => e.People)
            .Include(e => e.Locations)
                .ThenInclude(l => l.Ballots)
            .ToListAsync();

        var electionDtos = elections.Select(e => new ElectionSummaryDto
        {
            ElectionGuid = e.ElectionGuid,
            Name = e.Name,
            DateOfElection = e.DateOfElection,
            ElectionStage = e.ElectionStage,
            VoterCount = e.People.Count(p => p.CanVote == true),
            BallotCount = e.Locations.SelectMany(l => l.Ballots).Count(),
            ElectionType = ElectionTypeEnum.ParseCode(e.ElectionType),
            IsTellerAccessOpen = ElectionTellerAccessHelper.IsGuestTellerAccessOpen(e.ListedForPublicAsOf),
            IsOnlineVotingEnabled = e.OnlineWhenOpen != null && e.OnlineWhenClose != null,
            ShowAsTest = e.ShowAsTest
        }).ToList();

        return PaginatedResponse<ElectionSummaryDto>.Create(electionDtos, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// Retrieves a specific election by its unique identifier.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>An ElectionDto containing the election information, or null if not found.</returns>
    public async Task<ElectionDto?> GetElectionByGuidAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .Include(e => e.People)
            .Include(e => e.Locations)
                .ThenInclude(l => l.Ballots)
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return null;
        }

        var dto = MapToElectionDto(election);
        dto.VoterCount = election.People.Count(p => p.CanVote == true);
        dto.BallotCount = election.Locations.SelectMany(l => l.Ballots).Count();
        dto.LocationCount = election.Locations.Count;
        dto.ElectionType = ElectionTypeEnum.ParseCode(election.ElectionType);
        dto.HasUnits = election.People.Any(p => !string.IsNullOrWhiteSpace(p.UnitName));
        return dto;
    }

    /// <summary>
    /// Creates a new election.
    /// </summary>
    /// <param name="createDto">The data transfer object containing election creation information.</param>
    /// <returns>An ElectionDto representing the created election.</returns>
    public async Task<ElectionDto> CreateElectionAsync(CreateElectionDto createDto)
    {
        var election = MapFromCreateElectionDto(createDto);
        election.ElectionGuid = Guid.NewGuid();
        election.ElectionStage = ElectionStage.SettingUp;
        election.RowVersion = new byte[8];
        _context.Elections.Add(election);
        await _context.SaveChangesAsync();

        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out var userId))
        {
            var joinEntry = new JoinElectionUser
            {
                ElectionGuid = election.ElectionGuid,
                UserId = userId,
                Role = "Admin"
            };
            _context.JoinElectionUsers.Add(joinEntry);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Created election {ElectionGuid} - {Name}", election.ElectionGuid, election.Name);

        return await GetElectionByGuidAsync(election.ElectionGuid) ?? MapToElectionDto(election);
    }

    /// <summary>
    /// Updates an existing election with new information.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to update.</param>
    /// <param name="updateDto">The data transfer object containing updated election information.</param>
    /// <returns>An ElectionDto representing the updated election, or null if the election was not found.</returns>
    public async Task<ElectionDto?> UpdateElectionAsync(Guid electionGuid, UpdateElectionDto updateDto)
    {
        var election = await _context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return null;
        }

        var listForPublic = updateDto.ListForPublic;
        updateDto.CopyMatchingPropertiesTo(election, ignoreNulls: true);
        ElectionTellerAccessHelper.ApplyListForPublicFlag(election, listForPublic);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated election {ElectionGuid}", electionGuid);

        await _signalRNotificationService.SendElectionUpdateAsync(new ElectionUpdateDto
        {
            ElectionGuid = election.ElectionGuid,
            Name = election.Name,
            ElectionStage = election.ElectionStage,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        return await GetElectionByGuidAsync(electionGuid);
    }

    /// <summary>
    /// Changes the stage of an existing election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to update.</param>
    /// <param name="dto">The requested stage and optional confirmation flags.</param>
    /// <returns>The stage change result (success, not found, invalid transition, or confirmation required).</returns>
    public async Task<ChangeElectionStageResult> ChangeElectionStageAsync(
        Guid electionGuid,
        ChangeElectionStageDto dto)
    {
        var newStage = dto.ElectionStage;
        var election = await _context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return ChangeElectionStageResult.NotFound();
        }

        var currentStage = election.ElectionStage;

        if (!ElectionStageTransitions.CanTransition(currentStage, newStage, out var reason))
        {
            _logger.LogWarning(
                "Rejected stage change for election {ElectionGuid}: {CurrentStage} -> {NewStage}. {Reason}",
                electionGuid,
                currentStage,
                newStage,
                reason);
            return ChangeElectionStageResult.InvalidTransition(reason);
        }

        if (currentStage == ElectionStage.Finalized && newStage != ElectionStage.Finalized && !dto.ConfirmLeavingFinalized)
        {
            _logger.LogInformation(
                "Stage change for election {ElectionGuid} from Finalized to {NewStage} requires confirmation",
                electionGuid,
                newStage);
            return ChangeElectionStageResult.ConfirmationRequired(
                ElectionStageMessageKeys.ConfirmLeaveFinalized);
        }

        if (newStage == ElectionStage.Finalized && currentStage != ElectionStage.Finalized)
        {
            var readiness = await ElectionStageFinalizationReadiness.EvaluateAsync(_context, electionGuid);
            if (!readiness.IsReady)
            {
                var blockerSummary = string.Join("; ", readiness.Blockers);
                _logger.LogWarning(
                    "Rejected finalization for election {ElectionGuid}: {Blockers}",
                    electionGuid,
                    blockerSummary);
                return ChangeElectionStageResult.InvalidTransition(blockerSummary);
            }
        }

        election.ElectionStage = newStage;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Changed election {ElectionGuid} stage from {PreviousStage} to {NewStage}. ConfirmLeavingFinalized={ConfirmLeavingFinalized}",
            electionGuid,
            currentStage,
            newStage,
            dto.ConfirmLeavingFinalized);

        await _signalRNotificationService.SendElectionUpdateAsync(new ElectionUpdateDto
        {
            ElectionGuid = election.ElectionGuid,
            Name = election.Name,
            ElectionStage = election.ElectionStage,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var updated = await GetElectionByGuidAsync(electionGuid);
        return updated == null
            ? ChangeElectionStageResult.NotFound()
            : ChangeElectionStageResult.Success(updated);
    }

    /// <summary>
    /// Deletes an election by its unique identifier.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to delete.</param>
    /// <returns>True if the election was successfully deleted, false if the election was not found.</returns>
    public async Task<bool> DeleteElectionAsync(Guid electionGuid)
    {
        var election = await _context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return false;
        }

        _context.Elections.Remove(election);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted election {ElectionGuid}", electionGuid);

        return true;
    }

    /// <summary>
    /// Retrieves a summary of a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>An ElectionDto containing the election summary information, or null if not found.</returns>
    public async Task<ElectionDto?> GetElectionSummaryAsync(Guid electionGuid)
    {
        return await GetElectionByGuidAsync(electionGuid);
    }

    /// <summary>
    /// Toggles teller access for an election by setting or clearing the ListedForPublicAsOf timestamp.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="isOpen">Whether to open or close teller access.</param>
    /// <returns>The updated ElectionDto, or null if the election was not found.</returns>
    public async Task<ElectionDto?> ToggleTellerAccessAsync(Guid electionGuid, bool isOpen)
    {
        var election = await _context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return null;
        }

        election.ListedForPublicAsOf = isOpen ? DateTimeOffset.UtcNow : null;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Toggled teller access for election {ElectionGuid} to {IsOpen}", electionGuid, isOpen);

        await _signalRNotificationService.SendPublicElectionListUpdateAsync(electionGuid, isOpen);

        if (!isOpen)
        {
            await _signalRNotificationService.CloseOutGuestTellersAsync(electionGuid);
        }

        return await GetElectionByGuidAsync(electionGuid);
    }

    /// <summary>
    /// Updates the public listing status of an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="isListed">Whether the election should be listed for public access.</param>
    /// <returns>True if the listing status was updated successfully, false if the election was not found.</returns>
    public async Task<bool> UpdateElectionListingAsync(Guid electionGuid, bool isListed)
    {
        var updated = await ToggleTellerAccessAsync(electionGuid, isListed);
        return updated != null;
    }

    // =====================================================================
    // Explicit mapping helpers (replaces hidden Mapster profile logic).
    // All transformations are now visible and easy to understand.
    // =====================================================================

    private static ElectionDto MapToElectionDto(Election election)
    {
        var dto = election.CopyMatchingPropertiesToNew<ElectionDto>();

        // Enum conversions (were previously hidden in Mapster .Map expressions)
        dto.ElectionType = ElectionTypeEnum.ParseCode(election.ElectionType);
        dto.ElectionMode = ElectionModeEnum.ParseCode(election.ElectionMode);

        // Derived/computed fields
        dto.IsTellerAccessOpen = ElectionTellerAccessHelper.IsGuestTellerAccessOpen(election.ListedForPublicAsOf);
        dto.TellerAccessOpenedAt = election.ListedForPublicAsOf;
        dto.ListForPublic = dto.IsTellerAccessOpen;

        // Note: VoterCount, BallotCount, and LocationCount are set by the caller
        // after loading the necessary navigation properties (kept explicit for clarity).

        return dto;
    }

    private static Election MapFromCreateElectionDto(CreateElectionDto dto)
    {
        var election = dto.CopyMatchingPropertiesToNew<Election>();

        // Enum conversions on create
        election.ElectionType = ElectionTypeEnum.ToCodeString(dto.ElectionType);
        election.ElectionMode = ElectionModeEnum.ToCodeString(dto.ElectionMode);
        ElectionTellerAccessHelper.ApplyListForPublicFlag(election, dto.ListForPublic);

        return election;
    }
}



