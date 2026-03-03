using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Backend.DTOs.FrontDesk;
using Backend.DTOs.SignalR;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using System.Text.Json;

namespace Backend.Services;

/// <summary>
/// Service implementation for managing front desk operations including voter check-in and roll call.
/// </summary>
public class FrontDeskService : IFrontDeskService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<FrontDeskService> _logger;
    private readonly ISignalRNotificationService _signalRNotificationService;

    /// <summary>
    /// Initializes a new instance of the <see cref="FrontDeskService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="signalRNotificationService">The SignalR notification service.</param>
    public FrontDeskService(
        MainDbContext context,
        IMapper mapper,
        ILogger<FrontDeskService> logger,
        ISignalRNotificationService signalRNotificationService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _signalRNotificationService = signalRNotificationService;
    }

    /// <inheritdoc />
    public async Task<List<FrontDeskVoterDto>> GetEligibleVotersAsync(Guid electionGuid)
    {
        var voters = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true)
            // .OrderBy(p => p.LastName)
            // .ThenBy(p => p.FirstName)
            .ToListAsync();

        return _mapper.Map<List<FrontDeskVoterDto>>(voters);
    }

    /// <inheritdoc />
    public async Task<FrontDeskVoterDto> CheckInVoterAsync(Guid electionGuid, CheckInVoterDto checkInDto)
    {
        var person = await _context.People
            .FirstOrDefaultAsync(p => p.PersonGuid == checkInDto.PersonGuid && p.ElectionGuid == electionGuid);

        if (person == null)
        {
            throw new InvalidOperationException("Person not found");
        }

        if (person.CanVote != true)
        {
            throw new InvalidOperationException("Person is not eligible to vote");
        }

        if (person.RegistrationTime.HasValue)
        {
            throw new InvalidOperationException("Person has already checked in");
        }

        person.RegistrationTime = DateTime.UtcNow;
        person.VotingMethod = checkInDto.VotingMethod;
        person.VotingLocationGuid = checkInDto.VotingLocationGuid;

        if (!string.IsNullOrWhiteSpace(checkInDto.TellerName))
        {
            if (string.IsNullOrWhiteSpace(person.Teller1))
            {
                person.Teller1 = checkInDto.TellerName;
            }
            else
            {
                person.Teller2 = checkInDto.TellerName;
            }
        }

        var nextEnvNum = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.EnvNum.HasValue)
            .MaxAsync(p => (int?)p.EnvNum) ?? 0;
        person.EnvNum = nextEnvNum + 1;

        // Add history entry
        await AddRegistrationHistoryEntry(person, "CheckedIn", checkInDto.TellerName);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Voter {PersonGuid} checked in for election {ElectionGuid} with envelope {EnvNum}",
            person.PersonGuid, electionGuid, person.EnvNum);

        var voterDto = _mapper.Map<FrontDeskVoterDto>(person);

        await _signalRNotificationService.NotifyPersonCheckedInAsync(electionGuid, voterDto);

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

        // Store the envelope number for history
        var envNum = person.EnvNum;

        // Clear check-in data
        person.RegistrationTime = null;
        person.VotingMethod = null;
        person.VotingLocationGuid = null;
        person.EnvNum = null;
        person.Teller1 = null;
        person.Teller2 = null;

        // Add history entry with reason
        await AddRegistrationHistoryEntry(person, "Unregistered", null, unregisterDto.Reason);

        await _context.SaveChangesAsync();

        _logger.LogInformation("Voter {PersonGuid} unregistered from election {ElectionGuid}. Former envelope: {EnvNum}",
            person.PersonGuid, electionGuid, envNum);

        var voterDto = _mapper.Map<FrontDeskVoterDto>(person);

        await _signalRNotificationService.NotifyPersonCheckedInAsync(electionGuid, voterDto);

        var stats = await GetStatsAsync(electionGuid);
        await _signalRNotificationService.NotifyVoterCountUpdatedAsync(electionGuid, stats);

        return voterDto;
    }

    /// <summary>
    /// Adds a registration history entry to the person's history log.
    /// </summary>
    private async Task AddRegistrationHistoryEntry(Person person, string action, string? tellerName, string? reason = null)
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
            Timestamp = DateTime.UtcNow,
            Action = action,
            VotingMethod = person.VotingMethod,
            TellerName = tellerName,
            LocationName = locationName,
            EnvNum = person.EnvNum,
            PerformedBy = reason ?? tellerName // For unregister, reason describes why; for checkin, teller name
        };

        historyEntries.Add(entry);
        person.RegistrationHistory = JsonSerializer.Serialize(historyEntries);
    }

    /// <inheritdoc />
    public async Task<FrontDeskVoterDto> UpdatePersonFlagsAsync(Guid electionGuid, UpdatePersonFlagsDto updateFlagsDto)
    {
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

        var voterDto = _mapper.Map<FrontDeskVoterDto>(person);

        // Send SignalR notification to update all connected clients
        await _signalRNotificationService.SendPersonFlagsUpdatedAsync(electionGuid, voterDto);

        return voterDto;
    }
}



