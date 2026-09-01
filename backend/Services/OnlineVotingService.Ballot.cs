using System.Collections.Generic;
using System.Text.Json;
using Backend.Entities;
using Backend.DTOs.OnlineVoting;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class OnlineVotingService
{
    private static readonly JsonSerializerOptions PendingPayloadJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

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

            OnlineVotingInfo? existingVotingInfo = null;
            if (person != null)
            {
                existingVotingInfo = await _context.OnlineVotingInfos
                    .Where(ovi => ovi.ElectionGuid == dto.ElectionGuid && ovi.PersonGuid == person.PersonGuid)
                    .OrderByDescending(ovi => ovi.WhenStatus ?? ovi.WhenBallotCreated)
                    .FirstOrDefaultAsync();
            }

            if (existingVotingInfo != null && CannotChangeOnlineVote(existingVotingInfo))
            {
                await transaction.RollbackAsync();
                return (false, "voting.submit.alreadyProcessed");
            }

            var payloadJson = SerializePendingPayload(dto.Votes, dto.ListPool);

            if (existingVotingInfo != null)
            {
                var wrote = await TryWritePendingPayloadIfStillSubmittedAsync(
                    existingVotingInfo, payloadJson, now);
                if (!wrote)
                {
                    await transaction.RollbackAsync();
                    return (false, "voting.submit.alreadyProcessed");
                }
            }
            else
            {
                _context.OnlineVotingInfos.Add(new OnlineVotingInfo
                {
                    ElectionGuid = dto.ElectionGuid,
                    PersonGuid = person?.PersonGuid ?? Guid.NewGuid(),
                    WhenBallotCreated = now,
                    Status = OnlineBallotStatus.Submitted,
                    WhenStatus = now,
                    ListPool = payloadJson,
                    PoolLocked = true
                });
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

        var cannotChange = votingInfo != null && CannotChangeOnlineVote(votingInfo);
        var isProcessed = votingInfo != null && OnlineBallotStatus.IsProcessed(votingInfo.Status);
        var isProcessing = votingInfo != null && OnlineBallotStatus.IsProcessing(votingInfo.Status);
        var hasPending = votingInfo != null && OnlineBallotStatus.IsSubmitted(votingInfo.Status);

        var priorVotes = new List<OnlineVoteDto>();
        var listPool = new List<OnlinePoolEntryDto>();

        if ((hasPending || isProcessing) && TryReadPendingPayload(votingInfo!.ListPool, out var payload))
        {
            priorVotes = payload.Votes;
            listPool = payload.Pool;
        }
        else if (hasPending && votingInfo!.BallotGuid != null)
        {
            listPool = ParseLegacyPoolArray(votingInfo.ListPool);
            var storedVotes = await _context.Votes
                .Where(v => v.BallotGuid == votingInfo.BallotGuid)
                .OrderBy(v => v.PositionOnBallot)
                .Select(v => new
                {
                    v.PersonGuid,
                    v.OnlineVoteRaw,
                    v.PersonCombinedInfo,
                    v.PositionOnBallot
                })
                .ToListAsync();

            priorVotes = storedVotes.Select(v =>
            {
                var displayName = OnlineRawVote.Parse(v.OnlineVoteRaw).ToDisplayName();
                return new OnlineVoteDto
                {
                    PersonGuid = v.PersonGuid,
                    VoteName = !string.IsNullOrEmpty(displayName)
                        ? displayName
                        : v.PersonCombinedInfo,
                    PositionOnBallot = v.PositionOnBallot
                };
            }).ToList();
        }

        var onlineVoter = await _context.OnlineVoters
            .FirstOrDefaultAsync(ov => ov.VoterId == voterId);

        var hasVoted = person.HasOnlineBallot == true || hasPending || isProcessing || isProcessed;
        return new OnlineVoteStatusDto
        {
            HasVoted = hasVoted,
            WhenSubmitted = votingInfo?.WhenBallotCreated,
            Message = cannotChange
                ? "voting.status.alreadyProcessed"
                : hasVoted
                    ? "voting.status.alreadyVoted"
                    : "voting.status.notVoted",
            PriorVotes = priorVotes,
            ListPool = listPool,
            NotifyWhenProcessed = HasNotifyProcessedPreference(onlineVoter?.EmailCodes),
            CanChangeVote = !cannotChange
        };
    }

    /// <summary>
    /// True when the voter cannot change the vote: Accept-all has claimed the row
    /// (Processing), finished it (Processed), or a legacy submit-creates-ballot row
    /// still has BallotGuid. Do not null BallotGuid or revive the row.
    /// </summary>
    internal static bool CannotChangeOnlineVote(OnlineVotingInfo votingInfo)
    {
        return votingInfo.BallotGuid != null
               || OnlineBallotStatus.IsProcessed(votingInfo.Status)
               || OnlineBallotStatus.IsProcessing(votingInfo.Status);
    }

    /// <summary>
    /// Writes a new pending payload only while the row is still Submitted and has
    /// no BallotGuid. Relational providers use UPDATE … WHERE Status='Submitted'
    /// so a concurrent Accept-all that already set Processing or Processed cannot
    /// be clobbered by a Submit that loaded Submitted. Does not touch BallotGuid.
    /// </summary>
    internal async Task<bool> TryWritePendingPayloadIfStillSubmittedAsync(
        OnlineVotingInfo existingVotingInfo,
        string payloadJson,
        DateTimeOffset now)
    {
        if (_context.Database.IsRelational())
        {
            var updated = await _context.OnlineVotingInfos
                .Where(o => o.RowId == existingVotingInfo.RowId
                            && o.Status == OnlineBallotStatus.Submitted
                            && o.BallotGuid == null)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.WhenBallotCreated, now)
                    .SetProperty(o => o.WhenStatus, now)
                    .SetProperty(o => o.Status, OnlineBallotStatus.Submitted)
                    .SetProperty(o => o.ListPool, payloadJson)
                    .SetProperty(o => o.PoolLocked, true));
            return updated == 1;
        }

        if (CannotChangeOnlineVote(existingVotingInfo)
            || !OnlineBallotStatus.IsSubmitted(existingVotingInfo.Status))
        {
            return false;
        }

        existingVotingInfo.WhenBallotCreated = now;
        existingVotingInfo.WhenStatus = now;
        existingVotingInfo.Status = OnlineBallotStatus.Submitted;
        existingVotingInfo.ListPool = payloadJson;
        existingVotingInfo.PoolLocked = true;
        return true;
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

    private static string SerializePendingPayload(
        List<OnlineVoteDto> votes,
        List<OnlinePoolEntryDto> pool)
    {
        return JsonSerializer.Serialize(new PendingOnlineBallotPayload
        {
            Votes = votes ?? new List<OnlineVoteDto>(),
            Pool = pool ?? new List<OnlinePoolEntryDto>()
        }, PendingPayloadJson);
    }

    internal static bool TryReadPendingPayload(string? listPool, out PendingOnlineBallotPayload payload)
    {
        payload = new PendingOnlineBallotPayload();
        if (string.IsNullOrWhiteSpace(listPool))
        {
            return false;
        }

        var trimmed = listPool.TrimStart();
        if (trimmed.StartsWith('['))
        {
            return false;
        }

        try
        {
            var parsed = JsonSerializer.Deserialize<PendingOnlineBallotPayload>(listPool, PendingPayloadJson);
            if (parsed == null)
            {
                return false;
            }

            payload = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    internal static List<OnlinePoolEntryDto> ParseLegacyPoolArray(string? listPool)
    {
        if (string.IsNullOrWhiteSpace(listPool))
        {
            return new List<OnlinePoolEntryDto>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<OnlinePoolEntryDto>>(listPool, PendingPayloadJson)
                   ?? new List<OnlinePoolEntryDto>();
        }
        catch (JsonException)
        {
            return new List<OnlinePoolEntryDto>();
        }
    }
}
