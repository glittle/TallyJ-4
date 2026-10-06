using Backend.Configuration;
using Backend.Context;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Services;

/// <summary>
/// Stores guest teller passcode failures in <see cref="TellerLoginLockout"/>.
/// The count is per election, not per IP.
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
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var result = await AddFailureAsync(electionGuid, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }
            catch (DbUpdateException) when (attempt == 0)
            {
                await transaction.RollbackAsync(cancellationToken);
                _context.ChangeTracker.Clear();
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

    private async Task<TellerPasscodeFailureResult> AddFailureAsync(
        Guid electionGuid,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var row = await _context.TellerLoginLockouts
            .FirstOrDefaultAsync(item => item.ElectionGuid == electionGuid, cancellationToken);

        if (row == null)
        {
            row = new TellerLoginLockout
            {
                ElectionGuid = electionGuid
            };
            _context.TellerLoginLockouts.Add(row);
        }
        else if (row.LockedUntil is DateTimeOffset lockedUntil && lockedUntil > now)
        {
            return new TellerPasscodeFailureResult(
                IsLocked: true,
                LockoutStarted: false,
                LockedUntil: lockedUntil,
                ConsecutiveFailures: row.ConsecutiveFailures);
        }
        else if (row.LockedUntil != null)
        {
            row.ConsecutiveFailures = 0;
            row.LockedUntil = null;
        }

        row.ConsecutiveFailures++;

        var threshold = _options.ResolvedMaxConsecutiveFailures;
        if (row.ConsecutiveFailures < threshold)
        {
            return new TellerPasscodeFailureResult(
                IsLocked: false,
                LockoutStarted: false,
                LockedUntil: null,
                ConsecutiveFailures: row.ConsecutiveFailures);
        }

        var until = now.AddMinutes(_options.ResolvedCooldownMinutes);
        row.LockedUntil = until;
        return new TellerPasscodeFailureResult(
            IsLocked: true,
            LockoutStarted: true,
            LockedUntil: until,
            ConsecutiveFailures: row.ConsecutiveFailures);
    }
}
