using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TallyJ4.DTOs.Votes;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Entities;

namespace TallyJ4.Services;

public class VoteService : IVoteService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<VoteService> _logger;

    public VoteService(MainDbContext context, IMapper mapper, ILogger<VoteService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<List<VoteDto>> GetVotesByBallotAsync(Guid ballotGuid)
    {
        var votes = await _context.Votes
            .Where(v => v.BallotGuid == ballotGuid)
            .Include(v => v.Person)
            .OrderBy(v => v.PositionOnBallot)
            .ToListAsync();

        return votes.Select(MapToVoteDto).ToList();
    }

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

    public async Task<VoteDto> CreateVoteAsync(CreateVoteDto createDto)
    {
        var ballot = await _context.Ballots.FirstOrDefaultAsync(b => b.BallotGuid == createDto.BallotGuid);
        if (ballot == null)
        {
            throw new InvalidOperationException($"Ballot with GUID '{createDto.BallotGuid}' not found");
        }

        if (createDto.PersonGuid.HasValue)
        {
            var person = await _context.People.FirstOrDefaultAsync(p => p.PersonGuid == createDto.PersonGuid.Value);
            if (person == null)
            {
                throw new InvalidOperationException($"Person with GUID '{createDto.PersonGuid}' not found");
            }

            if (person.CanReceiveVotes != true)
            {
                throw new InvalidOperationException($"Person '{person.FullName}' is not eligible to receive votes (CanReceiveVotes = false)");
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
            StatusCode = createDto.StatusCode ?? "ok",
            RowVersion = new byte[8]
        };

        _context.Votes.Add(vote);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Created vote {VoteId} for ballot {BallotGuid} at position {Position}",
            vote.RowId, vote.BallotGuid, vote.PositionOnBallot);

        return await GetVoteByIdAsync(vote.RowId) ?? _mapper.Map<VoteDto>(vote);
    }

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

    public async Task<bool> DeleteVoteAsync(int id)
    {
        var vote = await _context.Votes.FirstOrDefaultAsync(v => v.RowId == id);
        if (vote == null)
        {
            return false;
        }

        _context.Votes.Remove(vote);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Deleted vote {VoteId}", id);

        return true;
    }

    private VoteDto MapToVoteDto(Vote vote)
    {
        var dto = _mapper.Map<VoteDto>(vote);
        dto.PersonFullName = vote.Person?.FullName;
        return dto;
    }
}
