using Microsoft.EntityFrameworkCore;
using TallyJ4.DTOs.Public;
using TallyJ4.EF.Context;

namespace TallyJ4.Services;

public class PublicService : IPublicService
{
    private readonly MainDbContext _context;
    private readonly ILogger<PublicService> _logger;

    public PublicService(MainDbContext context, ILogger<PublicService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<PublicHomeDto> GetPublicHomeDataAsync()
    {
        var availableElectionsCount = await _context.Elections
            .Where(e => !string.IsNullOrEmpty(e.ElectionPasscode))
            .CountAsync();

        _logger.LogInformation("Public home data requested. Available elections: {Count}", availableElectionsCount);

        return new PublicHomeDto
        {
            ApplicationName = "TallyJ 4",
            Version = "4.0.0",
            Description = "Election management and online voting system",
            AvailableElectionsCount = availableElectionsCount,
            ServerTime = DateTime.UtcNow
        };
    }

    public async Task<List<AvailableElectionDto>> GetAvailableElectionsAsync()
    {
        var elections = await _context.Elections
            .Where(e => !string.IsNullOrEmpty(e.ElectionPasscode))
            .OrderByDescending(e => e.DateOfElection ?? DateTime.MinValue)
            .Select(e => new AvailableElectionDto
            {
                ElectionGuid = e.ElectionGuid,
                Name = e.Name,
                DateOfElection = e.DateOfElection,
                ElectionType = e.ElectionType ?? "Unknown"
            })
            .ToListAsync();

        _logger.LogInformation("Retrieved {Count} available elections", elections.Count);

        return elections;
    }

    public async Task<ElectionStatusDto?> GetElectionStatusAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .Include(e => e.People)
            .Include(e => e.Locations)
                .ThenInclude(l => l.Ballots)
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            _logger.LogWarning("Election {ElectionGuid} not found", electionGuid);
            return null;
        }

        var voterCount = election.People.Count(p => p.CanVote == true);
        var ballots = election.Locations.SelectMany(l => l.Ballots).ToList();
        var ballotCount = ballots.Count;

        var isActive = election.TallyStatus != "Complete" && 
                      election.TallyStatus != "Archived" &&
                      election.TallyStatus != "Deleted";

        _logger.LogInformation("Election status for {ElectionGuid}: {Status}", electionGuid, election.TallyStatus);

        return new ElectionStatusDto
        {
            ElectionGuid = election.ElectionGuid,
            Name = election.Name,
            DateOfElection = election.DateOfElection,
            ElectionType = election.ElectionType ?? "Unknown",
            TallyStatus = election.TallyStatus ?? "Unknown",
            IsActive = isActive,
            RegisteredVoters = voterCount,
            BallotsSubmitted = ballotCount
        };
    }
}
