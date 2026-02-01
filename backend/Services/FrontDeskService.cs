using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TallyJ4.DTOs.FrontDesk;
using TallyJ4.DTOs.SignalR;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Entities;

namespace TallyJ4.Services;

public class FrontDeskService : IFrontDeskService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<FrontDeskService> _logger;
    private readonly ISignalRNotificationService _signalRNotificationService;

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

    public async Task<List<FrontDeskVoterDto>> GetEligibleVotersAsync(Guid electionGuid)
    {
        var voters = await _context.People
            .Where(p => p.ElectionGuid == electionGuid && p.CanVote == true)
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .ToListAsync();

        return _mapper.Map<List<FrontDeskVoterDto>>(voters);
    }

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

        await _context.SaveChangesAsync();

        _logger.LogInformation("Voter {PersonGuid} checked in for election {ElectionGuid} with envelope {EnvNum}", 
            person.PersonGuid, electionGuid, person.EnvNum);

        var voterDto = _mapper.Map<FrontDeskVoterDto>(person);

        await _signalRNotificationService.NotifyPersonCheckedInAsync(electionGuid, voterDto);

        var stats = await GetStatsAsync(electionGuid);
        await _signalRNotificationService.NotifyVoterCountUpdatedAsync(electionGuid, stats);

        return voterDto;
    }

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
}
