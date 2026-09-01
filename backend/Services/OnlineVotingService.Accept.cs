using Backend.DTOs.OnlineVoting;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

public partial class OnlineVotingService
{
    /// <inheritdoc/>
    public async Task<AcceptAllOnlineBallotsSummaryDto?> GetAcceptAllSummaryAsync(Guid electionGuid)
    {
        var exists = await _context.Elections.AnyAsync(e => e.ElectionGuid == electionGuid);
        if (!exists)
        {
            return null;
        }

        var rows = await _context.OnlineVotingInfos
            .AsNoTracking()
            .Where(o => o.ElectionGuid == electionGuid)
            .Select(o => o.Status)
            .ToListAsync();

        return new AcceptAllOnlineBallotsSummaryDto
        {
            PendingCount = rows.Count(s =>
                string.Equals(s, OnlineBallotStatus.Submitted, StringComparison.OrdinalIgnoreCase)),
            ProcessedCount = rows.Count(s =>
                string.Equals(s, OnlineBallotStatus.Processed, StringComparison.OrdinalIgnoreCase))
        };
    }

    /// <inheritdoc/>
    public async Task<AcceptAllOnlineBallotsResultDto> AcceptAllPendingAsync(Guid electionGuid)
    {
        var election = await _context.Elections
            .FirstOrDefaultAsync(e => e.ElectionGuid == electionGuid);

        if (election == null)
        {
            return new AcceptAllOnlineBallotsResultDto
            {
                Success = false,
                MessageKey = "voting.submit.electionNotFound"
            };
        }

        if (election.ElectionStage == ElectionStage.Finalized)
        {
            return new AcceptAllOnlineBallotsResultDto
            {
                Success = false,
                MessageKey = "monitoring.acceptAll.finalized"
            };
        }

        if (!_acceptLock.TryEnter(electionGuid))
        {
            return new AcceptAllOnlineBallotsResultDto
            {
                Success = false,
                AlreadyInProgress = true,
                MessageKey = "monitoring.acceptAll.inProgress"
            };
        }

        try
        {
            var pendingIds = await _context.OnlineVotingInfos
                .Where(o => o.ElectionGuid == electionGuid
                            && o.Status == OnlineBallotStatus.Submitted)
                .OrderBy(o => o.PersonGuid)
                .Select(o => o.RowId)
                .ToListAsync();

            var accepted = 0;
            var skipped = 0;

            foreach (var rowId in pendingIds)
            {
                var created = await AcceptOnePendingAsync(electionGuid, rowId);
                if (created)
                {
                    accepted++;
                }
                else
                {
                    skipped++;
                }
            }

            var pendingRemaining = await _context.OnlineVotingInfos
                .CountAsync(o => o.ElectionGuid == electionGuid
                                 && o.Status == OnlineBallotStatus.Submitted);

            return new AcceptAllOnlineBallotsResultDto
            {
                Success = true,
                AcceptedCount = accepted,
                SkippedCount = skipped,
                PendingRemaining = pendingRemaining,
                MessageKey = accepted == 0 && skipped == 0
                    ? "monitoring.acceptAll.nonePending"
                    : "monitoring.acceptAll.complete"
            };
        }
        finally
        {
            _acceptLock.Exit(electionGuid);
        }
    }

    /// <summary>
    /// Claims one Submitted row with a compare-and-swap
    /// (<c>UPDATE … SET Status = Processed WHERE Status = Submitted</c>) and either
    /// creates a regular ballot from the pending payload or, for a legacy row that
    /// already has a ballot, wipes the online payload without creating a second
    /// ballot. Returns false if the UPDATE matched 0 rows or the row had nothing
    /// to accept. There is no stored Processing status; claim and ballot create
    /// share this transaction so a rollback restores Submitted.
    /// </summary>
    private async Task<bool> AcceptOnePendingAsync(Guid electionGuid, int rowId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;

        var votingInfo = await TryClaimSubmittedRowAsync(electionGuid, rowId);
        if (votingInfo == null)
        {
            await transaction.RollbackAsync();
            return false;
        }

        var hasPendingPayload = TryReadPendingPayload(votingInfo.ListPool, out var payload)
            && payload.Votes.Count > 0;
        var hasLegacyBallot = votingInfo.BallotGuid != null;

        if (hasLegacyBallot)
        {
            // Already a regular ballot from the previous submit-creates-ballot path.
            // Accept only wipes the online payload and unlinks; do not create another.
        }
        else if (hasPendingPayload)
        {
            var ballot = await CreateRegularBallotFromPendingVotesAsync(electionGuid, payload.Votes);
            if (ballot == null)
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
        else
        {
            await transaction.RollbackAsync();
            return false;
        }

        votingInfo.Status = OnlineBallotStatus.Processed;
        votingInfo.WhenStatus = now;
        votingInfo.WhenBallotCreated = now;
        votingInfo.ListPool = null;
        votingInfo.PoolLocked = null;
        votingInfo.BallotGuid = null;
        votingInfo.HistoryStatus = AppendProcessedHistory(votingInfo.HistoryStatus, now);

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        return true;
    }

    /// <summary>
    /// Atomically claims a Submitted row by setting Status to Processed only when
    /// it is still Submitted. 0 rows updated means another Accept-all (or this
    /// host after a retry) already claimed it. In-memory tests have no SQL UPDATE,
    /// so they assign Status on the tracked entity instead.
    /// </summary>
    private async Task<OnlineVotingInfo?> TryClaimSubmittedRowAsync(Guid electionGuid, int rowId)
    {
        if (_context.Database.IsRelational())
        {
            var claimed = await _context.OnlineVotingInfos
                .Where(o => o.RowId == rowId
                            && o.ElectionGuid == electionGuid
                            && o.Status == OnlineBallotStatus.Submitted)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.Status, OnlineBallotStatus.Processed));
            if (claimed == 0)
            {
                return null;
            }
        }

        var votingInfo = await _context.OnlineVotingInfos
            .FirstOrDefaultAsync(o => o.RowId == rowId && o.ElectionGuid == electionGuid);
        if (votingInfo == null)
        {
            return null;
        }

        if (!_context.Database.IsRelational())
        {
            if (!string.Equals(votingInfo.Status, OnlineBallotStatus.Submitted, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            votingInfo.Status = OnlineBallotStatus.Processed;
            return votingInfo;
        }

        if (!string.Equals(votingInfo.Status, OnlineBallotStatus.Processed, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return votingInfo;
    }

    private async Task<Ballot?> CreateRegularBallotFromPendingVotesAsync(
        Guid electionGuid,
        List<OnlineVoteDto> votes)
    {
        var location = await OnlineLocationHelper.EnsureExistsAsync(_context, electionGuid);
        var nextBallotNum = await SpecialBallotNumbering.RepairOnlineAndGetNextAsync(
            _context, location.LocationGuid);
        var now = DateTimeOffset.UtcNow;

        var ballot = new Ballot
        {
            LocationGuid = location.LocationGuid,
            BallotGuid = Guid.NewGuid(),
            StatusCode = BallotStatus.Ok,
            ComputerCode = ComputerCodeHelper.Online,
            BallotNumAtComputer = nextBallotNum,
            BallotCode = $"{ComputerCodeHelper.Online}{nextBallotNum}",
            Teller1 = "Online",
            DateCreated = now,
            DateUpdated = now,
            RowVersion = new byte[8]
        };

        _context.Ballots.Add(ballot);
        await _context.SaveChangesAsync();

        foreach (var voteDto in votes.OrderBy(v => v.PositionOnBallot))
        {
            var hasPerson = voteDto.PersonGuid.HasValue;
            var hasFreeText = !string.IsNullOrWhiteSpace(voteDto.VoteName);
            var rawVote = hasFreeText && !hasPerson
                ? OnlineRawVote.Parse(voteDto.VoteName)
                : null;
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
                OnlineVoteRaw = rawVote?.ToJson(),
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
            else if (rawVote != null)
            {
                vote.PersonCombinedInfo = rawVote.ToDisplayName();
            }

            _context.Votes.Add(vote);
        }

        await _context.SaveChangesAsync();
        ballot.Location = location;
        await BallotStatusRefresher.RefreshAsync(_context, ballot, _logger);
        await _context.SaveChangesAsync();
        return ballot;
    }

    private static string AppendProcessedHistory(string? existing, DateTimeOffset when)
    {
        var entry = $"{OnlineBallotStatus.Processed}|{when:O}";
        return string.IsNullOrEmpty(existing) ? entry : $"{existing};{entry}";
    }
}
