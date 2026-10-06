using System.Data;
using System.Data.Common;
using Backend.Configuration;
using Backend.Context;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Backend.Services;

/// <summary>
/// Stores guest teller passcode failures in <see cref="TellerLoginLockout"/>.
/// The count is per election, not per IP.
/// On SQL Server one UPDATE statement increments the count and sets the lock,
/// so two concurrent failures cannot both write the same count.
/// </summary>
public class TellerLoginLockoutService : ITellerLoginLockoutService
{
    private readonly MainDbContext _context;
    private readonly TellerLoginProtectionOptions _options;

    /// <summary>
    /// Initializes the service with the database and lockout settings.
    /// </summary>
    public TellerLoginLockoutService(MainDbContext context, IOptions<TellerLoginProtectionOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    /// <inheritdoc />
    public async Task<bool> IsLockedAsync(Guid electionGuid, CancellationToken cancellationToken = default)
    {
        var lockedUntil = await _context.TellerLoginLockouts
            .AsNoTracking()
            .Where(row => row.ElectionGuid == electionGuid)
            .Select(row => row.LockedUntil)
            .FirstOrDefaultAsync(cancellationToken);

        return lockedUntil is DateTimeOffset until && until > DateTimeOffset.UtcNow;
    }

    /// <inheritdoc />
    public async Task<TellerPasscodeFailureResult> RecordPasscodeFailureAsync(
        Guid electionGuid,
        CancellationToken cancellationToken = default)
    {
        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                return await RecordOnceAsync(electionGuid, cancellationToken);
            }
            catch (Exception ex) when (attempt < 7 && IsDuplicateKeyOrDatabaseLocked(ex))
            {
                DetachLockoutEntries();
            }
        }

        throw new InvalidOperationException(
            $"Could not record the teller passcode failure for election {electionGuid}.");
    }

    /// <inheritdoc />
    public async Task ResetAsync(Guid electionGuid, CancellationToken cancellationToken = default)
    {
        var row = await _context.TellerLoginLockouts
            .FirstOrDefaultAsync(item => item.ElectionGuid == electionGuid, cancellationToken);
        if (row == null)
        {
            return;
        }

        row.ConsecutiveFailures = 0;
        row.LockedUntil = null;
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task<TellerPasscodeFailureResult> RecordOnceAsync(
        Guid electionGuid,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var threshold = _options.ResolvedMaxConsecutiveFailures;
        var lockUntil = now.AddMinutes(_options.ResolvedCooldownMinutes);

        if (_context.Database.IsSqlServer())
        {
            return await RecordOnSqlServerAsync(
                electionGuid,
                now,
                threshold,
                lockUntil,
                cancellationToken);
        }

        return await RecordOnOtherProvidersAsync(
            electionGuid,
            now,
            threshold,
            lockUntil,
            cancellationToken);
    }

    /// <summary>
    /// One UPDATE ... OUTPUT. The SET expressions read the row as it was before this
    /// statement. A lock that is still in the future is left in place.
    /// LockoutStarted is true only when this statement is the one that crossed the threshold.
    /// </summary>
    private async Task<TellerPasscodeFailureResult> RecordOnSqlServerAsync(
        Guid electionGuid,
        DateTimeOffset now,
        int threshold,
        DateTimeOffset lockUntil,
        CancellationToken cancellationToken)
    {
        const string sql = """
            UPDATE TellerLoginLockouts
            SET
                ConsecutiveFailures = CASE
                    WHEN LockedUntil IS NOT NULL AND LockedUntil <= @now THEN 1
                    ELSE ConsecutiveFailures + 1
                END,
                LockedUntil = CASE
                    WHEN LockedUntil IS NOT NULL AND LockedUntil > @now THEN LockedUntil
                    WHEN (
                        CASE
                            WHEN LockedUntil IS NOT NULL AND LockedUntil <= @now THEN 1
                            ELSE ConsecutiveFailures + 1
                        END
                    ) >= @threshold THEN @lockUntil
                    ELSE NULL
                END
            OUTPUT
                inserted.ConsecutiveFailures,
                inserted.LockedUntil,
                deleted.LockedUntil
            WHERE ElectionGuid = @electionGuid
            """;

        var updated = await ReadSqlServerUpdateAsync(sql, electionGuid, now, threshold, lockUntil, cancellationToken);
        if (updated != null)
        {
            return ToResult(updated.Value, now, threshold);
        }

        await InsertFirstFailureAsync(electionGuid, threshold, lockUntil, cancellationToken);
        return new TellerPasscodeFailureResult(
            IsLocked: threshold <= 1,
            LockoutStarted: threshold <= 1,
            LockedUntil: threshold <= 1 ? lockUntil : null,
            ConsecutiveFailures: 1);
    }

    /// <summary>
    /// SQLite has no OUTPUT deleted.* columns. The UPDATE only matches a row whose
    /// lock is missing or already expired, so a failure cannot clear a lock that is
    /// still active. A second concurrent statement then updates zero rows.
    /// That does not reproduce the SQL Server read-modify-write race.
    /// </summary>
    private async Task<TellerPasscodeFailureResult> RecordOnOtherProvidersAsync(
        Guid electionGuid,
        DateTimeOffset now,
        int threshold,
        DateTimeOffset lockUntil,
        CancellationToken cancellationToken)
    {
        // RETURNING reports the values this statement wrote. A later failure cannot
        // make this statement look like the one that crossed the threshold.
        const string sql = """
            UPDATE TellerLoginLockouts
            SET
                ConsecutiveFailures = CASE
                    WHEN LockedUntil IS NOT NULL AND LockedUntil <= @now THEN 1
                    ELSE ConsecutiveFailures + 1
                END,
                LockedUntil = CASE
                    WHEN (
                        CASE
                            WHEN LockedUntil IS NOT NULL AND LockedUntil <= @now THEN 1
                            ELSE ConsecutiveFailures + 1
                        END
                    ) >= @threshold THEN @lockUntil
                    ELSE NULL
                END
            WHERE ElectionGuid = @electionGuid
              AND (LockedUntil IS NULL OR LockedUntil <= @now)
            RETURNING ConsecutiveFailures, LockedUntil
            """;

        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var connection = _context.Database.GetDbConnection();
            await using (var pragma = connection.CreateCommand())
            {
                pragma.CommandText = "PRAGMA busy_timeout = 5000";
                pragma.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
                await pragma.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
            command.Parameters.Add(CreateMappedParameter(command, nameof(TellerLoginLockout.ElectionGuid), "@electionGuid", electionGuid));
            command.Parameters.Add(CreateMappedParameter(command, nameof(TellerLoginLockout.LockedUntil), "@now", now));
            command.Parameters.Add(CreateMappedParameter(command, nameof(TellerLoginLockout.LockedUntil), "@lockUntil", lockUntil));
            var thresholdParameter = command.CreateParameter();
            thresholdParameter.ParameterName = "@threshold";
            thresholdParameter.Value = threshold;
            command.Parameters.Add(thresholdParameter);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (await reader.ReadAsync(cancellationToken))
            {
                var count = reader.GetInt32(0);
                var insertedLock = ReadLockedUntil(reader, 1);
                var started = count >= threshold && insertedLock is DateTimeOffset until && until > now;
                return new TellerPasscodeFailureResult(
                    IsLocked: started,
                    LockoutStarted: started,
                    LockedUntil: started ? insertedLock : null,
                    ConsecutiveFailures: count);
            }
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }

        var existing = await _context.TellerLoginLockouts
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.ElectionGuid == electionGuid, cancellationToken);
        if (existing?.LockedUntil is DateTimeOffset active && active > now)
        {
            return new TellerPasscodeFailureResult(
                IsLocked: true,
                LockoutStarted: false,
                LockedUntil: active,
                ConsecutiveFailures: existing.ConsecutiveFailures);
        }

        if (existing != null)
        {
            throw new InvalidOperationException(
                $"Teller login lockout row for election {electionGuid} was not updated.");
        }

        await InsertFirstFailureAsync(electionGuid, threshold, lockUntil, cancellationToken);
        return new TellerPasscodeFailureResult(
            IsLocked: threshold <= 1,
            LockoutStarted: threshold <= 1,
            LockedUntil: threshold <= 1 ? lockUntil : null,
            ConsecutiveFailures: 1);
    }

    private async Task<SqlServerUpdateRow?> ReadSqlServerUpdateAsync(
        string sql,
        Guid electionGuid,
        DateTimeOffset now,
        int threshold,
        DateTimeOffset lockUntil,
        CancellationToken cancellationToken)
    {
        await _context.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            var connection = _context.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Transaction = _context.Database.CurrentTransaction?.GetDbTransaction();
            AddParameter(command, "@electionGuid", electionGuid);
            AddParameter(command, "@now", now);
            AddParameter(command, "@threshold", threshold);
            AddParameter(command, "@lockUntil", lockUntil);

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken))
            {
                return null;
            }

            var count = reader.GetInt32(0);
            var insertedLock = reader.IsDBNull(1) ? (DateTimeOffset?)null : reader.GetFieldValue<DateTimeOffset>(1);
            var deletedLock = reader.IsDBNull(2) ? (DateTimeOffset?)null : reader.GetFieldValue<DateTimeOffset>(2);
            return new SqlServerUpdateRow(count, insertedLock, deletedLock);
        }
        finally
        {
            await _context.Database.CloseConnectionAsync();
        }
    }

    private DbParameter CreateMappedParameter(
        DbCommand command,
        string propertyName,
        string parameterName,
        object? value)
    {
        var property = _context.Model.FindEntityType(typeof(TellerLoginLockout))!
            .FindProperty(propertyName)!;
        return property.GetRelationalTypeMapping().CreateParameter(command, parameterName, value);
    }

    private static DateTimeOffset? ReadLockedUntil(DbDataReader reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        return value switch
        {
            DateTimeOffset dateTimeOffset => dateTimeOffset,
            long ticks => new DateTimeOffset(ticks, TimeSpan.Zero),
            _ => throw new InvalidOperationException(
                $"Unexpected LockedUntil value type {value.GetType().FullName}.")
        };
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        if (value is DateTimeOffset)
        {
            parameter.DbType = DbType.DateTimeOffset;
        }

        command.Parameters.Add(parameter);
    }

    private static TellerPasscodeFailureResult ToResult(SqlServerUpdateRow row, DateTimeOffset now, int threshold)
    {
        var wasActive = row.DeletedLockedUntil is DateTimeOffset previous && previous > now;
        var isLocked = row.InsertedLockedUntil is DateTimeOffset current && current > now;
        return new TellerPasscodeFailureResult(
            IsLocked: isLocked,
            LockoutStarted: !wasActive && isLocked && row.ConsecutiveFailures >= threshold,
            LockedUntil: isLocked ? row.InsertedLockedUntil : null,
            ConsecutiveFailures: row.ConsecutiveFailures);
    }

    private async Task InsertFirstFailureAsync(
        Guid electionGuid,
        int threshold,
        DateTimeOffset lockUntil,
        CancellationToken cancellationToken)
    {
        _context.TellerLoginLockouts.Add(new TellerLoginLockout
        {
            ElectionGuid = electionGuid,
            ConsecutiveFailures = 1,
            LockedUntil = threshold <= 1 ? lockUntil : null
        });
        await _context.SaveChangesAsync(cancellationToken);
    }

    private void DetachLockoutEntries()
    {
        foreach (var entry in _context.ChangeTracker.Entries<TellerLoginLockout>().ToList())
        {
            entry.State = EntityState.Detached;
        }
    }

    private static bool IsDuplicateKeyOrDatabaseLocked(Exception exception)
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

            var number = current.GetType().GetProperty("Number")?.GetValue(current);
            if (number is int sqlNumber && sqlNumber is 2601 or 2627 or 1205)
            {
                return true;
            }
        }

        return false;
    }

    private readonly record struct SqlServerUpdateRow(
        int ConsecutiveFailures,
        DateTimeOffset? InsertedLockedUntil,
        DateTimeOffset? DeletedLockedUntil);
}
