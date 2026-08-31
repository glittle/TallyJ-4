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
            .ToListAsync();

        var electionGuids = elections.Select(e => e.ElectionGuid).ToList();
        var voterCounts = await GetVoterCountsByElectionAsync(electionGuids);
        var ballotCounts = await GetBallotCountsByElectionAsync(electionGuids);

        var electionDtos = elections.Select(e => new ElectionSummaryDto
        {
            ElectionGuid = e.ElectionGuid,
            Name = e.Name,
            DateOfElection = e.DateOfElection,
            ElectionStage = e.ElectionStage,
            VoterCount = voterCounts.TryGetValue(e.ElectionGuid, out var voterCount) ? voterCount : 0,
            BallotCount = ballotCounts.TryGetValue(e.ElectionGuid, out var ballotCount) ? ballotCount : 0,
            ElectionType = ElectionTypeEnum.ParseCode(e.ElectionType),
            IsTellerAccessOpen = ElectionTellerAccessHelper.IsGuestTellerAccessOpen(e.ListedForPublicAsOf),
            IsOnlineVotingEnabled = e.UseOnlineVoting,
            ShowAsTest = e.ShowAsTest,
            ToElect = e.NumberToElect,
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
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return null;
        }

        return MapToElectionDto(election);
    }

    /// <summary>
    /// Retrieves aggregate voter, ballot, and location counts for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>An ElectionStatsDto, or null if the election was not found.</returns>
    public async Task<ElectionStatsDto?> GetElectionStatsAsync(Guid electionGuid)
    {
        var exists = await _context.Elections
            .AnyAsync(e => e.ElectionGuid == electionGuid);

        if (!exists)
        {
            return null;
        }

        var counts = await GetElectionCountsAsync(electionGuid);
        return new ElectionStatsDto
        {
            VoterCount = counts.VoterCount,
            BallotCount = counts.BallotCount,
            LocationCount = counts.LocationCount,
        };
    }

    /// <summary>
    /// Retrieves lightweight election status (identity, stage, and aggregate counts).
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>An ElectionStatusDto, or null if the election was not found.</returns>
    public async Task<ElectionStatusDto?> GetElectionStatusAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .AsNoTracking()
            .Where(e => e.ElectionGuid == electionGuid)
            .Select(e => new
            {
                e.ElectionGuid,
                e.Name,
                e.DateOfElection,
                e.ElectionType,
                e.ElectionStage
            })
            .FirstOrDefaultAsync();

        if (election == null)
        {
            _logger.LogWarning("Election {ElectionGuid} not found", electionGuid);
            return null;
        }

        var counts = await GetElectionCountsAsync(electionGuid);

        return new ElectionStatusDto
        {
            ElectionGuid = election.ElectionGuid,
            Name = election.Name,
            DateOfElection = election.DateOfElection,
            ElectionType = ElectionTypeEnum.ParseCode(election.ElectionType),
            ElectionStage = election.ElectionStage,
            IsActive = election.ElectionStage != ElectionStage.ProcessingBallots,
            RegisteredVoters = counts.VoterCount,
            BallotsSubmitted = counts.BallotCount
        };
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

        if (TryGetCurrentUserId(out var userId))
        {
            AddCurrentUserAsAdmin(election.ElectionGuid, userId);
            await _context.SaveChangesAsync();
        }

        _logger.LogInformation("Created election {ElectionGuid} - {Name}", election.ElectionGuid, election.Name);

        return await GetElectionByGuidAsync(election.ElectionGuid) ?? MapToElectionDto(election);
    }

    /// <summary>
    /// Duplicates an owned election as a new test copy.
    /// Copies election settings, locations, and people (new PersonGuids).
    /// Does not copy ballots, results, computers, tellers, online votes, SMS logs, or analysis.
    /// </summary>
    public async Task<DuplicateElectionResult> DuplicateElectionAsync(
        Guid sourceElectionGuid,
        DuplicateElectionDto dto)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return DuplicateElectionResult.Forbidden();
        }

        var source = await _context.Elections
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.ElectionGuid == sourceElectionGuid);

        if (source == null)
        {
            return DuplicateElectionResult.NotFound();
        }

        var canDuplicate = await _context.JoinElectionUsers
            .AnyAsync(j =>
                j.ElectionGuid == sourceElectionGuid
                && j.UserId == userId
                && (j.Role == "Owner" || j.Role == "Admin"));

        if (!canDuplicate)
        {
            return DuplicateElectionResult.Forbidden();
        }

        var sourceLocations = await _context.Locations
            .AsNoTracking()
            .Where(l => l.ElectionGuid == sourceElectionGuid)
            .ToListAsync();

        var sourcePeople = await _context.People
            .AsNoTracking()
            .Where(p => p.ElectionGuid == sourceElectionGuid)
            .ToListAsync();

        var copy = CopyElectionSettings(source);
        copy.ElectionGuid = Guid.NewGuid();
        copy.Name = ResolveDuplicateName(dto.Name, source.Name);
        copy.ShowAsTest = true;
        copy.ElectionStage = ElectionStage.SettingUp;
        copy.LastEnvNum = null;
        copy.ListedForPublicAsOf = null;
        copy.OwnerLoginId = null;
        // GetAvailableElectionsAsync treats UseOnlineVoting + a null window as open.
        // Clear both so the copy is not listed to the same phone/email/kiosk voters.
        copy.OnlineWhenOpen = null;
        copy.OnlineWhenClose = null;
        copy.UseOnlineVoting = false;
        copy.RowVersion = new byte[8];

        _context.Elections.Add(copy);
        AddCurrentUserAsAdmin(copy.ElectionGuid, userId);

        foreach (var location in sourceLocations)
        {
            _context.Locations.Add(CopyLocationForElection(location, copy.ElectionGuid));
        }

        foreach (var person in sourcePeople)
        {
            _context.People.Add(CopyPersonForElection(person, copy.ElectionGuid));
        }

        await OnlineVoterPhoneHelper.EnsureOnlineVotersForPhonesAsync(
            _context,
            sourcePeople.Select(p => p.Phone));

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Duplicated election {SourceElectionGuid} to test copy {ElectionGuid} - {Name}",
            sourceElectionGuid,
            copy.ElectionGuid,
            copy.Name);

        var dtoResult = await GetElectionByGuidAsync(copy.ElectionGuid) ?? MapToElectionDto(copy);
        return DuplicateElectionResult.Success(dtoResult);
    }

    /// <summary>
    /// Resets runtime data on a ShowAsTest election so it can be practiced again.
    /// Same wipe list as what DuplicateElectionAsync does not copy. Refuses when
    /// ShowAsTest is false or null. People, locations, settings, ShowAsTest, and
    /// ownership stay. Stage returns to SettingUp. Online window is closed the
    /// same way as a new test copy (UseOnlineVoting false and dates cleared).
    /// </summary>
    public async Task<ResetElectionResult> ResetElectionAsync(Guid electionGuid)
    {
        if (!TryGetCurrentUserId(out var userId))
        {
            return ResetElectionResult.Forbidden();
        }

        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return ResetElectionResult.NotFound();
        }

        var canReset = await _context.JoinElectionUsers
            .AnyAsync(j =>
                j.ElectionGuid == electionGuid
                && j.UserId == userId
                && (j.Role == "Owner" || j.Role == "Admin"));

        if (!canReset)
        {
            return ResetElectionResult.Forbidden();
        }

        if (election.ShowAsTest != true)
        {
            return ResetElectionResult.NotTest();
        }

        var guestAccessWasOpen = ElectionTellerAccessHelper.IsGuestTellerAccessOpen(election.ListedForPublicAsOf);
        var previousOnlineWhenOpen = election.OnlineWhenOpen;
        var previousOnlineWhenClose = election.OnlineWhenClose;
        var previousOnlineCloseIsEstimate = election.OnlineCloseIsEstimate;
        var previousOnlineSelectionProcess = election.OnlineSelectionProcess;

        await RemoveRuntimeRowsAsync(electionGuid);
        ClearPersonRuntimeFields(electionGuid);
        ClearLocationRuntimeFields(electionGuid);

        election.LastEnvNum = null;
        election.ListedForPublicAsOf = null;
        // GetAvailableElectionsAsync treats UseOnlineVoting + a null window as open.
        election.OnlineWhenOpen = null;
        election.OnlineWhenClose = null;
        election.UseOnlineVoting = false;
        election.ElectionStage = ElectionStage.SettingUp;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Reset test election {ElectionGuid}", electionGuid);

        await _signalRNotificationService.SendElectionUpdateAsync(new ElectionUpdateDto
        {
            ElectionGuid = election.ElectionGuid,
            Name = election.Name,
            ElectionStage = election.ElectionStage,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        var onlineSettingsChanged =
            previousOnlineWhenOpen != election.OnlineWhenOpen
            || previousOnlineWhenClose != election.OnlineWhenClose
            || previousOnlineCloseIsEstimate != election.OnlineCloseIsEstimate
            || !string.Equals(previousOnlineSelectionProcess, election.OnlineSelectionProcess, StringComparison.Ordinal);

        if (onlineSettingsChanged)
        {
            await _signalRNotificationService.SendOnlineElectionUpdateAsync(new OnlineElectionUpdateDto
            {
                ElectionGuid = election.ElectionGuid,
                OnlineWhenOpen = election.OnlineWhenOpen,
                OnlineWhenClose = election.OnlineWhenClose,
                OnlineCloseIsEstimate = election.OnlineCloseIsEstimate,
                OnlineSelectionProcess = election.OnlineSelectionProcess
            });
        }

        if (guestAccessWasOpen)
        {
            await _signalRNotificationService.SendPublicElectionListUpdateAsync(electionGuid, false);
            await _signalRNotificationService.CloseOutGuestTellersAsync(electionGuid);
        }

        await _signalRNotificationService.RequestFrontDeskReloadAsync(electionGuid);

        var dtoResult = await GetElectionByGuidAsync(electionGuid) ?? MapToElectionDto(election);
        return ResetElectionResult.Success(dtoResult);
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

        var previousOnlineWhenOpen = election.OnlineWhenOpen;
        var previousOnlineWhenClose = election.OnlineWhenClose;
        var previousOnlineCloseIsEstimate = election.OnlineCloseIsEstimate;
        var previousOnlineSelectionProcess = election.OnlineSelectionProcess;

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

        // Operator monitor / front desk need a live push when the online window or process changes
        // (FrontDeskHub updateOnlineElection — v3 ElectionHelper parity).
        var onlineSettingsChanged =
            previousOnlineWhenOpen != election.OnlineWhenOpen
            || previousOnlineWhenClose != election.OnlineWhenClose
            || previousOnlineCloseIsEstimate != election.OnlineCloseIsEstimate
            || !string.Equals(previousOnlineSelectionProcess, election.OnlineSelectionProcess, StringComparison.Ordinal);

        if (onlineSettingsChanged)
        {
            await _signalRNotificationService.SendOnlineElectionUpdateAsync(new OnlineElectionUpdateDto
            {
                ElectionGuid = election.ElectionGuid,
                OnlineWhenOpen = election.OnlineWhenOpen,
                OnlineWhenClose = election.OnlineWhenClose,
                OnlineCloseIsEstimate = election.OnlineCloseIsEstimate,
                OnlineSelectionProcess = election.OnlineSelectionProcess
            });
        }

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

    /// <inheritdoc />
    public async Task<ElectionDto?> UpdateOnlineVotingWindowAsync(
        Guid electionGuid,
        UpdateOnlineVotingWindowDto dto)
    {
        var election = await _context.Elections.FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);
        if (election == null)
        {
            return null;
        }

        var previousOpen = election.OnlineWhenOpen;
        var previousClose = election.OnlineWhenClose;
        var previousCloseIsEstimate = election.OnlineCloseIsEstimate;

        election.OnlineWhenOpen = dto.OnlineWhenOpen;
        election.OnlineWhenClose = dto.OnlineWhenClose;
        election.OnlineCloseIsEstimate = dto.OnlineCloseIsEstimate;
        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated online voting window for election {ElectionGuid}: open={OnlineWhenOpen}, close={OnlineWhenClose}, closeIsEstimate={OnlineCloseIsEstimate}",
            electionGuid,
            election.OnlineWhenOpen,
            election.OnlineWhenClose,
            election.OnlineCloseIsEstimate);

        if (previousOpen != election.OnlineWhenOpen
            || previousClose != election.OnlineWhenClose
            || previousCloseIsEstimate != election.OnlineCloseIsEstimate)
        {
            await _signalRNotificationService.SendOnlineElectionUpdateAsync(new OnlineElectionUpdateDto
            {
                ElectionGuid = election.ElectionGuid,
                OnlineWhenOpen = election.OnlineWhenOpen,
                OnlineWhenClose = election.OnlineWhenClose,
                OnlineCloseIsEstimate = election.OnlineCloseIsEstimate,
                OnlineSelectionProcess = election.OnlineSelectionProcess
            });
        }

        return await GetElectionByGuidAsync(electionGuid);
    }

    /// <summary>
    /// Updates whether the election appears in the guest-teller join list.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="isListed">Whether guest tellers may discover and join the election.</param>
    /// <returns>True if the listing status was updated successfully, false if the election was not found.</returns>
    public async Task<bool> UpdateElectionListingAsync(Guid electionGuid, bool isListed)
    {
        var updated = await ToggleTellerAccessAsync(electionGuid, isListed);
        return updated != null;
    }

    // =====================================================================
    // Aggregate count helpers — use indexed COUNT/ANY queries instead of
    // eager-loading entire People / Location / Ballot graphs.
    // =====================================================================

    private sealed record ElectionCounts(int VoterCount, int BallotCount, int LocationCount);

    private async Task<ElectionCounts> GetElectionCountsAsync(Guid electionGuid)
    {
        var voterCount = await _context.People
            .CountAsync(p => p.ElectionGuid == electionGuid && p.CanVote == true);
        var locationCount = await _context.Locations
            .CountAsync(l => l.ElectionGuid == electionGuid);
        var ballotCount = await _context.Ballots
            .CountAsync(b => b.Location.ElectionGuid == electionGuid);

        return new ElectionCounts(voterCount, ballotCount, locationCount);
    }

    private async Task<Dictionary<Guid, int>> GetVoterCountsByElectionAsync(IReadOnlyCollection<Guid> electionGuids)
    {
        if (electionGuids.Count == 0)
        {
            return [];
        }

        return await _context.People
            .Where(p => electionGuids.Contains(p.ElectionGuid) && p.CanVote == true)
            .GroupBy(p => p.ElectionGuid)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
    }

    private async Task<Dictionary<Guid, int>> GetBallotCountsByElectionAsync(IReadOnlyCollection<Guid> electionGuids)
    {
        if (electionGuids.Count == 0)
        {
            return [];
        }

        return await _context.Ballots
            .Where(b => electionGuids.Contains(b.Location.ElectionGuid))
            .GroupBy(b => b.Location.ElectionGuid)
            .Select(g => new { g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Key, x => x.Count);
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

    private bool TryGetCurrentUserId(out Guid userId)
    {
        userId = Guid.Empty;
        var userIdString = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;
        return !string.IsNullOrEmpty(userIdString) && Guid.TryParse(userIdString, out userId);
    }

    /// <summary>
    /// Same ownership row CreateElectionAsync writes for the current user.
    /// </summary>
    private void AddCurrentUserAsAdmin(Guid electionGuid, Guid userId)
    {
        _context.JoinElectionUsers.Add(new JoinElectionUser
        {
            ElectionGuid = electionGuid,
            UserId = userId,
            Role = "Admin"
        });
    }

    internal static string ResolveDuplicateName(string? requestedName, string sourceName)
    {
        if (!string.IsNullOrWhiteSpace(requestedName))
        {
            return requestedName.Trim();
        }

        const string prefix = "Copy of ";
        var candidate = prefix + sourceName;
        return candidate.Length <= 150 ? candidate : candidate[..150];
    }

    /// <summary>
    /// Copies persisted election settings only. Identity, stage, teller-access,
    /// online window, UseOnlineVoting, envelope numbering, and ownership are set
    /// by the caller.
    /// </summary>
    private static Election CopyElectionSettings(Election source)
    {
        return new Election
        {
            Convenor = source.Convenor,
            DateOfElection = source.DateOfElection,
            ElectionType = source.ElectionType,
            ElectionMode = source.ElectionMode,
            NumberToElect = source.NumberToElect,
            NumberExtra = source.NumberExtra,
            ShowFullReport = source.ShowFullReport,
            LinkedElectionGuid = source.LinkedElectionGuid,
            LinkedElectionKind = source.LinkedElectionKind,
            ElectionPasscode = source.ElectionPasscode,
            UseCallInButton = source.UseCallInButton,
            HidePreBallotPages = source.HidePreBallotPages,
            MaskVotingMethod = source.MaskVotingMethod,
            OnlineCloseIsEstimate = source.OnlineCloseIsEstimate,
            OnlineSelectionProcess = source.OnlineSelectionProcess,
            EmailFromAddress = source.EmailFromAddress,
            EmailFromName = source.EmailFromName,
            EmailText = source.EmailText,
            SmsText = source.SmsText,
            EmailSubject = source.EmailSubject,
            CustomMethods = source.CustomMethods,
            VotingMethods = source.VotingMethods,
            Flags = source.Flags
        };
    }

    /// <summary>
    /// Copies location setup fields. Tally status and ballots-collected stay unset.
    /// </summary>
    private static Location CopyLocationForElection(Location source, Guid newElectionGuid)
    {
        return new Location
        {
            LocationGuid = Guid.NewGuid(),
            ElectionGuid = newElectionGuid,
            Name = source.Name,
            ContactInfo = source.ContactInfo,
            Long = source.Long,
            Lat = source.Lat,
            SortOrder = source.SortOrder,
            LocationTypeCode = source.LocationTypeCode
        };
    }

    /// <summary>
    /// Deletes the same runtime rows DuplicateElectionAsync does not copy:
    /// votes, ballots, results, result summaries, result ties, computers,
    /// tellers, online votes, and SMS logs.
    /// </summary>
    private async Task RemoveRuntimeRowsAsync(Guid electionGuid)
    {
        var locationGuids = await _context.Locations
            .Where(l => l.ElectionGuid == electionGuid)
            .Select(l => l.LocationGuid)
            .ToListAsync();

        var ballotGuids = await _context.Ballots
            .Where(b => locationGuids.Contains(b.LocationGuid))
            .Select(b => b.BallotGuid)
            .ToListAsync();

        _context.Votes.RemoveRange(
            await _context.Votes.Where(v => ballotGuids.Contains(v.BallotGuid)).ToListAsync());
        _context.Ballots.RemoveRange(
            await _context.Ballots.Where(b => locationGuids.Contains(b.LocationGuid)).ToListAsync());
        _context.Results.RemoveRange(
            await _context.Results.Where(r => r.ElectionGuid == electionGuid).ToListAsync());
        _context.ResultSummaries.RemoveRange(
            await _context.ResultSummaries.Where(r => r.ElectionGuid == electionGuid).ToListAsync());
        _context.ResultTies.RemoveRange(
            await _context.ResultTies.Where(r => r.ElectionGuid == electionGuid).ToListAsync());
        _context.Computers.RemoveRange(
            await _context.Computers.Where(c => c.ElectionGuid == electionGuid).ToListAsync());
        _context.Tellers.RemoveRange(
            await _context.Tellers.Where(t => t.ElectionGuid == electionGuid).ToListAsync());
        _context.OnlineVotingInfos.RemoveRange(
            await _context.OnlineVotingInfos.Where(o => o.ElectionGuid == electionGuid).ToListAsync());
        _context.SmsLogs.RemoveRange(
            await _context.SmsLogs.Where(s => s.ElectionGuid == electionGuid).ToListAsync());
    }

    /// <summary>
    /// Clears the same person runtime fields DuplicateElectionAsync does not copy.
    /// </summary>
    private void ClearPersonRuntimeFields(Guid electionGuid)
    {
        var people = _context.People.Where(p => p.ElectionGuid == electionGuid).ToList();
        foreach (var person in people)
        {
            person.RegistrationTime = null;
            person.VotingLocationGuid = null;
            person.VotingMethod = null;
            person.EnvNum = null;
            person.Teller1 = null;
            person.Teller2 = null;
            person.HasOnlineBallot = null;
            person.RegistrationHistory = null;
        }
    }

    /// <summary>
    /// Clears location tally status and ballots-collected (not copied by duplicate).
    /// </summary>
    private void ClearLocationRuntimeFields(Guid electionGuid)
    {
        var locations = _context.Locations.Where(l => l.ElectionGuid == electionGuid).ToList();
        foreach (var location in locations)
        {
            location.LocationTallyStatus = null;
            location.BallotsCollected = null;
        }
    }

    /// <summary>
    /// Copies person identity and eligibility. Check-in, voting method, envelope,
    /// teller names, online-ballot flag, and registration history are not copied.
    /// </summary>
    private static Person CopyPersonForElection(Person source, Guid newElectionGuid)
    {
        return new Person
        {
            PersonGuid = Guid.NewGuid(),
            ElectionGuid = newElectionGuid,
            LastName = source.LastName,
            FirstName = source.FirstName,
            OtherLastNames = source.OtherLastNames,
            OtherNames = source.OtherNames,
            OtherInfo = source.OtherInfo,
            Area = source.Area,
            BahaiId = source.BahaiId,
            CombinedInfo = source.CombinedInfo,
            CombinedSoundCodes = source.CombinedSoundCodes,
            CombinedInfoAtStart = source.CombinedInfoAtStart,
            CanVote = source.CanVote,
            CanReceiveVotes = source.CanReceiveVotes,
            IneligibleReasonCode = source.IneligibleReasonCode,
            Email = source.Email,
            Phone = source.Phone,
            Flags = source.Flags,
            UnitName = source.UnitName,
            KioskCode = source.KioskCode,
            RowVersion = new byte[8]
        };
    }
}



