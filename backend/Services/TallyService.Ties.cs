using Backend.DTOs.Results;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class TallyService
{
    /// <summary>
    /// Retrieves tie-breaking information for a specific tie group in an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="tieBreakGroup">The tie break group number.</param>
    /// <returns>A TieDetailsDto containing information about the tie situation.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<TieDetailsDto> GetTiesAsync(Guid electionGuid, int tieBreakGroup)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        var tieResults = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid && r.TieBreakGroup == tieBreakGroup && r.IsTied == true)
            .ToListAsync();

        if (!tieResults.Any())
        {
            throw new ArgumentException($"Tie break group {tieBreakGroup} not found in election {electionGuid}");
        }

        var section = tieResults[0].Section ?? SectionOther;

        var people = tieResults.Select(r => new TiePersonDto
        {
            PersonGuid = r.PersonGuid,
            FullName = r.Person?.FullNameFl ?? UnknownFallbackValue,
            VoteCount = r.VoteCount ?? 0,
            TieBreakCount = r.TieBreakCount
        }).ToList();

        return new TieDetailsDto
        {
            TieBreakGroup = tieBreakGroup,
            Section = section,
            People = people
        };
    }

    /// <summary>
    /// Saves tie-breaking vote counts for an election and re-analyzes when any count is persisted.
    /// An explicit 0 is a valid runoff result; omit a person to leave their count unset (null).
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="request">The request containing tie count information.</param>
    /// <returns>A SaveTieCountsResponseDto containing the result of the save operation.</returns>
    /// <exception cref="ArgumentException">Thrown when the election is not found.</exception>
    public async Task<SaveTieCountsResponseDto> SaveTieCountsAsync(Guid electionGuid, SaveTieCountsRequestDto request)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            throw new ArgumentException($"Election {electionGuid} not found");
        }

        var updatedCount = 0;

        foreach (var count in request.Counts)
        {
            var result = await _context.Results
                .FirstOrDefaultAsync(r => r.ElectionGuid == electionGuid && r.PersonGuid == count.PersonGuid && r.IsTied == true);

            if (result != null)
            {
                result.TieBreakCount = count.TieBreakCount;
                updatedCount++;
            }
        }

        var reAnalysisTriggered = false;

        if (updatedCount > 0)
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Saved {Count} tie break counts for election {ElectionGuid}", updatedCount, electionGuid);

            // Re-analyze after any persisted count (including a single member or all-0).
            // Partial counts already change rank; v3 always re-ran analysis after save.
            _logger.LogInformation("Tie-break counts updated, re-analyzing election {ElectionGuid}", electionGuid);
            if (election.ElectionType == "Oth")
            {
                await CalculateSingleNameElectionAsync(electionGuid);
            }
            else
            {
                await CalculateNormalElectionAsync(electionGuid);
            }

            reAnalysisTriggered = true;
        }

        return new SaveTieCountsResponseDto
        {
            Success = true,
            Message = $"Successfully saved {updatedCount} tie break counts",
            ReAnalysisTriggered = reAnalysisTriggered
        };
    }
}
