using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TallyJ4.DTOs.Elections;
using TallyJ4.DTOs.Setup;
using TallyJ4.EF.Context;
using TallyJ4.EF.Models;

namespace TallyJ4.Services;

public class SetupService : ISetupService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<SetupService> _logger;

    public SetupService(MainDbContext context, IMapper mapper, ILogger<SetupService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<ElectionDto> CreateElectionStep1Async(ElectionStep1Dto step1Dto)
    {
        var election = new Election
        {
            ElectionGuid = Guid.NewGuid(),
            Name = step1Dto.Name,
            DateOfElection = step1Dto.DateOfElection,
            TallyStatus = "Setup",
            RowVersion = new byte[8],
            NumberToElect = 1,
            ElectionType = "STV",
            ElectionMode = "N"
        };

        _context.Elections.Add(election);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created election (Step 1) {ElectionGuid} - {Name}", election.ElectionGuid, election.Name);

        var electionDto = _mapper.Map<ElectionDto>(election);
        electionDto.VoterCount = 0;
        electionDto.BallotCount = 0;
        electionDto.LocationCount = 0;

        return electionDto;
    }

    public async Task<ElectionDto?> ConfigureElectionStep2Async(Guid electionGuid, ElectionStep2Dto step2Dto)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return null;
        }

        election.NumberToElect = step2Dto.NumberToElect;
        election.ElectionType = step2Dto.ElectionType;
        election.ElectionMode = step2Dto.ElectionMode;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Configured election (Step 2) {ElectionGuid}", electionGuid);

        var electionDto = _mapper.Map<ElectionDto>(election);
        electionDto.VoterCount = 0;
        electionDto.BallotCount = 0;
        electionDto.LocationCount = 0;

        return electionDto;
    }

    public async Task<ElectionSetupStatusDto?> GetSetupStatusAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return null;
        }

        var step1Complete = !string.IsNullOrWhiteSpace(election.Name);
        var step2Complete = election.NumberToElect.HasValue && 
                           election.NumberToElect > 0 &&
                           !string.IsNullOrWhiteSpace(election.ElectionType) &&
                           !string.IsNullOrWhiteSpace(election.ElectionMode);

        var progressPercent = 0;
        if (step1Complete) progressPercent += 50;
        if (step2Complete) progressPercent += 50;

        return new ElectionSetupStatusDto
        {
            ElectionGuid = election.ElectionGuid,
            Name = election.Name,
            TallyStatus = election.TallyStatus ?? "Setup",
            Step1Complete = step1Complete,
            Step2Complete = step2Complete,
            ProgressPercent = progressPercent
        };
    }
}
