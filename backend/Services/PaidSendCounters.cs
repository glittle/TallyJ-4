using System.Data;
using System.Data.Common;
using Backend.Context;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;

namespace Backend.Services;

/// <summary>
/// Increments paid-send counters with one UPDATE statement so two concurrent sends
/// cannot both pass the same remaining allowance.
/// SQL Server uses UPDATE ... OUTPUT. SQLite uses UPDATE ... RETURNING.
/// The in-memory provider used by some unit tests updates the tracked row instead.
/// </summary>
public class PaidSendCounters
{
    private readonly MainDbContext _context;

    /// <summary>
    /// Initializes the counter with the database that stores the rows.
    /// </summary>
    public PaidSendCounters(MainDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Adds one to the election allowance counter when it is still under <paramref name="allowance"/>.
    /// Returns false when the allowance is already used up.
    /// </summary>
    public Task<bool> TryConsumeElectionAsync(
        Guid electionGuid,
        int allowance,
        CancellationToken cancellationToken = default)
    {
        return TryConsumeAsync(
            allowance,
            () => ConsumeElectionRelationalAsync(electionGuid, allowance, cancellationToken),
            () => ConsumeElectionInMemoryAsync(electionGuid, allowance, cancellationToken));
    }

    /// <summary>
    /// Subtracts one from the election counter after a later owner-cap check failed.
    /// The election allowance is not left consumed when the send is refused.
    /// </summary>
    public async Task RefundElectionAsync(Guid electionGuid, CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational())
        {
            var row = await _context.ElectionSendControls
                .FirstOrDefaultAsync(item => item.ElectionGuid == electionGuid, cancellationToken);
            if (row != null && row.PaidSendsUsed > 0)
            {
                row.PaidSendsUsed -= 1;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        const string sql = """
            UPDATE ElectionSendControls
            SET PaidSendsUsed = PaidSendsUsed - 1
            WHERE ElectionGuid = @electionGuid AND PaidSendsUsed > 0
            """;

        await ExecuteNonQueryAsync(
            sql,
            command =>
            {
                command.Parameters.Add(CreateMappedParameter<ElectionSendControl>(
                    command, nameof(ElectionSendControl.ElectionGuid), "@electionGuid", electionGuid));
            },
            cancellationToken);
        DetachCounters();
    }

    /// <summary>
    /// Subtracts one from the owner-day counter after a later prefix check refused the send.
    /// The daily cap is not left consumed when the send is refused.
    /// </summary>
    public async Task RefundOwnerDayAsync(
        Guid userId,
        DateOnly utcDate,
        CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational())
        {
            var row = await _context.OwnerDailyPaidSends
                .FirstOrDefaultAsync(item => item.UserId == userId && item.UtcDate == utcDate, cancellationToken);
            if (row != null && row.SendCount > 0)
            {
                row.SendCount -= 1;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return;
        }

        const string sql = """
            UPDATE OwnerDailyPaidSends
            SET SendCount = SendCount - 1
            WHERE UserId = @userId AND UtcDate = @utcDate AND SendCount > 0
            """;

        await ExecuteNonQueryAsync(
            sql,
            command =>
            {
                command.Parameters.Add(CreateMappedParameter<OwnerDailyPaidSend>(
                    command, nameof(OwnerDailyPaidSend.UserId), "@userId", userId));
                command.Parameters.Add(CreateMappedParameter<OwnerDailyPaidSend>(
                    command, nameof(OwnerDailyPaidSend.UtcDate), "@utcDate", utcDate));
            },
            cancellationToken);
        DetachCounters();
    }

    /// <summary>
    /// Adds one to the owner's count for <paramref name="utcDate"/> when it is still under <paramref name="cap"/>.
    /// </summary>
    public Task<bool> TryConsumeOwnerDayAsync(
        Guid userId,
        DateOnly utcDate,
        int cap,
        CancellationToken cancellationToken = default)
    {
        return TryConsumeAsync(
            cap,
            () => ConsumeOwnerRelationalAsync(userId, utcDate, cap, cancellationToken),
            () => ConsumeOwnerInMemoryAsync(userId, utcDate, cap, cancellationToken));
    }

    /// <summary>
    /// Stamps <see cref="OwnerSendControl.FirstPaidSendAt"/> when it is still null.
    /// Returns true only for the call that writes the stamp.
    /// </summary>
    public async Task<bool> TryStampFirstPaidSendAsync(
        Guid userId,
        DateTimeOffset now,
        CancellationToken cancellationToken = default)
    {
        if (!_context.Database.IsRelational())
        {
            var row = await _context.OwnerSendControls
                .FirstOrDefaultAsync(item => item.UserId == userId, cancellationToken);
            if (row == null || row.FirstPaidSendAt != null)
            {
                return false;
            }

            row.FirstPaidSendAt = now;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        const string sql = """
            UPDATE OwnerSendControls
            SET FirstPaidSendAt = @now
            WHERE UserId = @userId AND FirstPaidSendAt IS NULL
            """;

        var updated = await ExecuteNonQueryAsync(
            sql,
            command =>
            {
                command.Parameters.Add(CreateMappedParameter<OwnerSendControl>(
                    command, nameof(OwnerSendControl.UserId), "@userId", userId));
                command.Parameters.Add(CreateMappedParameter<OwnerSendControl>(
                    command, nameof(OwnerSendControl.FirstPaidSendAt), "@now", now));
            },
            cancellationToken);
        DetachOwnerControls();
        return updated == 1;
    }

    private async Task<bool> TryConsumeAsync(
        int limit,
        Func<Task<bool?>> relational,
        Func<Task<bool>> inMemory)
    {
        if (limit < 1)
        {
            return false;
        }

        if (!_context.Database.IsRelational())
        {
            return await inMemory();
        }

        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                var consumed = await relational();
                DetachCounters();
                if (consumed.HasValue)
                {
                    return consumed.Value;
                }
            }
            catch (Exception ex) when (attempt < 7 && IsDuplicateOrBusy(ex))
            {
                DetachCounters();
            }
        }

        return false;
    }

    /// <summary>
    /// Returns true when this statement consumed a slot, false when the row is already at the cap,
    /// and null when there is no row yet.
    /// </summary>
    private async Task<bool?> ConsumeElectionRelationalAsync(
        Guid electionGuid,
        int allowance,
        CancellationToken cancellationToken)
    {
        var sql = _context.Database.IsSqlServer()
            ? """
              UPDATE ElectionSendControls
              SET PaidSendsUsed = PaidSendsUsed + 1
              OUTPUT inserted.PaidSendsUsed
              WHERE ElectionGuid = @electionGuid AND PaidSendsUsed < @allowance
              """
            : """
              UPDATE ElectionSendControls
              SET PaidSendsUsed = PaidSendsUsed + 1
              WHERE ElectionGuid = @electionGuid AND PaidSendsUsed < @allowance
              RETURNING PaidSendsUsed
              """;

        var updated = await ReadIntAsync(
            sql,
            command =>
            {
                command.Parameters.Add(CreateMappedParameter<ElectionSendControl>(
                    command, nameof(ElectionSendControl.ElectionGuid), "@electionGuid", electionGuid));
                AddInt(command, "@allowance", allowance);
            },
            cancellationToken);

        if (updated.HasValue)
        {
            return true;
        }

        var exists = await _context.ElectionSendControls
            .AsNoTracking()
            .AnyAsync(item => item.ElectionGuid == electionGuid, cancellationToken);
        if (exists)
        {
            return false;
        }

        _context.ElectionSendControls.Add(new ElectionSendControl
        {
            ElectionGuid = electionGuid,
            PaidSendsUsed = 1
        });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool?> ConsumeOwnerRelationalAsync(
        Guid userId,
        DateOnly utcDate,
        int cap,
        CancellationToken cancellationToken)
    {
        var sql = _context.Database.IsSqlServer()
            ? """
              UPDATE OwnerDailyPaidSends
              SET SendCount = SendCount + 1
              OUTPUT inserted.SendCount
              WHERE UserId = @userId AND UtcDate = @utcDate AND SendCount < @cap
              """
            : """
              UPDATE OwnerDailyPaidSends
              SET SendCount = SendCount + 1
              WHERE UserId = @userId AND UtcDate = @utcDate AND SendCount < @cap
              RETURNING SendCount
              """;

        var updated = await ReadIntAsync(
            sql,
            command =>
            {
                command.Parameters.Add(CreateMappedParameter<OwnerDailyPaidSend>(
                    command, nameof(OwnerDailyPaidSend.UserId), "@userId", userId));
                command.Parameters.Add(CreateMappedParameter<OwnerDailyPaidSend>(
                    command, nameof(OwnerDailyPaidSend.UtcDate), "@utcDate", utcDate));
                AddInt(command, "@cap", cap);
            },
            cancellationToken);

        if (updated.HasValue)
        {
            return true;
        }

        var exists = await _context.OwnerDailyPaidSends
            .AsNoTracking()
            .AnyAsync(item => item.UserId == userId && item.UtcDate == utcDate, cancellationToken);
        if (exists)
        {
            return false;
        }

        _context.OwnerDailyPaidSends.Add(new OwnerDailyPaidSend
        {
            UserId = userId,
            UtcDate = utcDate,
            SendCount = 1
        });
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> ConsumeElectionInMemoryAsync(
        Guid electionGuid,
        int allowance,
        CancellationToken cancellationToken)
    {
        var row = await _context.ElectionSendControls
            .FirstOrDefaultAsync(item => item.ElectionGuid == electionGuid, cancellationToken);
        if (row == null)
        {
            _context.ElectionSendControls.Add(new ElectionSendControl
            {
                ElectionGuid = electionGuid,
                PaidSendsUsed = 1
            });
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        if (row.PaidSendsUsed >= allowance)
        {
            return false;
        }

        row.PaidSendsUsed += 1;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<bool> ConsumeOwnerInMemoryAsync(
        Guid userId,
        DateOnly utcDate,
        int cap,
        CancellationToken cancellationToken)
    {
        var row = await _context.OwnerDailyPaidSends
            .FirstOrDefaultAsync(item => item.UserId == userId && item.UtcDate == utcDate, cancellationToken);
        if (row == null)
        {
            _context.OwnerDailyPaidSends.Add(new OwnerDailyPaidSend
            {
                UserId = userId,
                UtcDate = utcDate,
                SendCount = 1
            });
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        if (row.SendCount >= cap)
        {
            return false;
        }

        row.SendCount += 1;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task<int?> ReadIntAsync(
        string sql,
        Action<DbCommand> addParameters,
        CancellationToken cancellationToken)
    {
        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var connection = _context.Database.GetDbConnection();
            await SetSqliteBusyTimeoutAsync(connection, cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
            addParameters(command);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            return Convert.ToInt32(result);
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    private async Task<int> ExecuteNonQueryAsync(
        string sql,
        Action<DbCommand> addParameters,
        CancellationToken cancellationToken)
    {
        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var connection = _context.Database.GetDbConnection();
            await SetSqliteBusyTimeoutAsync(connection, cancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
            addParameters(command);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    private static async Task SetSqliteBusyTimeoutAsync(DbConnection connection, CancellationToken cancellationToken)
    {
        if (!string.Equals(connection.GetType().Name, "SqliteConnection", StringComparison.Ordinal))
        {
            return;
        }

        await using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA busy_timeout = 5000";
        await pragma.ExecuteNonQueryAsync(cancellationToken);
    }

    private DbParameter CreateMappedParameter<TEntity>(
        DbCommand command,
        string propertyName,
        string parameterName,
        object? value)
        where TEntity : class
    {
        var property = _context.Model.FindEntityType(typeof(TEntity))!
            .FindProperty(propertyName)!;
        return property.GetRelationalTypeMapping().CreateParameter(command, parameterName, value);
    }

    private static void AddInt(DbCommand command, string name, int value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.Int32;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private void DetachCounters()
    {
        foreach (var entry in _context.ChangeTracker.Entries<ElectionSendControl>().ToList())
        {
            entry.State = EntityState.Detached;
        }

        foreach (var entry in _context.ChangeTracker.Entries<OwnerDailyPaidSend>().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private void DetachOwnerControls()
    {
        foreach (var entry in _context.ChangeTracker.Entries<OwnerSendControl>().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static bool IsDuplicateOrBusy(Exception exception)
    {
        for (var current = exception; current != null; current = current.InnerException)
        {
            if (current.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("database is locked", StringComparison.OrdinalIgnoreCase)
                || current.Message.Contains("SQLITE_BUSY", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
