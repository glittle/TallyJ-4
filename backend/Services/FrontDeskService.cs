using System.Text.Json;
using Backend.Context;
using Backend.Entities;
using Backend.DTOs.FrontDesk;
using Backend.DTOs.People;
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
        var phoneRows = await OnlineVoterPhoneHelper.FindPhoneOnlineVotersAsync(
            _context,
            voters.Select(p => p.Phone));

        return voters.Select(person =>
            MapToFrontDeskVoterDto(
                person,
                onlineStatusByPerson.TryGetValue(person.PersonGuid, out var status)
                    ? status
                    : null,
                OnlineVoterPhoneHelper.ToListHint(person.Phone, phoneRows)))
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

        if (VotingMethodCodes.IsOnline(checkInDto.VotingMethod))
        {
            throw new InvalidOperationException(FrontDeskMessageKeys.OnlineIsVoterInitiated);
        }

        var onlineInfo = await LoadLatestOnlineVotingInfoAsync(electionGuid, person.PersonGuid);
        await ThrowIfOnlineBlocksCheckInAsync(onlineInfo);

        if (_context.Database.IsRelational())
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await ApplyCheckInAndWithdrawAsync(person, checkInDto, onlineInfo);
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        else
        {
            await ApplyCheckInAndWithdrawAsync(person, checkInDto, onlineInfo);
        }

        _logger.LogInformation("Voter {PersonGuid} checked in for election {ElectionGuid} with envelope {EnvNum}",
            person.PersonGuid, electionGuid, person.EnvNum);

        var voterDto = MapToFrontDeskVoterDto(
            person,
            person.HasOnlineBallot == true
                ? onlineInfo?.Status
                : null,
            await LoadPhoneSmsHintAsync(person.Phone));

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
        var people = await _context.People
            .AsNoTracking()
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true)
            .Select(p => new { p.PersonGuid, p.VotingMethod })
            .ToListAsync();

        var processedOnline = await _context.OnlineVotingInfos
            .AsNoTracking()
            .Where(o => o.ElectionGuid == electionGuid && o.Status == OnlineBallotStatus.Processed)
            .Select(o => o.PersonGuid)
            .ToListAsync();
        var processedSet = processedOnline.ToHashSet();

        var totalEligible = people.Count;
        var checkedIn = people.Count(p =>
            VotingMethodCodes.HasVotedForCounts(
                p.VotingMethod,
                processedSet.Contains(p.PersonGuid)));

        return new FrontDeskStatsDto
        {
            TotalEligible = totalEligible,
            CheckedIn = checkedIn,
            NotYetCheckedIn = totalEligible - checkedIn
        };
    }

    /// <inheritdoc />
    /// Desk-only: requires <see cref="Person.RegistrationTime"/>. Does not
    /// restore an online row that check-in already withdrew.
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
        var voterDto = MapToFrontDeskVoterDto(
            person,
            remainingOnline?.Status,
            await LoadPhoneSmsHintAsync(person.Phone));

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
        var voterDto = MapToFrontDeskVoterDto(
            person,
            onlineInfo?.Status,
            await LoadPhoneSmsHintAsync(person.Phone));

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
        var voterDto = MapToFrontDeskVoterDto(
            person,
            onlineInfo?.Status,
            await LoadPhoneSmsHintAsync(person.Phone));
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
        var kioskVoterId = KioskCodeLifetime.HasLiveCode(person.KioskCode)
            ? KioskCodeLifetime.ToVoterId(person.ElectionGuid, person.KioskCode!)
            : null;

        return _signalRNotificationService.NotifyVoterPersonalUpdateAsync(
            person.Email,
            person.Phone,
            kioskVoterId,
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
    /// Processing / Processed must refuse even when this context still tracks
    /// a stale Submitted row (Accept-all claimed or finished on another
    /// context). Relational reads Status from the database, not the tracker.
    /// </summary>
    private async Task ThrowIfOnlineBlocksCheckInAsync(OnlineVotingInfo? onlineInfo)
    {
        if (onlineInfo == null)
        {
            return;
        }

        var status = onlineInfo.Status;
        if (_context.Database.IsRelational())
        {
            var dbStatus = await _context.OnlineVotingInfos
                .AsNoTracking()
                .Where(o => o.RowId == onlineInfo.RowId)
                .Select(o => o.Status)
                .FirstOrDefaultAsync();
            if (dbStatus == null)
            {
                return;
            }

            status = dbStatus;
        }

        if (OnlineBallotStatus.IsProcessed(status))
        {
            throw new InvalidOperationException(FrontDeskMessageKeys.AlreadyAcceptedOnline);
        }

        if (OnlineBallotStatus.IsProcessing(status))
        {
            throw new InvalidOperationException(FrontDeskMessageKeys.AlreadyProcessingOnline);
        }
    }

    private async Task ApplyCheckInAndWithdrawAsync(
        Person person,
        CheckInVoterDto checkInDto,
        OnlineVotingInfo? onlineInfo)
    {
        person.RegistrationTime = DateTimeOffset.UtcNow;
        person.VotingMethod = checkInDto.VotingMethod;
        person.VotingLocationGuid = checkInDto.VotingLocationGuid;

        person.Teller1 = NormalizeTellerName(checkInDto.Teller1);
        person.Teller2 = NormalizeTellerName(checkInDto.Teller2);

        await AddRegistrationHistoryEntry(person, "CheckedIn", person.Teller1, person.Teller2);

        if (VotingMethodCodes.IsRecordedOtherThanOnline(checkInDto.VotingMethod)
            && onlineInfo != null)
        {
            await WithdrawPendingOnlineIfStillEditableAsync(person, onlineInfo);
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Discard a Draft or Submitted online row after Front Desk records a
    /// different method. The row is removed; Unregister and later method
    /// changes do not restore it. Processed / Processing rows are never withdrawn.
    /// Relational delete is a compare-and-swap on Status so a concurrent
    /// Accept-all claim cannot be removed while we also write a desk method.
    /// </summary>
    private async Task WithdrawPendingOnlineIfStillEditableAsync(
        Person person,
        OnlineVotingInfo onlineInfo)
    {
        if (_context.Database.IsRelational())
        {
            var deleted = await _context.OnlineVotingInfos
                .Where(o => o.RowId == onlineInfo.RowId
                            && (o.Status == OnlineBallotStatus.Draft
                                || o.Status == OnlineBallotStatus.Submitted))
                .ExecuteDeleteAsync();
            _context.Entry(onlineInfo).State = EntityState.Detached;

            if (deleted == 0)
            {
                await ThrowIfOnlineBlocksCheckInAsync(onlineInfo);
                person.HasOnlineBallot = false;
                return;
            }

            person.HasOnlineBallot = false;
            _logger.LogInformation(
                "Withdrew pending online ballot for person {PersonGuid}",
                person.PersonGuid);
            return;
        }

        if (!OnlineBallotStatus.IsEditable(onlineInfo.Status))
        {
            await ThrowIfOnlineBlocksCheckInAsync(onlineInfo);
            return;
        }

        WithdrawPendingOnlineBallot(person, onlineInfo);
    }

    /// <summary>
    /// Discard a Draft or Submitted online row after Front Desk records a
    /// different method. In-memory tests use this path; relational check-in
    /// deletes with a Status filter instead.
    /// </summary>
    private void WithdrawPendingOnlineBallot(Person person, OnlineVotingInfo onlineInfo)
    {
        _context.OnlineVotingInfos.Remove(onlineInfo);
        person.HasOnlineBallot = false;
        _logger.LogInformation(
            "Withdrew pending {Status} online ballot for person {PersonGuid}",
            OnlineBallotStatus.IsDraft(onlineInfo.Status)
                ? OnlineBallotStatus.Draft
                : OnlineBallotStatus.Submitted,
            person.PersonGuid);
    }

    private async Task<PersonPhoneSmsHintDto?> LoadPhoneSmsHintAsync(string? phone)
    {
        return OnlineVoterPhoneHelper.ToListHint(
            phone,
            await OnlineVoterPhoneHelper.FindPhoneOnlineVoterAsync(_context, phone));
    }

    // Explicit mapping for FrontDeskVoterDto (replaces logic that was in Mapster profiles).
    // Handles the JSON deserialization of RegistrationHistory with good error context.
    private static FrontDeskVoterDto MapToFrontDeskVoterDto(
        Person person,
        string? onlineBallotStatus,
        PersonPhoneSmsHintDto? phoneOnlineVoter)
    {
        var dto = person.CopyMatchingPropertiesToNew<FrontDeskVoterDto>();

        dto.RegistrationHistory = DeserializeRegistrationHistory(person.RegistrationHistory, person.PersonGuid);
        dto.OnlineBallotStatus = onlineBallotStatus;
        dto.PhoneOnlineVoter = phoneOnlineVoter;

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



