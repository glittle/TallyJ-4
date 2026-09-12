using System.Text.Json;
using Backend.Context;
using Backend.Entities;
using Backend.DTOs.FrontDesk;
using Backend.DTOs.SignalR;
using Backend.Enumerations;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service implementation for managing front desk operations including voter check-in and roll call.
/// </summary>
public class FrontDeskService : IFrontDeskService
{
    private readonly MainDbContext _context;
    private readonly ILogger<FrontDeskService> _logger;
    private readonly ISignalRNotificationService _signalRNotificationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrontDeskService"/> class.
    /// </summary>
    public FrontDeskService(
        MainDbContext context,
        ILogger<FrontDeskService> logger,
        ISignalRNotificationService signalRNotificationService)
    {
        _context = context;
        _logger = logger;
        _signalRNotificationService = signalRNotificationService;
    }

    /// <inheritdoc />
    public async Task<List<FrontDeskVoterDto>> GetEligibleVotersAsync(Guid electionGuid)
    {
        var voters = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true)
            .ToListAsync();

        var onlineStatusByPerson = await LoadLatestOnlineStatusByPersonAsync(electionGuid);

        return voters.Select(person =>
            MapToFrontDeskVoterDto(
                person,
                onlineStatusByPerson.GetValueOrDefault(person.PersonGuid)))
            .ToList();
    }

    /// <inheritdoc />
    public async Task<FrontDeskVoterDto> CheckInVoterAsync(Guid electionGuid, CheckInVoterDto checkInDto)
    {
        await ElectionFinalizedWriteGuard.ThrowIfLockedAsync(_context, electionGuid);

        var person = await _context.People
            .FirstOrDefaultAsync(p => p.PersonGuid == checkInDto.PersonGuid && p.ElectionGuid == electionGuid);

        if (person == null)
        {
            throw new InvalidOperationException("Person not found");
        }

        if (!person.CanVote.GetValueOrDefault())
        {
            throw new InvalidOperationException("Person is not eligible to vote");
        }

        if (person.RegistrationTime.HasValue)
        {
            throw new InvalidOperationException("Person has already checked in");
        }

        var onlineInfo = await LoadLatestOnlineVotingInfoAsync(electionGuid, person.PersonGuid);
        if (VotingMethodCodes.IsOnline(checkInDto.VotingMethod))
        {
            throw new InvalidOperationException(FrontDeskMessageKeys.OnlineIsVoterInitiated);
        }

        if (onlineInfo != null && OnlineBallotStatus.IsProcessed(onlineInfo.Status))
        {
            throw new InvalidOperationException(FrontDeskMessageKeys.AlreadyAcceptedOnline);
        }

        if (onlineInfo != null && OnlineBallotStatus.IsProcessing(onlineInfo.Status))
        {
            throw new InvalidOperationException(FrontDeskMessageKeys.AlreadyProcessingOnline);
        }

        person.RegistrationTime = DateTimeOffset.UtcNow;
        person.VotingMethod = checkInDto.VotingMethod;
        person.VotingLocationGuid = checkInDto.VotingLocationGuid;

        person.Teller1 = NormalizeTellerName(checkInDto.Teller1);
        person.Teller2 = NormalizeTellerName(checkInDto.Teller2);

        await AddRegistrationHistoryEntry(person, "CheckedIn", person.Teller1, person.Teller2);

        if (VotingMethodCodes.IsRecordedOtherThanOnline(checkInDto.VotingMethod)
            && onlineInfo != null
            && OnlineBallotStatus.IsEditable(onlineInfo.Status))
        {
            WithdrawPendingOnlineBallot(person, onlineInfo);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Voter {PersonGuid} checked in for election {ElectionGuid} with envelope {EnvNum}",
            person.PersonGuid, electionGuid, person.EnvNum);

        var voterDto = MapToFrontDeskVoterDto(person, person.HasOnlineBallot == true
            ? onlineInfo?.Status
            : null);

        await _signalRNotificationService.NotifyPersonCheckedInAsync(electionGuid, voterDto);
        await NotifyVoterPersonalRegistrationAsync(person);

        var stats = await GetStatsAsync(electionGuid);
        await _signalRNotificationService.NotifyVoterCountUpdatedAsync(electionGuid, stats);

        return voterDto;
    }

    /// <inheritdoc />
    public async Task<RollCallDto> GetRollCallAsync(Guid electionGuid)
    {
        var voters = await GetEligibleVotersAsync(electionGuid);
        var stats = await GetStatsAsync(electionGuid);

        return new RollCallDto
        {
            Voters = voters,
            Stats = stats
        };
    }

    /// <inheritdoc />
    public async Task<FrontDeskStatsDto> GetStatsAsync(Guid electionGuid)
    {
        var totalEligible = await _context.People
            .CountAsync(p => p.ElectionGuid == electionGuid && p.CanVote == true);

        var checkedIn = await _context.People
            .CountAsync(p => p.ElectionGuid == electionGuid && p.CanVote == true && p.RegistrationTime.HasValue);

        return new FrontDeskStatsDto
        {
            TotalEligible = totalEligible,
            CheckedIn = checkedIn,
            NotYetCheckedIn = totalEligible - checkedIn
        };
    }

    /// <inheritdoc />
    public async Task<FrontDeskVoterDto> UnregisterVoterAsync(Guid electionGuid, UnregisterVoterDto unregisterDto)
    {
        await ElectionFinalizedWriteGuard.ThrowIfLockedAsync(_context, electionGuid);

        var person = await _context.People
            .FirstOrDefaultAsync(p => p.PersonGuid == unregisterDto.PersonGuid && p.ElectionGuid == electionGuid);

        if (person == null)
        {
            throw new InvalidOperationException("Person not found");
        }

        if (!person.RegistrationTime.HasValue)
        {
            throw new InvalidOperationException("Person is not currently checked in");
        }

        // Store the envelope number and tellers for history
        var envNum = person.EnvNum;
        var teller1 = person.Teller1;
        var teller2 = person.Teller2;

        // Clear check-in data
        person.RegistrationTime = null;
        person.VotingMethod = null;
        person.VotingLocationGuid = null;
        person.EnvNum = null;
        person.Teller1 = null;
        person.Teller2 = null;

        await AddRegistrationHistoryEntry(
            person,
            "Unregistered",
            teller1,
            teller2,
            unregisterDto.Reason);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Voter {PersonGuid} unregistered from election {ElectionGuid}. Former envelope: {EnvNum}",
            person.PersonGuid, electionGuid, envNum);

        var remainingOnline = await LoadLatestOnlineVotingInfoAsync(electionGuid, person.PersonGuid);
        var voterDto = MapToFrontDeskVoterDto(person, remainingOnline?.Status);

        await _signalRNotificationService.NotifyPersonCheckedInAsync(electionGuid, voterDto);
        await NotifyVoterPersonalRegistrationAsync(person);

        var stats = await GetStatsAsync(electionGuid);
        await _signalRNotificationService.NotifyVoterCountUpdatedAsync(electionGuid, stats);

        return voterDto;
    }

    /// <summary>
    /// Adds a registration history entry to the person's history log.
    /// </summary>
    private async Task AddRegistrationHistoryEntry(
        Person person,
        string action,
        string? teller1,
        string? teller2,
        string? performedBy = null)
    {
        var historyEntries = string.IsNullOrEmpty(person.RegistrationHistory)
            ? new List<RegistrationHistoryEntryDto>()
            : JsonSerializer.Deserialize<List<RegistrationHistoryEntryDto>>(person.RegistrationHistory) ?? new List<RegistrationHistoryEntryDto>();

        var locationName = person.VotingLocationGuid.HasValue
            ? await _context.Locations
                .Where(l => l.LocationGuid == person.VotingLocationGuid)
                .Select(l => l.Name)
                .FirstOrDefaultAsync()
            : null;

        var entry = new RegistrationHistoryEntryDto
        {
            Timestamp = DateTimeOffset.UtcNow,
            Action = action,
            VotingMethod = person.VotingMethod,
            Teller1 = teller1,
            Teller2 = teller2,
            LocationName = locationName,
            EnvNum = person.EnvNum,
            PerformedBy = performedBy
        };

        historyEntries.Add(entry);
        person.RegistrationHistory = JsonSerializer.Serialize(historyEntries);
    }

    /// <inheritdoc />
    public async Task<FrontDeskVoterDto> UpdatePersonFlagsAsync(Guid electionGuid, UpdatePersonFlagsDto updateFlagsDto)
    {
        await ElectionFinalizedWriteGuard.ThrowIfLockedAsync(_context, electionGuid);

        var person = await _context.People
            .FirstOrDefaultAsync(p => p.PersonGuid == updateFlagsDto.PersonGuid && p.ElectionGuid == electionGuid);

        if (person == null)
        {
            throw new InvalidOperationException("Person not found");
        }

        person.Flags = updateFlagsDto.Flags;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated flags for person {PersonGuid} in election {ElectionGuid}",
            person.PersonGuid, electionGuid);

        var onlineInfo = await LoadLatestOnlineVotingInfoAsync(electionGuid, person.PersonGuid);
        var voterDto = MapToFrontDeskVoterDto(person, onlineInfo?.Status);

        await _signalRNotificationService.SendPersonFlagsUpdatedAsync(electionGuid, voterDto);

        return voterDto;
    }

    /// <inheritdoc />
    public async Task<FrontDeskVoterDto> UpdateEnvelopeNumberAsync(
        Guid electionGuid,
        UpdateEnvelopeNumberDto updateDto)
    {
        await ElectionFinalizedWriteGuard.ThrowIfLockedAsync(_context, electionGuid);

        var person = await _context.People
            .FirstOrDefaultAsync(p =>
                p.PersonGuid == updateDto.PersonGuid && p.ElectionGuid == electionGuid);

        if (person == null)
        {
            throw new InvalidOperationException("Person not found");
        }

        if (updateDto.EnvNum.HasValue)
        {
            if (updateDto.EnvNum.Value <= 0)
            {
                throw new InvalidOperationException("Envelope number must be greater than zero");
            }

            var envelopeInUse = await _context.People.AnyAsync(p =>
                p.ElectionGuid == electionGuid &&
                p.EnvNum == updateDto.EnvNum.Value &&
                p.PersonGuid != updateDto.PersonGuid);

            if (envelopeInUse)
            {
                throw new InvalidOperationException("Envelope number is already in use");
            }

            person.EnvNum = updateDto.EnvNum.Value;
        }
        else
        {
            person.EnvNum = null;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Updated envelope number for person {PersonGuid} in election {ElectionGuid} to {EnvNum}",
            person.PersonGuid,
            electionGuid,
            person.EnvNum);

        var onlineInfo = await LoadLatestOnlineVotingInfoAsync(electionGuid, person.PersonGuid);
        var voterDto = MapToFrontDeskVoterDto(person, onlineInfo?.Status);
        await _signalRNotificationService.NotifyPersonCheckedInAsync(electionGuid, voterDto);
        await NotifyVoterPersonalRegistrationAsync(person);

        return voterDto;
    }

    /// <summary>
    /// Thin personal push so a connected online voter re-fetches status when front desk
    /// changes their registration (v3 VoterPersonalHub parity).
    /// </summary>
    private Task NotifyVoterPersonalRegistrationAsync(Person person)
    {
        return _signalRNotificationService.NotifyVoterPersonalUpdateAsync(
            person.Email,
            person.Phone,
            person.KioskCode,
            new VoterPersonalUpdateDto
            {
                UpdateRegistration = true,
                ElectionGuid = person.ElectionGuid,
                VotingMethod = person.VotingMethod,
                RegistrationTime = person.RegistrationTime
            });
    }

    private static string? NormalizeTellerName(string? tellerName)
    {
        return string.IsNullOrWhiteSpace(tellerName) ? null : tellerName.Trim();
    }

    private async Task<Dictionary<Guid, string>> LoadLatestOnlineStatusByPersonAsync(Guid electionGuid)
    {
        var rows = await _context.OnlineVotingInfos
            .AsNoTracking()
            .Where(o => o.ElectionGuid == electionGuid)
            .Select(o => new { o.PersonGuid, o.Status, o.WhenStatus, o.WhenBallotCreated })
            .ToListAsync();

        return rows
            .GroupBy(o => o.PersonGuid)
            .ToDictionary(
                g => g.Key,
                g => g
                    .OrderByDescending(o => o.WhenStatus ?? o.WhenBallotCreated)
                    .First()
                    .Status);
    }

    private async Task<OnlineVotingInfo?> LoadLatestOnlineVotingInfoAsync(Guid electionGuid, Guid personGuid)
    {
        return await _context.OnlineVotingInfos
            .Where(o => o.ElectionGuid == electionGuid && o.PersonGuid == personGuid)
            .OrderByDescending(o => o.WhenStatus ?? o.WhenBallotCreated)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Discard a Draft or Submitted online row after Front Desk records a
    /// different method. Processed / Processing rows are never withdrawn.
    /// </summary>
    private void WithdrawPendingOnlineBallot(Person person, OnlineVotingInfo onlineInfo)
    {
        _context.OnlineVotingInfos.Remove(onlineInfo);
        person.HasOnlineBallot = false;
        _logger.LogInformation(
            "Withdrew pending {Status} online ballot for person {PersonGuid} after Front Desk method {VotingMethod}",
            onlineInfo.Status,
            person.PersonGuid,
            person.VotingMethod);
    }

    // Explicit mapping for FrontDeskVoterDto (replaces logic that was in Mapster profiles).
    // Handles the JSON deserialization of RegistrationHistory with good error context.
    private static FrontDeskVoterDto MapToFrontDeskVoterDto(Person person, string? onlineBallotStatus)
    {
        var dto = person.CopyMatchingPropertiesToNew<FrontDeskVoterDto>();

        dto.RegistrationHistory = DeserializeRegistrationHistory(person.RegistrationHistory, person.PersonGuid);
        dto.OnlineBallotStatus = onlineBallotStatus;

        return dto;
    }

    private static List<RegistrationHistoryEntryDto>? DeserializeRegistrationHistory(string? json, Guid personGuid)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<RegistrationHistoryEntryDto>>(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize RegistrationHistory for Person {personGuid}: {ex.Message}. JSON: {json}", ex);
        }
    }
}



