using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Backend.DTOs.SignalR;
using Backend.DTOs.Votes;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;

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
    private readonly ISignalRNotificationService _signalRNotificationService;

    /// <summary>
    /// Initializes a new instance of the VoteService.
    /// </summary>
    /// <param name="context">The main database context for accessing vote data.</param>
    /// <param name="mapper">AutoMapper instance for object mapping operations.</param>
    /// <param name="logger">Logger for recording vote service operations.</param>
    /// <param name="signalRNotificationService">Service for broadcasting real-time updates.</param>
    public VoteService(MainDbContext context, IMapper mapper, ILogger<VoteService> logger, ISignalRNotificationService signalRNotificationService)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
        _signalRNotificationService = signalRNotificationService;
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
    /// If the person is ineligible to receive votes, the vote is created as a spoiled vote
    /// with the person's ineligibility reason code as the status.
    /// </summary>
    /// <param name="createDto">The data transfer object containing vote creation information.</param>
    /// <returns>A VoteDto representing the created vote.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the ballot or person is not found.</exception>
    public async Task<VoteDto> CreateVoteAsync(CreateVoteDto createDto)
    {
        var ballot = await _context.Ballots
            .Include(b => b.Location)
            .FirstOrDefaultAsync(b => b.BallotGuid == createDto.BallotGuid);

        if (ballot == null)
        {
            throw new InvalidOperationException($"Ballot with GUID '{createDto.BallotGuid}' not found");
        }

        var electionGuid = ballot.Location.ElectionGuid;
        var statusCode = createDto.StatusCode;

        if (createDto.PersonGuid.HasValue)
        {
            var person = await _context.People.FirstOrDefaultAsync(p => p.PersonGuid == createDto.PersonGuid.Value);
            if (person == null)
            {
                throw new InvalidOperationException($"Person with GUID '{createDto.PersonGuid}' not found");
            }

            if (person.CanReceiveVotes != true)
            {
                var ineligibleCode = IneligibleReasonEnum.GetByGuid(person.IneligibleReasonGuid)?.Code;
                statusCode = ineligibleCode ?? "spoiled";
                _logger.LogInformation("Person '{FullName}' is ineligible; vote created as spoiled with status '{StatusCode}'",
                    person.FullName, statusCode);
            }

            var duplicateVote = await _context.Votes
                .AnyAsync(v => v.BallotGuid == createDto.BallotGuid && v.PersonGuid == createDto.PersonGuid);

            if (duplicateVote)
            {
                throw new InvalidOperationException($"Person '{person.FullName}' already has a vote on this ballot");
            }
        }

        var vote = new Vote
        {
            BallotGuid = createDto.BallotGuid,
            PersonGuid = createDto.PersonGuid,
            PositionOnBallot = createDto.PositionOnBallot,
            StatusCode = statusCode ?? "ok",
            RowVersion = new byte[8]
        };

        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created vote {VoteId} for ballot {BallotGuid} at position {Position}",
            vote.RowId, vote.BallotGuid, vote.PositionOnBallot);

        if (createDto.PersonGuid.HasValue)
        {
            await BroadcastPersonVoteCountAsync(createDto.PersonGuid.Value, electionGuid);
        }

        return await GetVoteByIdAsync(vote.RowId) ?? _mapper.Map<VoteDto>(vote);
    }

    /// <summary>
    /// Updates an existing vote with new information.
    /// </summary>
    /// <param name="id">The database row identifier of the vote to update.</param>
    /// <param name="updateDto">The data transfer object containing updated vote information.</param>
    /// <returns>A VoteDto representing the updated vote, or null if the vote was not found.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the ballot or person is not found, or when validation fails.</exception>
    public async Task<VoteDto?> UpdateVoteAsync(int id, CreateVoteDto updateDto)
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

        if (updateDto.PersonGuid.HasValue)
        {
            var person = await _context.People.FirstOrDefaultAsync(p => p.PersonGuid == updateDto.PersonGuid.Value);
            if (person == null)
            {
                throw new InvalidOperationException($"Person with GUID '{updateDto.PersonGuid}' not found");
            }

            if (person.CanReceiveVotes != true)
            {
                throw new InvalidOperationException($"Person '{person.FullName}' is not eligible to receive votes (CanReceiveVotes = false)");
            }

            var duplicateVote = await _context.Votes
                .AnyAsync(v => v.BallotGuid == updateDto.BallotGuid && v.PersonGuid == updateDto.PersonGuid && v.RowId != id);

            if (duplicateVote)
            {
                throw new InvalidOperationException($"Person '{person.FullName}' already has a vote on this ballot");
            }
        }

        vote.BallotGuid = updateDto.BallotGuid;
        vote.PersonGuid = updateDto.PersonGuid;
        vote.PositionOnBallot = updateDto.PositionOnBallot;
        vote.StatusCode = updateDto.StatusCode ?? "ok";

        await _context.SaveChangesAsync();

        _logger.LogInformation("Updated vote {VoteId} for ballot {BallotGuid}", id, updateDto.BallotGuid);

        return await GetVoteByIdAsync(id);
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

        _context.Votes.Remove(vote);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted vote {VoteId}", id);

        if (personGuid.HasValue)
        {
            await BroadcastPersonVoteCountAsync(personGuid.Value, electionGuid);
        }

        return true;
    }

    private async Task BroadcastPersonVoteCountAsync(Guid personGuid, Guid electionGuid)
    {
        try
        {
            var liveCount = await _context.Votes
                .CountAsync(v => v.PersonGuid == personGuid &&
                                 v.Ballot.Location.ElectionGuid == electionGuid);

            await _signalRNotificationService.SendPersonVoteCountUpdateAsync(new PersonVoteCountUpdateDto
            {
                ElectionGuid = electionGuid,
                PersonGuid = personGuid,
                VoteCount = liveCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error broadcasting vote count for person {PersonGuid} in election {ElectionGuid}",
                personGuid, electionGuid);
        }
    }

    private VoteDto MapToVoteDto(Vote vote)
    {
        var dto = _mapper.Map<VoteDto>(vote);
        dto.PersonFullName = vote.Person?.FullName;
        return dto;
    }
}
