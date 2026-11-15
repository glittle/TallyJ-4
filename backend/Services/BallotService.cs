using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.DTOs.Ballots;
using Backend.DTOs.Votes;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Teller paper ballots. A ballot is stored on a location; the election is
/// that location's election. The Online and Imported locations are reserved
/// and reject a teller-created ballot. Those ballots also cannot be deleted.
/// <see cref="BallotStatus.Review"/> stays until the teller clears it.
/// </summary>
public class BallotService : IBallotService
{
    private readonly MainDbContext _context;
    private readonly ILogger<BallotService> _logger;

    public BallotService(MainDbContext context, ILogger<BallotService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Ballots for one election, ordered by computer code then number at that computer.
    /// Repairs legacy online codes (<c>WW</c>, number 0) before the list is read.
    /// </summary>
    public async Task<PaginatedResponse<BallotDto>> GetBallotsByElectionAsync(Guid electionGuid, int pageNumber = 1, int pageSize = 50)
    {
        await SpecialBallotNumbering.RepairOnlineForElectionAsync(_context, electionGuid);

        var query = _context.Ballots
            .Where(b => b.Location.ElectionGuid == electionGuid)
            .Include(b => b.Location)
            .Include(b => b.Votes)
                .ThenInclude(v => v.Person)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var ballots = await query
            .OrderBy(b => b.ComputerCode)
            .ThenBy(b => b.BallotNumAtComputer)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var ballotDtos = ballots.Select(b => MapToBallotDto(b)).ToList();

        return PaginatedResponse<BallotDto>.Create(ballotDtos, pageNumber, pageSize, totalCount);
    }

    /// <summary>
    /// One ballot with its votes. An online ballot still numbered 0, or still
    /// on the legacy <c>WW</c> code, is renumbered onto <c>OL</c> first.
    /// </summary>
    public async Task<BallotDto?> GetBallotByGuidAsync(Guid ballotGuid)
    {
        var ballot = await _context.Ballots
            .Include(b => b.Location)
            .Include(b => b.Votes)
                .ThenInclude(v => v.Person)
            .FirstOrDefaultAsync(b => b.BallotGuid == ballotGuid);

        if (ballot == null)
        {
            return null;
        }

        if (ComputerCodeHelper.IsOnlineCode(ballot.ComputerCode)
            && (ballot.BallotNumAtComputer <= 0 || ballot.ComputerCode == ComputerCodeHelper.LegacyOnline))
        {
            await SpecialBallotNumbering.RepairOnlineAndGetNextAsync(_context, ballot.LocationGuid);
            ballot = await _context.Ballots
                .Include(b => b.Location)
                .Include(b => b.Votes)
                    .ThenInclude(v => v.Person)
                .FirstAsync(b => b.BallotGuid == ballotGuid);
        }

        return MapToBallotDto(ballot);
    }

    /// <summary>
    /// Next paper ballot at this computer and location. The number is one past
    /// the highest <c>BallotNumAtComputer</c> already stored for that pair.
    /// Refused when the election is Finalized, or when the location is missing,
    /// in another election, or the reserved Online / Imported location.
    /// </summary>
    public async Task<BallotDto> CreateBallotAsync(CreateBallotDto createDto)
    {
        await ElectionFinalizedWriteGuard.ThrowIfLockedAsync(_context, createDto.ElectionGuid);

        var location = await _context.Locations.FirstOrDefaultAsync(l =>
            l.LocationGuid == createDto.LocationGuid
            && l.ElectionGuid == createDto.ElectionGuid);

        if (location == null)
        {
            throw new InvalidOperationException("Location not found");
        }

        if (location.LocationTypeEnum == LocationType.Online)
        {
            throw new InvalidOperationException(
                "Ballots cannot be created at the Online location");
        }

        if (location.LocationTypeEnum == LocationType.Imported)
        {
            throw new InvalidOperationException(
                "Ballots cannot be created at the Imported location");
        }

        var nextBallotNum = await _context.Ballots
            .Where(b => b.LocationGuid == location.LocationGuid && b.ComputerCode == createDto.ComputerCode)
            .MaxAsync(b => (int?)b.BallotNumAtComputer) ?? 0;

        nextBallotNum++;

        var now = DateTimeOffset.UtcNow;
        var ballot = new Ballot
        {
            BallotGuid = Guid.NewGuid(),
            LocationGuid = location.LocationGuid,
            ComputerCode = createDto.ComputerCode,
            BallotNumAtComputer = nextBallotNum,
            BallotCode = $"{createDto.ComputerCode}{nextBallotNum}",
            StatusCode = BallotStatus.Empty,
            Teller1 = NormalizeTellerName(createDto.Teller1),
            Teller2 = NormalizeTellerName(createDto.Teller2),
            DateCreated = now,
            DateUpdated = now,
            RowVersion = new byte[8],
        };

        _context.Ballots.Add(ballot);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created ballot {BallotGuid} - {BallotCode} at location {LocationGuid}",
            ballot.BallotGuid, ballot.BallotCode, ballot.LocationGuid);

        return await GetBallotByGuidAsync(ballot.BallotGuid) ?? ballot.CopyMatchingPropertiesToNew<BallotDto>();
    }

    /// <summary>
    /// Teller names, and status when Review is not holding the ballot.
    /// Clearing Review reseeds <see cref="BallotStatus.Empty"/> and then
    /// re-evaluates from the votes. A teller cannot move a ballot out of
    /// Review by sending another status.
    /// </summary>
    public async Task<BallotDto?> UpdateBallotAsync(Guid ballotGuid, UpdateBallotDto updateDto)
    {
        var ballot = await _context.Ballots.FirstOrDefaultAsync(b => b.BallotGuid == ballotGuid);

        if (ballot == null)
        {
            return null;
        }

        await ThrowIfBallotElectionLockedAsync(ballot.LocationGuid);

        ballot.Teller1 = NormalizeTellerName(updateDto.Teller1);
        ballot.Teller2 = NormalizeTellerName(updateDto.Teller2);

        if (updateDto.ClearNeedsReview)
        {
            if (ballot.StatusCode != BallotStatus.Review)
            {
                throw new InvalidOperationException(
                    "Needs Review can only be cleared for ballots currently in Review status.");
            }

            // Review is sticky in BallotAnalyzer; seed a neutral status before re-evaluating.
            ballot.StatusCode = BallotStatus.Empty;
            await BallotStatusRefresher.RefreshAsync(_context, ballot, _logger);
        }
        else if (ballot.StatusCode == BallotStatus.Review)
        {
            // Review is manual and sticky — teller updates cannot change it.
        }
        else if (updateDto.StatusCode == BallotStatus.Review)
        {
            ballot.StatusCode = BallotStatus.Review;
        }
        else
        {
            ballot.StatusCode = updateDto.StatusCode;
        }

        ballot.DateUpdated = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated ballot {BallotGuid}", ballotGuid);

        return await GetBallotByGuidAsync(ballotGuid);
    }

    private async Task ThrowIfBallotElectionLockedAsync(Guid locationGuid)
    {
        var electionGuid = await _context.Locations
            .AsNoTracking()
            .Where(l => l.LocationGuid == locationGuid)
            .Select(l => (Guid?)l.ElectionGuid)
            .FirstOrDefaultAsync();

        if (electionGuid.HasValue)
        {
            await ElectionFinalizedWriteGuard.ThrowIfLockedAsync(_context, electionGuid.Value);
        }
    }

    private static string? NormalizeTellerName(string? tellerName)
    {
        return string.IsNullOrWhiteSpace(tellerName) ? null : tellerName.Trim();
    }

    /// <summary>
    /// Deletes a paper ballot and its votes. Online and imported ballots are refused.
    /// Returns false when the ballot does not exist.
    /// </summary>
    public async Task<bool> DeleteBallotAsync(Guid ballotGuid)
    {
        var ballot = await _context.Ballots.FirstOrDefaultAsync(b => b.BallotGuid == ballotGuid);

        if (ballot == null)
        {
            return false;
        }

        await ThrowIfBallotElectionLockedAsync(ballot.LocationGuid);

        if (ComputerCodeHelper.IsOnlineCode(ballot.ComputerCode)
            || ComputerCodeHelper.IsImportedCode(ballot.ComputerCode))
        {
            throw new InvalidOperationException(
                "Online or imported ballots cannot be deleted");
        }

        _context.Ballots.Remove(ballot);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted ballot {BallotGuid}", ballotGuid);

        return true;
    }

    private BallotDto MapToBallotDto(Ballot ballot)
    {
        var dto = ballot.CopyMatchingPropertiesToNew<BallotDto>();
        dto.LocationName = ballot.Location.Name;
        dto.BallotCode = ballot.BallotCode ?? $"{ballot.ComputerCode}{ballot.BallotNumAtComputer}";
        dto.VoteCount = ballot.Votes.Count;
        dto.Votes = ballot.Votes.Select(MapToVoteDto).ToList();

        return dto;
    }

    private static VoteDto MapToVoteDto(Vote vote)
    {
        var voteDto = vote.CopyMatchingPropertiesToNew<VoteDto>();
        voteDto.PersonFullName = vote.Person?.FullName;

        if (string.IsNullOrEmpty(voteDto.IneligibleReasonCode)
            && vote.VoteStatus == VoteStatus.Spoiled
            && !PersonEligibilityHelper.CanReceiveVotes(vote.Person))
        {
            voteDto.IneligibleReasonCode = vote.Person?.IneligibleReasonCode;
        }

        return voteDto;
    }
}



