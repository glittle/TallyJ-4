using Mapster;
using Microsoft.EntityFrameworkCore;
using Backend.DTOs.Votes;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using MapsterMapper;

namespace Backend.Services;

/// <summary>
/// Service for managing vote operations including creation, retrieval, updates, and deletion.
/// Provides functionality to handle votes within ballots and elections.
/// </summary>
public class VoteService : IVoteService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<VoteService> _logger;
    private readonly IVoteCountBroadcastService _voteCountBroadcastService;

    /// <summary>
    /// Initializes a new instance of the VoteService.
    /// </summary>
    /// <param name="context">The main database context for accessing vote data.</param>
    /// <param name="mapper">AutoMapper instance for object mapping operations.</param>
    /// <param name="logger">Logger for recording vote service operations.</param>
    /// <param name="voteCountBroadcastService">Service for batching and broadcasting vote count updates.</param>
    public VoteService(MainDbContext context, IMapper mapper, ILogger<VoteService> logger, IVoteCountBroadcastService voteCountBroadcastService)
    {
        _context = context;
        _mapper = mapper;
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
                _logger.LogInformation("Person '{FullName}' is ineligible; vote created as spoiled",
                    person.FullName);
            }

            var duplicateVote = await _context.Votes
                .AnyAsync(v => v.BallotGuid == createDto.BallotGuid && v.PersonGuid == createDto.PersonGuid);

            if (duplicateVote)
            {
                ballot.StatusCode = BallotStatus.Dup;
                _logger.LogInformation("Duplicate vote detected for person '{FullName}' on ballot {BallotGuid}; ballot status set to Dup",
                    person.FullName, createDto.BallotGuid);
            }
        }

        var vote = new Vote
        {
            BallotGuid = createDto.BallotGuid,
            PersonGuid = createDto.PersonGuid,
            PositionOnBallot = createDto.PositionOnBallot,
            VoteStatus = statusCode,
            RowVersion = new byte[8]
        };

        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created vote {VoteId} for ballot {BallotGuid} at position {Position}",
            vote.RowId, vote.BallotGuid, vote.PositionOnBallot);

        if (createDto.PersonGuid.HasValue)
        {
            QueueVoteCountBroadcast(createDto.PersonGuid.Value, electionGuid);
        }

        var voteDto = await GetVoteByIdAsync(vote.RowId) ?? _mapper.Map<VoteDto>(vote);
        return new VoteWithBallotStatusDto { Vote = voteDto, BallotStatusCode = ballot.StatusCode };
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

        var ballot = await _context.Ballots.FirstOrDefaultAsync(b => b.BallotGuid == updateDto.BallotGuid);
        if (ballot == null)
        {
            throw new InvalidOperationException($"Ballot with GUID '{updateDto.BallotGuid}' not found");
        }

        var statusCode = VoteStatus.Ok;
        var duplicateFound = false;

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
                _logger.LogInformation("Person '{FullName}' is ineligible; vote updated as spoiled",
                    person.FullName);
            }

            var duplicateVote = await _context.Votes
                .AnyAsync(v => v.BallotGuid == updateDto.BallotGuid && v.PersonGuid == updateDto.PersonGuid && v.RowId != id);

            if (duplicateVote)
            {
                duplicateFound = true;
                ballot.StatusCode = BallotStatus.Dup;
                _logger.LogInformation("Duplicate vote detected for person '{FullName}' on ballot {BallotGuid}; ballot status set to Dup",
                    person.FullName, updateDto.BallotGuid);
            }
        }

        vote.BallotGuid = updateDto.BallotGuid;
        vote.PersonGuid = updateDto.PersonGuid;
        vote.PositionOnBallot = updateDto.PositionOnBallot;
        vote.VoteStatus = statusCode;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated vote {VoteId} for ballot {BallotGuid}", id, updateDto.BallotGuid);

        if (!duplicateFound && ballot.StatusCode == BallotStatus.Dup)
        {
            var hasDuplicates = await HasBallotDuplicatesAsync(ballot.BallotGuid);
            if (!hasDuplicates)
            {
                ballot.StatusCode = BallotStatus.Ok;
                await _context.SaveChangesAsync();
            }
        }

        var voteDto = await GetVoteByIdAsync(id);
        if (voteDto == null) return null;
        return new VoteWithBallotStatusDto { Vote = voteDto, BallotStatusCode = ballot.StatusCode };
    }

    /// <summary>
    /// Deletes a vote by its database identifier.
    /// Broadcasts the updated live vote count for the affected person via SignalR.
    /// </summary>
    /// <param name="id">The database row identifier of the vote to delete.</param>
    /// <returns>True if the vote was successfully deleted, false if the vote was not found.</returns>
    public async Task<bool> DeleteVoteAsync(int id)
    {
        var vote = await _context.Votes
            .Include(v => v.Ballot)
                .ThenInclude(b => b.Location)
            .FirstOrDefaultAsync(v => v.RowId == id);

        if (vote == null)
        {
            return false;
        }

        var personGuid = vote.PersonGuid;
        var electionGuid = vote.Ballot.Location.ElectionGuid;

        var ballotGuid = vote.BallotGuid;
        var ballot = vote.Ballot;

        _context.Votes.Remove(vote);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted vote {VoteId}", id);

        if (personGuid.HasValue)
        {
            QueueVoteCountBroadcast(personGuid.Value, electionGuid);
        }

        if (ballot?.StatusCode == BallotStatus.Dup)
        {
            var hasDuplicates = await HasBallotDuplicatesAsync(ballotGuid);
            if (!hasDuplicates)
            {
                ballot.StatusCode = BallotStatus.Ok;
                await _context.SaveChangesAsync();
            }
        }

        return true;
    }

    private async Task<bool> HasBallotDuplicatesAsync(Guid ballotGuid)
    {
        var personGuids = await _context.Votes
            .Where(v => v.BallotGuid == ballotGuid && v.PersonGuid != null)
            .Select(v => v.PersonGuid!.Value)
            .ToListAsync();
        return personGuids.GroupBy(g => g).Any(g => g.Count() > 1);
    }

    private void QueueVoteCountBroadcast(Guid personGuid, Guid electionGuid)
    {
        _voteCountBroadcastService.QueueVoteCountUpdate(personGuid, electionGuid);
        _logger.LogDebug("Queued vote count broadcast for person {PersonGuid} in election {ElectionGuid}",
            personGuid, electionGuid);
    }

    private VoteDto MapToVoteDto(Vote vote)
    {
        var dto = _mapper.Map<VoteDto>(vote);
        dto.PersonFullName = vote.Person?.FullName;
        return dto;
    }
}
