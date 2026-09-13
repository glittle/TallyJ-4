using Backend.Context;
using Backend.DTOs.People;
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
    /// <see cref="OnlineVoter.VoterIdType"/> for a phone identifier (phone string as stored on Person).
    /// </summary>
    public const string PhoneVoterIdType = "P";

    /// <summary>
    /// If <paramref name="phone"/> is non-whitespace, add an <see cref="OnlineVoter"/> row
    /// with <see cref="PhoneVoterIdType"/> when no row exists for that <see cref="OnlineVoter.VoterId"/>.
    /// Lookup is by <see cref="OnlineVoter.VoterId"/> only: <c>IX_OnlineVoter_Id</c> is unique on
    /// that column, so a second row cannot be inserted if any type already owns the string.
    /// If the existing row is not <see cref="PhoneVoterIdType"/>, it is left unchanged
    /// (no convert, no wipe, no second row).
    /// Does not call <see cref="DbContext.SaveChangesAsync(CancellationToken)"/>.
    /// Does not change <see cref="OnlineVoter.WhenRegistered"/>, <see cref="OnlineVoter.WhenLastLogin"/>,
    /// <see cref="OnlineVoter.SmsStatus"/>, or <see cref="OnlineVoter.WhatsAppStatus"/> on an existing row.
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

    /// <summary>
    /// The phone OnlineVoter row for this Person phone string:
    /// <see cref="OnlineVoter.VoterId"/> equals <paramref name="phone"/> and
    /// <see cref="OnlineVoter.VoterIdType"/> is <see cref="PhoneVoterIdType"/>.
    /// Does not look up by VoterId alone. A non-P row occupying that VoterId is not returned.
    /// </summary>
    public static async Task<OnlineVoter?> FindPhoneOnlineVoterAsync(
        MainDbContext context,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        return await context.OnlineVoters
            .AsNoTracking()
            .FirstOrDefaultAsync(
                ov => ov.VoterId == phone && ov.VoterIdType == PhoneVoterIdType,
                cancellationToken);
    }

    /// <summary>
    /// Tracked phone OnlineVoter row for this <paramref name="phone"/>:
    /// <see cref="OnlineVoter.VoterId"/> equals the phone and
    /// <see cref="OnlineVoter.VoterIdType"/> is <see cref="PhoneVoterIdType"/>.
    /// Same predicate as <see cref="FindPhoneOnlineVoterAsync"/> but not AsNoTracking,
    /// so callers can write <see cref="OnlineVoter.SmsStatus"/> or
    /// <see cref="OnlineVoter.WhatsAppStatus"/>.
    /// Does not look up by VoterId alone. A non-P row occupying that VoterId is not returned.
    /// </summary>
    public static Task<OnlineVoter?> FindTrackedPhoneOnlineVoterAsync(
        MainDbContext context,
        string? phone,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (string.IsNullOrWhiteSpace(phone))
        {
            return Task.FromResult<OnlineVoter?>(null);
        }

        var local = context.OnlineVoters.Local.FirstOrDefault(
            ov => ov.VoterId == phone && ov.VoterIdType == PhoneVoterIdType);
        if (local != null)
        {
            return Task.FromResult<OnlineVoter?>(local);
        }

        return context.OnlineVoters.FirstOrDefaultAsync(
            ov => ov.VoterId == phone && ov.VoterIdType == PhoneVoterIdType,
            cancellationToken);
    }

    /// <summary>
    /// Phone OnlineVoter P rows for the given Person phone strings.
    /// Only <see cref="PhoneVoterIdType"/> rows are returned. A non-P occupant
    /// of a candidate <see cref="OnlineVoter.VoterId"/> is omitted (never seen).
    /// Keyed by stored <see cref="OnlineVoter.VoterId"/> (Person phone as stored).
    /// </summary>
    public static async Task<Dictionary<string, OnlineVoter>> FindPhoneOnlineVotersAsync(
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
            return new Dictionary<string, OnlineVoter>(StringComparer.Ordinal);
        }

        var rows = await context.OnlineVoters
            .AsNoTracking()
            .Where(ov =>
                distinctPhones.Contains(ov.VoterId) && ov.VoterIdType == PhoneVoterIdType)
            .ToListAsync(cancellationToken);

        return rows.ToDictionary(ov => ov.VoterId, StringComparer.Ordinal);
    }

    /// <summary>
    /// Compact list/Front Desk hint. Null when there is no phone (UI hides the cell).
    /// A missing or non-P <paramref name="phoneRow"/> is never seen
    /// (<see cref="PersonPhoneSmsHintDto.HasPhoneRow"/> false; status/dates unset).
    /// </summary>
    public static PersonPhoneSmsHintDto? ToListHint(string? phone, OnlineVoter? phoneRow)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        if (phoneRow == null || phoneRow.VoterIdType != PhoneVoterIdType)
        {
            return new PersonPhoneSmsHintDto { HasPhoneRow = false };
        }

        return new PersonPhoneSmsHintDto
        {
            HasPhoneRow = true,
            WhenRegistered = phoneRow.WhenRegistered,
            SmsStatus = phoneRow.SmsStatus
        };
    }

    /// <summary>
    /// Same as <see cref="ToListHint(string?, OnlineVoter?)"/> using a batch
    /// dictionary from <see cref="FindPhoneOnlineVotersAsync"/>.
    /// </summary>
    public static PersonPhoneSmsHintDto? ToListHint(
        string? phone,
        IReadOnlyDictionary<string, OnlineVoter> rowsByVoterId)
    {
        ArgumentNullException.ThrowIfNull(rowsByVoterId);

        OnlineVoter? row = null;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            rowsByVoterId.TryGetValue(phone, out row);
        }

        return ToListHint(phone, row);
    }
}
