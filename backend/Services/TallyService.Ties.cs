using Backend.DTOs.Results;
using Backend.Helpers;
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

        // Resolved groups clear IsTied. They stay reachable so a teller can correct a count.
        var tieResults = await _context.Results
            .Include(r => r.Person)
            .Where(r => r.ElectionGuid == electionGuid && r.TieBreakGroup == tieBreakGroup)
            .OrderBy(r => r.Rank)
            .ToListAsync();

        if (!tieResults.Any())
        {
            throw new ArgumentException($"Tie break group {tieBreakGroup} not found in election {electionGuid}");
        }

        var section = tieResults[0].Section ?? SectionOther;
        var evaluated = TieBreakResolution.Evaluate(tieResults);
        var stillRequired = evaluated.TryGetValue(tieBreakGroup, out var status) && status.TieBreakRequired;
        var resolved = evaluated.TryGetValue(tieBreakGroup, out status) && status.IsResolved;

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
            People = people,
            TieBreakRequired = stillRequired,
            IsResolved = resolved,
            Instructions = stillRequired
                ? "Enter tie-break vote counts for these people"
                : string.Empty
        };
    }

    /// <summary>
    /// Saves tie-breaking vote counts for an election and re-analyzes when any count is persisted.
    /// Null unsets a previous count. An explicit 0 is a valid runoff result.
    /// Omit a person to leave their stored count unchanged.
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
            // TieBreakGroup stays set after a runoff is resolved, so a later edit can re-open it.
            // A person in no tie group is never given a count.
            var result = await _context.Results
                .FirstOrDefaultAsync(r => r.ElectionGuid == electionGuid
                    && r.PersonGuid == count.PersonGuid
                    && r.TieBreakGroup != null);

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
