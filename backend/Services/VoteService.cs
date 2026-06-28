using Backend.Context;
using Backend.Entities;
using Backend.Enumerations;
using Backend.DTOs.Votes;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service for managing vote operations including creation, retrieval, updates, and deletion.
/// Provides functionality to handle votes within ballots and elections.
/// </summary>
public class VoteService : IVoteService
{
    private readonly MainDbContext _context;
    private readonly ILogger<VoteService> _logger;
    private readonly IVoteCountBroadcastService _voteCountBroadcastService;

    /// <summary>
    /// Initializes a new instance of the VoteService.
    /// </summary>
    public VoteService(MainDbContext context, ILogger<VoteService> logger, IVoteCountBroadcastService voteCountBroadcastService)
    {
        _context = context;
        _logger = logger;
        _voteCountBroadcastService = voteCountBroadcastService;
    }

    /// <summary>
    /// Retrieves all votes associated with a specific ballot.
    /// </summary>
    /// <param name="ballotGuid">The unique identifier of the ballot.</param>
    /// <returns>A list of VoteDto objects representing the votes on the ballot, ordered by position.</returns>
    public async Task<List<VoteDto>> GetVotesByBallotAsync(Guid ballotGuid)
    {
        var votes = await _context.Votes
            .Where(v => v.BallotGuid == ballotGuid)
            .Include(v => v.Person)
            .OrderBy(v => v.PositionOnBallot)
            .ToListAsync();

        return votes.Select(MapToVoteDto).ToList();
    }

    /// <summary>
    /// Retrieves all votes associated with a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A list of VoteDto objects representing all votes in the election, ordered by ballot number and position.</returns>
    public async Task<List<VoteDto>> GetVotesByElectionAsync(Guid electionGuid)
    {
        var votes = await _context.Votes
            .Where(v => v.Ballot.Location.ElectionGuid == electionGuid)
            .Include(v => v.Person)
            .Include(v => v.Ballot)
            .OrderBy(v => v.Ballot.BallotNumAtComputer)
            .ThenBy(v => v.PositionOnBallot)
            .ToListAsync();

        return votes.Select(MapToVoteDto).ToList();
    }

    /// <summary>
    /// Retrieves a specific vote by its database identifier.
    /// </summary>
    /// <param name="id">The database row identifier of the vote.</param>
    /// <returns>A VoteDto object if found, null otherwise.</returns>
    public async Task<VoteDto?> GetVoteByIdAsync(int id)
    {
        var vote = await _context.Votes
            .Include(v => v.Person)
            .Include(v => v.Ballot)
            .FirstOrDefaultAsync(v => v.RowId == id);

        if (vote == null)
        {
            return null;
        }

        return MapToVoteDto(vote);
    }

    /// <summary>
    /// Creates a new vote for a ballot.
    /// The vote status is determined server-side based on the person's eligibility.
    /// If the person is ineligible, the status is set from their ineligibility reason code.
    /// </summary>
    /// <param name="createDto">The data transfer object containing vote creation information.</param>
    /// <returns>A VoteWithBallotStatusDto containing the created vote and the ballot's current status.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the ballot or person is not found.</exception>
    public async Task<VoteWithBallotStatusDto> CreateVoteAsync(CreateVoteDto createDto)
    {
        var ballot = await _context.Ballots
            .Include(b => b.Location)
            .FirstOrDefaultAsync(b => b.BallotGuid == createDto.BallotGuid);

        if (ballot == null)
        {
            throw new InvalidOperationException($"Ballot with GUID '{createDto.BallotGuid}' not found");
        }

        var electionGuid = ballot.Location.ElectionGuid;
        var statusCode = VoteStatus.Ok;
        string? ineligibleReasonCode = null;

        if (createDto.PersonGuid.HasValue)
        {
            var person = await _context.People.FirstOrDefaultAsync(p => p.PersonGuid == createDto.PersonGuid.Value);
            if (person == null)
            {
                throw new InvalidOperationException($"Person with GUID '{createDto.PersonGuid}' not found");
            }

            if (person.CanReceiveVotes != true)
            {
                statusCode = VoteStatus.Spoiled;
                ineligibleReasonCode = GetIneligibleReasonCode(person.IneligibleReasonGuid);
                _logger.LogInformation("Person '{FullName}' is ineligible; vote created as spoiled with reason {ReasonCode}",
                    person.FullName, ineligibleReasonCode);
            }

        }

        await CompactVotePositionsAsync(createDto.BallotGuid);
        await _context.SaveChangesAsync();
        var assignedPosition = await GetNextVotePositionAsync(createDto.BallotGuid);

        var vote = new Vote
        {
            BallotGuid = createDto.BallotGuid,
            PersonGuid = createDto.PersonGuid,
            PositionOnBallot = assignedPosition,
            VoteStatus = statusCode,
            IneligibleReasonCode = ineligibleReasonCode,
            RowVersion = new byte[8]
        };

        ballot.DateUpdated = DateTimeOffset.UtcNow;
        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created vote {VoteId} for ballot {BallotGuid} at position {Position}",
            vote.RowId, vote.BallotGuid, vote.PositionOnBallot);

        await RefreshBallotStatusAsync(ballot);
        await _context.SaveChangesAsync();

        if (createDto.PersonGuid.HasValue)
        {
            QueueVoteCountBroadcast(createDto.PersonGuid.Value, electionGuid);
        }

        return await BuildVoteMutationResponseAsync(ballot, vote.RowId);
    }

    /// <summary>
    /// Updates an existing vote with new information.
    /// The vote status is determined server-side based on the person's eligibility.
    /// </summary>
    /// <param name="id">The database row identifier of the vote to update.</param>
    /// <param name="updateDto">The data transfer object containing updated vote information.</param>
    /// <returns>A VoteWithBallotStatusDto containing the updated vote and ballot status, or null if the vote was not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the ballot or person is not found.</exception>
    public async Task<VoteWithBallotStatusDto?> UpdateVoteAsync(int id, CreateVoteDto updateDto)
    {
        var vote = await _context.Votes.FirstOrDefaultAsync(v => v.RowId == id);
        if (vote == null)
        {
            return null;
        }

        var ballot = await _context.Ballots
            .Include(b => b.Location)
            .FirstOrDefaultAsync(b => b.BallotGuid == updateDto.BallotGuid);
        if (ballot == null)
        {
            throw new InvalidOperationException($"Ballot with GUID '{updateDto.BallotGuid}' not found");
        }

        var statusCode = VoteStatus.Ok;
        string? ineligibleReasonCode = null;

        if (updateDto.PersonGuid.HasValue)
        {
            var person = await _context.People.FirstOrDefaultAsync(p => p.PersonGuid == updateDto.PersonGuid.Value);
            if (person == null)
            {
                throw new InvalidOperationException($"Person with GUID '{updateDto.PersonGuid}' not found");
            }

            if (person.CanReceiveVotes != true)
            {
                statusCode = VoteStatus.Spoiled;
                ineligibleReasonCode = GetIneligibleReasonCode(person.IneligibleReasonGuid);
                _logger.LogInformation("Person '{FullName}' is ineligible; vote updated as spoiled with reason {ReasonCode}",
                    person.FullName, ineligibleReasonCode);
            }
        }

        vote.BallotGuid = updateDto.BallotGuid;
        vote.PersonGuid = updateDto.PersonGuid;
        vote.VoteStatus = statusCode;
        vote.IneligibleReasonCode = ineligibleReasonCode;

        ballot.DateUpdated = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        await CompactVotePositionsAsync(updateDto.BallotGuid);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated vote {VoteId} for ballot {BallotGuid}", id, updateDto.BallotGuid);

        await RefreshBallotStatusAsync(ballot);
        await _context.SaveChangesAsync();

        return await BuildVoteMutationResponseAsync(ballot, id);
    }

    /// <summary>
    /// Deletes a vote by its database identifier.
    /// Broadcasts the updated live vote count for the affected person via SignalR.
    /// </summary>
    /// <param name="id">The database row identifier of the vote to delete.</param>
    /// <returns>The updated ballot status, or null if the vote was not found.</returns>
    public async Task<VoteWithBallotStatusDto?> DeleteVoteAsync(int id)
    {
        var vote = await _context.Votes
            .Include(v => v.Ballot)
                .ThenInclude(b => b.Location)
            .FirstOrDefaultAsync(v => v.RowId == id);

        if (vote == null)
        {
            return null;
        }

        var personGuid = vote.PersonGuid;
        var electionGuid = vote.Ballot.Location.ElectionGuid;
        var ballot = vote.Ballot;
        var ballotGuid = vote.BallotGuid;

        ballot.DateUpdated = DateTimeOffset.UtcNow;
        _context.Votes.Remove(vote);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted vote {VoteId}", id);

        await CompactVotePositionsAsync(ballotGuid);
        await _context.SaveChangesAsync();

        if (personGuid.HasValue)
        {
            QueueVoteCountBroadcast(personGuid.Value, electionGuid);
        }

        if (ballot != null)
        {
            await RefreshBallotStatusAsync(ballot);
            await _context.SaveChangesAsync();
        }

        return ballot == null
            ? null
            : await BuildVoteMutationResponseAsync(ballot, highlightedVoteId: null);
    }

    /// <summary>
    /// Reorders votes on a ballot to match the supplied row ID sequence.
    /// </summary>
    public async Task<VoteWithBallotStatusDto?> ReorderVotesAsync(ReorderVotesDto reorderDto)
    {
        var ballot = await _context.Ballots
            .Include(b => b.Location)
            .FirstOrDefaultAsync(b => b.BallotGuid == reorderDto.BallotGuid);

        if (ballot == null)
        {
            return null;
        }

        var votes = await _context.Votes
            .Where(v => v.BallotGuid == reorderDto.BallotGuid)
            .ToListAsync();

        if (votes.Count != reorderDto.VoteRowIds.Count)
        {
            throw new InvalidOperationException(
                "Vote row ID count does not match the number of votes on this ballot");
        }

        var voteById = votes.ToDictionary(v => v.RowId);
        if (reorderDto.VoteRowIds.Any(id => !voteById.ContainsKey(id)))
        {
            throw new InvalidOperationException("One or more vote row IDs do not belong to this ballot");
        }

        for (var i = 0; i < reorderDto.VoteRowIds.Count; i++)
        {
            voteById[reorderDto.VoteRowIds[i]].PositionOnBallot = i + 1;
        }

        ballot.DateUpdated = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("Reordered {VoteCount} votes for ballot {BallotGuid}",
            votes.Count, reorderDto.BallotGuid);

        await RefreshBallotStatusAsync(ballot);
        await _context.SaveChangesAsync();

        return await BuildVoteMutationResponseAsync(ballot, highlightedVoteId: null);
    }

    private async Task<int> GetNextVotePositionAsync(Guid ballotGuid)
    {
        var occupiedPositions = await _context.Votes
            .Where(v => v.BallotGuid == ballotGuid)
            .Select(v => v.PositionOnBallot)
            .ToListAsync();

        var position = 1;
        while (occupiedPositions.Contains(position))
        {
            position++;
        }

        return position;
    }

    private async Task CompactVotePositionsAsync(Guid ballotGuid)
    {
        var votes = await _context.Votes
            .Where(v => v.BallotGuid == ballotGuid)
            .OrderBy(v => v.PositionOnBallot)
            .ThenBy(v => v.RowId)
            .ToListAsync();

        for (var i = 0; i < votes.Count; i++)
        {
            votes[i].PositionOnBallot = i + 1;
        }
    }

    private async Task<VoteWithBallotStatusDto> BuildVoteMutationResponseAsync(
        Ballot ballot,
        int? highlightedVoteId)
    {
        var voteDtos = await GetBallotVoteDtosAsync(ballot.BallotGuid);
        VoteDto? highlightedVote = null;
        if (highlightedVoteId.HasValue)
        {
            highlightedVote = voteDtos.FirstOrDefault(v => v.RowId == highlightedVoteId.Value);
        }

        return new VoteWithBallotStatusDto
        {
            Vote = highlightedVote,
            BallotStatusCode = ballot.StatusCode,
            Votes = voteDtos
        };
    }

    private async Task<List<VoteDto>> GetBallotVoteDtosAsync(Guid ballotGuid)
    {
        var votes = await _context.Votes
            .Where(v => v.BallotGuid == ballotGuid)
            .Include(v => v.Person)
            .OrderBy(v => v.PositionOnBallot)
            .ThenBy(v => v.RowId)
            .ToListAsync();

        return votes.Select(MapToVoteDto).ToList();
    }

    private Task RefreshBallotStatusAsync(Ballot ballot) =>
        BallotStatusRefresher.RefreshAsync(_context, ballot, _logger);

    private void QueueVoteCountBroadcast(Guid personGuid, Guid electionGuid)
    {
        _voteCountBroadcastService.QueueVoteCountUpdate(personGuid, electionGuid);
        _logger.LogDebug("Queued vote count broadcast for person {PersonGuid} in election {ElectionGuid}",
            personGuid, electionGuid);
    }

    private static VoteDto MapToVoteDto(Vote vote)
    {
        var dto = vote.CopyMatchingPropertiesToNew<VoteDto>();
        dto.PersonFullName = vote.Person?.FullName;

        if (string.IsNullOrEmpty(dto.IneligibleReasonCode)
            && vote.VoteStatus == VoteStatus.Spoiled
            && vote.Person?.CanReceiveVotes != true)
        {
            dto.IneligibleReasonCode = GetIneligibleReasonCode(vote.Person.IneligibleReasonGuid);
        }

        return dto;
    }

    private static string? GetIneligibleReasonCode(Guid? guid)
    {
        return guid.HasValue ? IneligibleReasonEnum.GetByGuid(guid.Value)?.Code : null;
    }
}
