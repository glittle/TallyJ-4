using System.Data;
using System.Data.Common;
using Backend.Configuration;
using Backend.Context;
using Backend.Entities;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace Backend.Services;

/// <summary>
/// Counts paid login-code sends per phone prefix.
/// On SQL Server one <c>UPDATE ... OUTPUT</c> takes the slot only while the sliding
/// estimate is under the cap, so two concurrent sends cannot both pass the last slot.
/// SQLite uses the same statement with <c>RETURNING</c>. Email never reaches the table.
/// </summary>
public class PhonePrefixSendLimiter : IPhonePrefixSendLimiter
{
    private readonly MainDbContext _context;
    private readonly AntiAbuseOptions _options;
    private readonly ICodeSendClock _clock;
    private readonly ILogger<PhonePrefixSendLimiter> _logger;

    /// <summary>
    /// Initializes the limiter.
    /// </summary>
    public PhonePrefixSendLimiter(
        MainDbContext context,
        IOptions<AntiAbuseOptions> options,
        ICodeSendClock clock,
        ILogger<PhonePrefixSendLimiter> logger)
    {
        _context = context;
        _options = options.Value;
        _clock = clock;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PhonePrefixSendResult> TryConsumeAsync(
        string channel,
        string destination,
        CancellationToken cancellationToken = default)
    {
        if (!PaidDestinationPhone.IsPaidChannel(channel))
        {
            return new PhonePrefixSendResult(true, null, null);
        }

        var digits = _options.ResolvedPhonePrefixDigits;
        if (!PhonePrefix.TryExtract(destination, digits, out var prefix))
        {
            _logger.LogInformation(
                "Paid login code skipped: destination did not parse for a prefix ({Channel})",
                channel);
            return new PhonePrefixSendResult(false, null, CodeSendBlockReason.PrefixUnparsed);
        }

        var windowSeconds = _options.ResolvedPhonePrefixWindowMinutes * 60L;
        var nowUnix = _clock.UtcNow.ToUnixTimeSeconds();
        var bucketStart = nowUnix / windowSeconds * windowSeconds;
        var previousBucketStart = bucketStart - windowSeconds;
        var elapsed = nowUnix - bucketStart;
        var weight = 1d - (elapsed / (double)windowSeconds);
        if (weight < 0d)
        {
            weight = 0d;
        }
        else if (weight > 1d)
        {
            weight = 1d;
        }

        var consumed = await TryConsumeSlotAsync(
            prefix,
            _options.ResolvedPhonePrefixSendLimit,
            bucketStart,
            previousBucketStart,
            weight,
            cancellationToken);

        if (!consumed)
        {
            _logger.LogInformation(
                "Paid login code skipped: prefix {Prefix} is at the sliding cap",
                prefix);
            return new PhonePrefixSendResult(false, prefix, CodeSendBlockReason.PrefixLimit);
        }

        return new PhonePrefixSendResult(true, prefix, null);
    }

    private async Task<bool> TryConsumeSlotAsync(
        string prefix,
        int limit,
        long bucketStart,
        long previousBucketStart,
        double weight,
        CancellationToken cancellationToken)
    {
        if (!_context.Database.IsRelational())
        {
            return await ConsumeInMemoryAsync(
                prefix,
                limit,
                bucketStart,
                previousBucketStart,
                weight,
                cancellationToken);
        }

        for (var attempt = 0; attempt < 8; attempt++)
        {
            try
            {
                var updated = await UpdateRelationalAsync(
                    prefix,
                    limit,
                    bucketStart,
                    previousBucketStart,
                    weight,
                    cancellationToken);
                Detach();
                if (updated.HasValue)
                {
                    return true;
                }

                var exists = await _context.PhonePrefixSendCounters
                    .AsNoTracking()
                    .AnyAsync(row => row.Prefix == prefix, cancellationToken);
                if (exists)
                {
                    return false;
                }

                _context.PhonePrefixSendCounters.Add(new PhonePrefixSendCounter
                {
                    Prefix = prefix,
                    BucketStartedUnix = bucketStart,
                    SendCount = 1,
                    PreviousCount = 0
                });
                await _context.SaveChangesAsync(cancellationToken);
                Detach();
                return true;
            }
            catch (Exception ex) when (attempt < 7 && IsDuplicateOrBusy(ex))
            {
                Detach();
            }
        }

        _logger.LogWarning(
            "Paid login code skipped: prefix counter could not be updated ({Prefix})",
            prefix);
        return false;
    }

    /// <summary>
    /// The SET expressions read the row as it was before this statement.
    /// A bucket older than the previous window is dropped. The previous window
    /// still counts, scaled by how much of it sits inside the sliding hour.
    /// </summary>
    private async Task<int?> UpdateRelationalAsync(
        string prefix,
        int limit,
        long bucketStart,
        long previousBucketStart,
        double weight,
        CancellationToken cancellationToken)
    {
        var sql = _context.Database.IsSqlServer()
            ? """
              UPDATE PhonePrefixSendCounters
              SET
                  PreviousCount = CASE
                      WHEN BucketStartedUnix = @bucketStart THEN PreviousCount
                      WHEN BucketStartedUnix = @previousBucketStart THEN SendCount
                      ELSE 0
                  END,
                  SendCount = CASE
                      WHEN BucketStartedUnix = @bucketStart THEN SendCount + 1
                      ELSE 1
                  END,
                  BucketStartedUnix = @bucketStart
              OUTPUT inserted.SendCount
              WHERE Prefix = @prefix
                AND (
                    (BucketStartedUnix = @bucketStart AND (PreviousCount * @weight + SendCount) < @limit)
                    OR (BucketStartedUnix = @previousBucketStart AND (SendCount * @weight) < @limit)
                    OR (BucketStartedUnix <> @bucketStart AND BucketStartedUnix <> @previousBucketStart)
                )
              """
            : """
              UPDATE PhonePrefixSendCounters
              SET
                  PreviousCount = CASE
                      WHEN BucketStartedUnix = @bucketStart THEN PreviousCount
                      WHEN BucketStartedUnix = @previousBucketStart THEN SendCount
                      ELSE 0
                  END,
                  SendCount = CASE
                      WHEN BucketStartedUnix = @bucketStart THEN SendCount + 1
                      ELSE 1
                  END,
                  BucketStartedUnix = @bucketStart
              WHERE Prefix = @prefix
                AND (
                    (BucketStartedUnix = @bucketStart AND (PreviousCount * @weight + SendCount) < @limit)
                    OR (BucketStartedUnix = @previousBucketStart AND (SendCount * @weight) < @limit)
                    OR (BucketStartedUnix <> @bucketStart AND BucketStartedUnix <> @previousBucketStart)
                )
              RETURNING SendCount
              """;

        return await ReadIntAsync(
            sql,
            command =>
            {
                command.Parameters.Add(CreateMappedParameter(
                    command, nameof(PhonePrefixSendCounter.Prefix), "@prefix", prefix));
                AddLong(command, "@bucketStart", bucketStart);
                AddLong(command, "@previousBucketStart", previousBucketStart);
                AddDouble(command, "@weight", weight);
                AddInt(command, "@limit", limit);
            },
            cancellationToken);
    }

    private async Task<bool> ConsumeInMemoryAsync(
        string prefix,
        int limit,
        long bucketStart,
        long previousBucketStart,
        double weight,
        CancellationToken cancellationToken)
    {
        var row = await _context.PhonePrefixSendCounters
            .FirstOrDefaultAsync(item => item.Prefix == prefix, cancellationToken);
        if (row == null)
        {
            _context.PhonePrefixSendCounters.Add(new PhonePrefixSendCounter
            {
                Prefix = prefix,
                BucketStartedUnix = bucketStart,
                SendCount = 1,
                PreviousCount = 0
            });
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        if (row.BucketStartedUnix == bucketStart)
        {
            if ((row.PreviousCount * weight) + row.SendCount >= limit)
            {
                return false;
            }

            row.SendCount += 1;
        }
        else if (row.BucketStartedUnix == previousBucketStart)
        {
            if (row.SendCount * weight >= limit)
            {
                return false;
            }

            row.PreviousCount = row.SendCount;
            row.SendCount = 1;
            row.BucketStartedUnix = bucketStart;
        }
        else
        {
            row.PreviousCount = 0;
            row.SendCount = 1;
            row.BucketStartedUnix = bucketStart;
        }

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

    private DbParameter CreateMappedParameter(
        DbCommand command,
        string propertyName,
        string parameterName,
        object? value)
    {
        var property = _context.Model.FindEntityType(typeof(PhonePrefixSendCounter))!
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

    private static void AddLong(DbCommand command, string name, long value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.Int64;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private static void AddDouble(DbCommand command, string name, double value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = DbType.Double;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private void Detach()
    {
        foreach (var entry in _context.ChangeTracker.Entries<PhonePrefixSendCounter>().ToList())
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
