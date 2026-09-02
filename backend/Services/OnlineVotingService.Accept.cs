using Backend;
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
                OnlineBallotStatus.IsSubmitted(s) || OnlineBallotStatus.IsProcessing(s)),
            ProcessedCount = rows.Count(OnlineBallotStatus.IsProcessed)
        };
    }

    /// <inheritdoc/>
    public async Task<AcceptAllOnlineBallotsResultDto> AcceptAllPendingAsync(
        Guid electionGuid,
        string? acceptedByUserId = null)
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
            var countsBefore = await CountPendingAndAcceptedAsync(electionGuid);

            // Pass 1: load the expected set, then persist Processing so another
            // server sharing this database can see the claim.
            var expectedIds = await _context.OnlineVotingInfos
                .Where(o => o.ElectionGuid == electionGuid
                            && (o.Status == OnlineBallotStatus.Submitted
                                || o.Status == OnlineBallotStatus.Processing))
                .OrderBy(o => o.PersonGuid)
                .Select(o => o.RowId)
                .ToListAsync();

            await ClaimSubmittedRowsAsProcessingAsync(electionGuid, expectedIds);

            var accepted = 0;
            var skipped = 0;

            // Pass 2: process that expected set only while each row is still Processing.
            foreach (var rowId in expectedIds)
            {
                var created = await ProcessIfStillProcessingAsync(electionGuid, rowId);
                if (created)
                {
                    accepted++;
                }
                else
                {
                    skipped++;
                }
            }

            var countsAfter = await CountPendingAndAcceptedAsync(electionGuid);

            await WriteAcceptAllAuditAsync(
                electionGuid,
                acceptedByUserId,
                countsBefore.Pending,
                countsBefore.Accepted,
                countsAfter.Pending,
                countsAfter.Accepted);

            return new AcceptAllOnlineBallotsResultDto
            {
                Success = true,
                AcceptedCount = accepted,
                SkippedCount = skipped,
                PendingRemaining = countsAfter.Pending,
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

    private async Task<(int Pending, int Accepted)> CountPendingAndAcceptedAsync(Guid electionGuid)
    {
        var rows = await _context.OnlineVotingInfos
            .AsNoTracking()
            .Where(o => o.ElectionGuid == electionGuid)
            .Select(o => o.Status)
            .ToListAsync();

        return (
            rows.Count(s => OnlineBallotStatus.IsSubmitted(s) || OnlineBallotStatus.IsProcessing(s)),
            rows.Count(OnlineBallotStatus.IsProcessed));
    }

    /// <summary>
    /// Persists one operational audit row for a completed Accept-all run.
    /// Stores the teller user id and optional display name, not voter contact
    /// details. Called only after the run finishes (including 0 accepted).
    /// </summary>
    private async Task WriteAcceptAllAuditAsync(
        Guid electionGuid,
        string? acceptedByUserId,
        int pendingBefore,
        int acceptedBefore,
        int pendingAfter,
        int acceptedAfter)
    {
        string? acceptedByDisplayName = null;
        if (!string.IsNullOrWhiteSpace(acceptedByUserId))
        {
            acceptedByDisplayName = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == acceptedByUserId)
                .Select(u => u.DisplayName)
                .FirstOrDefaultAsync();
        }

        var metadata = AcceptAllOnlineBallotsAudit.FormatMetadata(
            pendingBefore,
            acceptedBefore,
            pendingAfter,
            acceptedAfter,
            acceptedByDisplayName);

        _context.SecurityAuditLogs.Add(new SecurityAuditLog
        {
            Timestamp = DateTimeOffset.UtcNow,
            EventType = SecurityEventType.OperationalActivity,
            UserId = acceptedByUserId,
            ElectionGuid = electionGuid,
            Details = AcceptAllOnlineBallotsAudit.FormatDetails(
                pendingBefore,
                acceptedBefore,
                pendingAfter,
                acceptedAfter),
            Severity = SecurityEventSeverity.Info,
            MetadataJson = System.Text.Json.JsonSerializer.Serialize(metadata)
        });

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Persists Processing on every still-Submitted row in the expected set.
    /// Committed before ballot create so another server sharing the database can
    /// see the claim. 0 rows updated on a given row means another worker already
    /// moved it. Already-Processing rows (a crashed previous run) are left as-is
    /// for pass 2. In-memory tests assign Status on the tracked entity instead.
    /// </summary>
    private async Task ClaimSubmittedRowsAsProcessingAsync(Guid electionGuid, List<int> expectedIds)
    {
        if (expectedIds.Count == 0)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;
        if (_context.Database.IsRelational())
        {
            await _context.OnlineVotingInfos
                .Where(o => o.ElectionGuid == electionGuid
                            && o.Status == OnlineBallotStatus.Submitted
                            && expectedIds.Contains(o.RowId))
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.Status, OnlineBallotStatus.Processing)
                    .SetProperty(o => o.WhenStatus, now));
            _context.ChangeTracker.Clear();
            return;
        }

        var rows = await _context.OnlineVotingInfos
            .Where(o => o.ElectionGuid == electionGuid
                        && o.Status == OnlineBallotStatus.Submitted
                        && expectedIds.Contains(o.RowId))
            .ToListAsync();
        foreach (var row in rows)
        {
            row.Status = OnlineBallotStatus.Processing;
            row.WhenStatus = now;
        }

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Pass 2: in a transaction, take the row only while Status is still Processing
    /// (<c>UPDATE … SET Status = Processed WHERE Status = Processing</c>), then
    /// create the regular ballot (or unlink a legacy row) and wipe the payload.
    /// 0 rows updated means another server already took it. A rollback restores
    /// Processing so a later Accept-all can retry.
    /// </summary>
    private async Task<bool> ProcessIfStillProcessingAsync(Guid electionGuid, int rowId)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync();
        var now = DateTimeOffset.UtcNow;

        var votingInfo = await TryTakeProcessingRowAsync(electionGuid, rowId);
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
    /// Atomically takes a Processing row by setting Status to Processed only when
    /// it is still Processing. In-memory tests assign Status on the tracked entity.
    /// </summary>
    private async Task<OnlineVotingInfo?> TryTakeProcessingRowAsync(Guid electionGuid, int rowId)
    {
        if (_context.Database.IsRelational())
        {
            var taken = await _context.OnlineVotingInfos
                .Where(o => o.RowId == rowId
                            && o.ElectionGuid == electionGuid
                            && o.Status == OnlineBallotStatus.Processing)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(o => o.Status, OnlineBallotStatus.Processed));
            if (taken == 0)
            {
                return null;
            }

            _context.ChangeTracker.Clear();
        }

        var votingInfo = await _context.OnlineVotingInfos
            .FirstOrDefaultAsync(o => o.RowId == rowId && o.ElectionGuid == electionGuid);
        if (votingInfo == null)
        {
            return null;
        }

        if (!_context.Database.IsRelational())
        {
            if (!OnlineBallotStatus.IsProcessing(votingInfo.Status))
            {
                return null;
            }

            votingInfo.Status = OnlineBallotStatus.Processed;
            return votingInfo;
        }

        if (!OnlineBallotStatus.IsProcessed(votingInfo.Status))
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
