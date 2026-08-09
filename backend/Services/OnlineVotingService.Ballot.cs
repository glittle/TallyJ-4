using System.Collections.Generic;
using System.Text.Json;
using Backend.Entities;
using Backend.Enumerations;
using Backend.DTOs.OnlineVoting;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class OnlineVotingService
{
    /// <inheritdoc/>
    public async Task<(bool Success, string? Error)> SubmitBallotAsync(SubmitOnlineBallotDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;
        var safeVoterId = SanitizeForLog(dto.VoterId);

        try
        {
            var election = await _context.Elections
                .FirstOrDefaultAsync(e => e.ElectionGuid == dto.ElectionGuid);

            if (election == null)
            {
                return (false, "voting.submit.electionNotFound");
            }

            if (!election.UseOnlineVoting ||
                (election.OnlineWhenOpen != null && election.OnlineWhenOpen > now) ||
                (election.OnlineWhenClose != null && election.OnlineWhenClose <= now))
            {
                return (false, "voting.submit.notOpen");
            }

            var onlineVoter = await _context.OnlineVoters
                .FirstOrDefaultAsync(ov => ov.VoterId == dto.VoterId);

            if (onlineVoter == null)
            {
                return (false, "voting.submit.voterNotFound");
            }

            var person = await _context.People
                .FirstOrDefaultAsync(p => p.ElectionGuid == dto.ElectionGuid &&
                                        (p.Email == dto.VoterId || p.Phone == dto.VoterId || p.KioskCode == dto.VoterId));

            var isResubmit = person != null && person.HasOnlineBallot == true;
            OnlineVotingInfo? existingVotingInfo = null;

            if (isResubmit && person != null)
            {
                existingVotingInfo = await _context.OnlineVotingInfos
                    .Where(ovi => ovi.ElectionGuid == dto.ElectionGuid && ovi.PersonGuid == person.PersonGuid)
                    .OrderByDescending(ovi => ovi.WhenBallotCreated)
                    .FirstOrDefaultAsync();

                if (existingVotingInfo?.BallotGuid != null)
                {
                    var oldBallot = await _context.Ballots
                        .FirstOrDefaultAsync(b => b.BallotGuid == existingVotingInfo.BallotGuid);

                    if (oldBallot != null)
                    {
                        oldBallot.StatusCode = BallotStatus.Review;
                        oldBallot.DateUpdated = now;
                        existingVotingInfo.HistoryStatus = AppendBallotHistory(
                            existingVotingInfo.HistoryStatus,
                            existingVotingInfo.BallotGuid.Value,
                            existingVotingInfo.WhenBallotCreated);
                    }
                }
            }

            var location = await _context.Locations
                .FirstOrDefaultAsync(l => l.ElectionGuid == dto.ElectionGuid && l.LocationTypeCode == nameof(LocationType.Online));

            if (location == null)
            {
                location = new Location
                {
                    LocationGuid = Guid.NewGuid(),
                    ElectionGuid = dto.ElectionGuid,
                    Name = "Online",
                    ContactInfo = "Online voting",
                    SortOrder = 999,
                    LocationTypeCode = nameof(LocationType.Online)
                };
                _context.Locations.Add(location);
                await _context.SaveChangesAsync();
            }

            var ballot = new Ballot
            {
                LocationGuid = location.LocationGuid,
                BallotGuid = Guid.NewGuid(),
                StatusCode = BallotStatus.Ok,
                ComputerCode = "WW",
                BallotNumAtComputer = 0,
                Teller1 = "Online",
                DateCreated = now,
                DateUpdated = now,
                RowVersion = new byte[8]
            };

            _context.Ballots.Add(ballot);
            await _context.SaveChangesAsync();

            foreach (var voteDto in dto.Votes.OrderBy(v => v.PositionOnBallot))
            {
                var hasPerson = voteDto.PersonGuid.HasValue;
                var hasFreeText = !string.IsNullOrWhiteSpace(voteDto.VoteName);
                var vote = new Vote
                {
                    BallotGuid = ballot.BallotGuid,
                    PositionOnBallot = voteDto.PositionOnBallot,
                    PersonGuid = voteDto.PersonGuid,
                    VoteStatus = hasPerson
                        ? VoteStatus.Ok
                        : hasFreeText
                            ? VoteStatus.Raw
                            : VoteStatus.Spoiled,
                    OnlineVoteRaw = voteDto.VoteName,
                    RowVersion = new byte[8]
                };

                if (voteDto.PersonGuid is Guid personGuid)
                {
                    var votedPerson = await _context.People
                        .FirstOrDefaultAsync(p => p.PersonGuid == personGuid);

                    if (votedPerson != null)
                    {
                        vote.PersonCombinedInfo = votedPerson.CombinedInfo;
                    }
                }
                else if (hasFreeText)
                {
                    vote.PersonCombinedInfo = voteDto.VoteName!.Trim();
                }

                _context.Votes.Add(vote);
            }

            if (existingVotingInfo != null)
            {
                existingVotingInfo.BallotGuid = ballot.BallotGuid;
                existingVotingInfo.WhenBallotCreated = now;
                existingVotingInfo.WhenStatus = now;
                existingVotingInfo.Status = "Submitted";
                existingVotingInfo.ListPool = SerializeListPool(dto.ListPool);
            }
            else
            {
                var votingInfo = new OnlineVotingInfo
                {
                    ElectionGuid = dto.ElectionGuid,
                    PersonGuid = person?.PersonGuid ?? Guid.NewGuid(),
                    BallotGuid = ballot.BallotGuid,
                    WhenBallotCreated = now,
                    Status = "Submitted",
                    WhenStatus = now,
                    ListPool = SerializeListPool(dto.ListPool)
                };

                _context.OnlineVotingInfos.Add(votingInfo);
            }

            if (person != null)
            {
                person.HasOnlineBallot = true;
            }

            await ApplyNotifyPreferenceAsync(onlineVoter, dto.NotifyWhenProcessed);

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            _logger.LogInformation("Online ballot submitted for voter {VoterId} in election {ElectionGuid}",
                safeVoterId, dto.ElectionGuid);

            return (true, null);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error submitting online ballot for voter {VoterId}", safeVoterId);
            return (false, "voting.submit.error");
        }
    }

    /// <inheritdoc/>
    public async Task<OnlineVoteStatusDto> GetVoteStatusAsync(Guid electionGuid, string voterId)
    {
        var person = await _context.People
            .FirstOrDefaultAsync(p => p.ElectionGuid == electionGuid &&
                                    (p.Email == voterId || p.Phone == voterId || p.KioskCode == voterId));

        if (person == null)
        {
            return new OnlineVoteStatusDto
            {
                HasVoted = false,
                Message = "voting.status.voterNotFound"
            };
        }

        var votingInfo = await _context.OnlineVotingInfos
            .Where(ov => ov.ElectionGuid == electionGuid && ov.PersonGuid == person.PersonGuid)
            .OrderByDescending(ov => ov.WhenBallotCreated)
            .FirstOrDefaultAsync();

        var priorVotes = new List<OnlineVoteDto>();
        if (votingInfo?.BallotGuid != null)
        {
            priorVotes = await _context.Votes
                .Where(v => v.BallotGuid == votingInfo.BallotGuid)
                .OrderBy(v => v.PositionOnBallot)
                .Select(v => new OnlineVoteDto
                {
                    PersonGuid = v.PersonGuid,
                    VoteName = v.OnlineVoteRaw ?? v.PersonCombinedInfo,
                    PositionOnBallot = v.PositionOnBallot
                })
                .ToListAsync();
        }

        var onlineVoter = await _context.OnlineVoters
            .FirstOrDefaultAsync(ov => ov.VoterId == voterId);

        return new OnlineVoteStatusDto
        {
            HasVoted = person.HasOnlineBallot == true,
            WhenSubmitted = votingInfo?.WhenBallotCreated,
            Message = person.HasOnlineBallot == true
                ? "voting.status.alreadyVoted"
                : "voting.status.notVoted",
            PriorVotes = priorVotes,
            ListPool = ParseListPool(votingInfo?.ListPool),
            NotifyWhenProcessed = HasNotifyProcessedPreference(onlineVoter?.EmailCodes)
        };
    }

    private async Task ApplyNotifyPreferenceAsync(OnlineVoter? onlineVoter, bool notifyWhenProcessed)
    {
        if (onlineVoter == null)
        {
            return;
        }

        var codes = ParseEmailCodes(onlineVoter.EmailCodes);
        if (notifyWhenProcessed)
        {
            codes.Add(NotifyProcessedCode);
        }
        else
        {
            codes.Remove(NotifyProcessedCode);
        }

        onlineVoter.EmailCodes = codes.Count == 0 ? null : string.Join("|", codes);
        await Task.CompletedTask;
    }

    private static bool HasNotifyProcessedPreference(string? emailCodes)
    {
        return ParseEmailCodes(emailCodes).Contains(NotifyProcessedCode);
    }

    private static HashSet<string> ParseEmailCodes(string? emailCodes)
    {
        if (string.IsNullOrWhiteSpace(emailCodes))
        {
            return new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        return emailCodes
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    private static string? SerializeListPool(List<OnlinePoolEntryDto> pool)
    {
        if (pool == null || pool.Count == 0)
        {
            return null;
        }

        return JsonSerializer.Serialize(pool);
    }

    private static List<OnlinePoolEntryDto> ParseListPool(string? listPool)
    {
        if (string.IsNullOrWhiteSpace(listPool))
        {
            return new List<OnlinePoolEntryDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<OnlinePoolEntryDto>>(listPool)
                   ?? new List<OnlinePoolEntryDto>();
        }
        catch
        {
            return new List<OnlinePoolEntryDto>();
        }
    }

    private static string AppendBallotHistory(string? existing, Guid ballotGuid, DateTimeOffset? when)
    {
        var entry = $"{ballotGuid}|{when:O}";
        return string.IsNullOrEmpty(existing) ? entry : $"{existing};{entry}";
    }
}
