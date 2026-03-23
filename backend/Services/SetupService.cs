using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.DTOs.Elections;
using Backend.DTOs.Setup;
using Mapster;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service for managing election setup operations including multi-step election configuration.
/// Provides functionality to create and configure elections through a step-by-step process.
/// </summary>
public class SetupService : ISetupService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<SetupService> _logger;

    /// <summary>
    /// Initializes a new instance of the SetupService.
    /// </summary>
    /// <param name="context">The main database context for accessing election data.</param>
    /// <param name="mapper">Mapster instance for object mapping operations.</param>
    /// <param name="logger">Logger for recording setup service operations.</param>
    public SetupService(MainDbContext context, IMapper mapper, ILogger<SetupService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new election with basic information (Step 1 of setup process).
    /// </summary>
    /// <param name="step1Dto">The data transfer object containing basic election information.</param>
    /// <returns>An ElectionDto representing the newly created election.</returns>
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
            ElectionType = ElectionTypeEnum.LSA.Code,
            ElectionMode = ElectionModeEnum.Normal.Code
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

    /// <summary>
    /// Configures election parameters (Step 2 of setup process).
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to configure.</param>
    /// <param name="step2Dto">The data transfer object containing election configuration parameters.</param>
    /// <returns>An ElectionDto representing the configured election, or null if the election was not found.</returns>
    public async Task<ElectionDto?> ConfigureElectionStep2Async(Guid electionGuid, ElectionStep2Dto step2Dto)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return null;
        }

        election.NumberToElect = step2Dto.NumberToElect;
        election.ElectionType = step2Dto.ElectionType.ToString();
        election.ElectionMode = step2Dto.ElectionMode.ToString();

        await _context.SaveChangesAsync();

        _logger.LogInformation("Configured election (Step 2) {ElectionGuid}", electionGuid);

        var electionDto = _mapper.Map<ElectionDto>(election);
        electionDto.VoterCount = 0;
        electionDto.BallotCount = 0;
        electionDto.LocationCount = 0;

        return electionDto;
    }

    /// <summary>
    /// Retrieves the current setup status and progress for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>An ElectionSetupStatusDto containing setup progress information, or null if the election was not found.</returns>
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



