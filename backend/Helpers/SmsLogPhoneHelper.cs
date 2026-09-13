using Backend.Context;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers;

/// <summary>
/// Recent <see cref="SmsLog"/> rows for a Person phone string.
/// Lookup is by phone (+/- variant), not by PersonGuid or election.
/// </summary>
public static class SmsLogPhoneHelper
{
    /// <summary>
    /// Newest rows returned on person detail. Enough for a teller glance.
    /// </summary>
    public const int RecentLimit = 5;

    /// <summary>
    /// Newest <see cref="SmsLog"/> rows whose <see cref="SmsLog.Phone"/> matches
    /// <paramref name="phone"/> or the +/- E.164 variant
    /// (<see cref="TwilioSmsStatusHelper.VoterIdLookupKeys"/>).
    /// Does not look up by PersonGuid or ElectionGuid (verification SMS often has neither).
    /// Does not create rows. Empty when the phone is whitespace or no logs match.
    /// </summary>
    public static async Task<List<SmsLog>> FindRecentForPhoneAsync(
        MainDbContext context,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var keys = TwilioSmsStatusHelper.VoterIdLookupKeys(phone);
        if (keys.Count == 0)
        {
            return [];
        }

        return await context.SmsLogs
            .AsNoTracking()
            .Where(sl => keys.Contains(sl.Phone))
            .OrderByDescending(sl => sl.SentDate)
            .ThenByDescending(sl => sl.RowId)
            .Take(RecentLimit)
            .ToListAsync(cancellationToken);
    }
}
