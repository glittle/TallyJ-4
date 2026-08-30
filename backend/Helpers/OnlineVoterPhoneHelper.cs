using Backend.Context;
using Backend.Entities;
using Microsoft.EntityFrameworkCore;

namespace Backend.Helpers;

/// <summary>
/// Ensures a global phone <see cref="OnlineVoter"/> row exists when a Person phone is written.
/// Does not record registration or login — those stay null until real auth use.
/// </summary>
public static class OnlineVoterPhoneHelper
{
    /// <summary>
    /// <see cref="OnlineVoter.VoterIdType"/> for a phone identifier (E.164 as stored on Person).
    /// </summary>
    public const string PhoneVoterIdType = "P";

    /// <summary>
    /// If <paramref name="phone"/> is non-whitespace, add an <see cref="OnlineVoter"/> row
    /// with <see cref="PhoneVoterIdType"/> when none exists for that <see cref="OnlineVoter.VoterId"/>.
    /// Does not call <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.
    /// Does not change <see cref="OnlineVoter.WhenRegistered"/>, <see cref="OnlineVoter.WhenLastLogin"/>,
    /// or <see cref="OnlineVoter.SmsStatus"/> on an existing row.
    /// </summary>
    public static Task EnsureOnlineVoterForPhoneAsync(
        MainDbContext context,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        return EnsureOnlineVotersForPhonesAsync(context, [phone], cancellationToken);
    }

    /// <summary>
    /// Same as <see cref="EnsureOnlineVoterForPhoneAsync"/> for each distinct non-whitespace phone.
    /// Used by people import so one lookup covers a batch.
    /// </summary>
    public static async Task EnsureOnlineVotersForPhonesAsync(
        MainDbContext context,
        IEnumerable<string?> phones,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(phones);

        var distinctPhones = phones
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p!)
            .Distinct()
            .ToList();

        if (distinctPhones.Count == 0)
        {
            return;
        }

        var alreadyHave = await context.OnlineVoters
            .Where(ov => distinctPhones.Contains(ov.VoterId))
            .Select(ov => ov.VoterId)
            .ToListAsync(cancellationToken);

        var have = new HashSet<string>(alreadyHave, StringComparer.Ordinal);
        foreach (var local in context.OnlineVoters.Local)
        {
            have.Add(local.VoterId);
        }

        foreach (var phone in distinctPhones)
        {
            if (!have.Add(phone))
            {
                continue;
            }

            context.OnlineVoters.Add(new OnlineVoter
            {
                VoterId = phone,
                VoterIdType = PhoneVoterIdType,
                WhenRegistered = null
            });
        }
    }
}
