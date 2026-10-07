using System.Globalization;
using Backend.Configuration;
using Backend.Context;
using Backend.DTOs.Security;
using Backend.Entities;
using Backend.Enumerations;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using PhoneNumbers;

namespace Backend.Services;

/// <summary>
/// Flags invalid phones, unexpected countries, sequential numbers, disposable domains, and domains with no MX.
/// </summary>
public class VoterContactReviewer : IVoterContactReviewer
{
    private readonly MainDbContext _context;
    private readonly IDisposableDomainList _disposableDomains;
    private readonly IMailExchangerLookup _mailExchangers;
    private readonly IAbuseAlertService _alerts;
    private readonly ISecurityAuditService _audit;
    private readonly AntiAbuseOptions _options;
    private readonly ILogger<VoterContactReviewer> _logger;
    private readonly PhoneNumberUtil _phones = PhoneNumberUtil.GetInstance();

    /// <summary>
    /// Initializes the reviewer.
    /// </summary>
    public VoterContactReviewer(
        MainDbContext context,
        IDisposableDomainList disposableDomains,
        IMailExchangerLookup mailExchangers,
        IAbuseAlertService alerts,
        ISecurityAuditService audit,
        IOptions<AntiAbuseOptions> options,
        ILogger<VoterContactReviewer> logger)
    {
        _context = context;
        _disposableDomains = disposableDomains;
        _mailExchangers = mailExchangers;
        _alerts = alerts;
        _audit = audit;
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ReviewElectionAsync(
        Guid electionGuid,
        IReadOnlyDictionary<Guid, int>? sourceRowNumbers = null,
        CancellationToken cancellationToken = default)
    {
        var election = await _context.Elections
            .AsNoTracking()
            .Where(item => item.ElectionGuid == electionGuid)
            .Select(item => new { item.Name, item.ExpectedPhoneRegions })
            .FirstOrDefaultAsync(cancellationToken);
        if (election == null)
        {
            return;
        }

        var people = await _context.People
            .AsNoTracking()
            .Where(person => person.ElectionGuid == electionGuid)
            .Select(person => new PersonContact(person.PersonGuid, person.Phone, person.Email))
            .ToListAsync(cancellationToken);

        var expected = ExpectedRegions(election.ExpectedPhoneRegions);
        var flags = new List<VoterContactFlag>();
        var parsedPhones = new List<ParsedPhone>();
        foreach (var person in people)
        {
            CollectPhone(person, expected.DefaultRegion, expected.Regions, sourceRowNumbers, flags, parsedPhones);
        }

        CollectConsecutiveRuns(parsedPhones, sourceRowNumbers, flags);
        await CollectEmailsAsync(people, sourceRowNumbers, flags, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        foreach (var flag in flags)
        {
            flag.ElectionGuid = electionGuid;
            flag.FlaggedAt = now;
            flag.Active = true;
        }

        var previous = await _context.VoterContactFlags
            .Where(row => row.ElectionGuid == electionGuid && row.Active)
            .ToListAsync(cancellationToken);
        foreach (var row in previous)
        {
            row.Active = false;
        }

        _context.VoterContactFlags.AddRange(flags);
        await _context.SaveChangesAsync(cancellationToken);

        var distinct = flags.Select(flag => flag.ContactKey).Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (distinct <= _options.ResolvedFlaggedEntryThreshold)
        {
            return;
        }

        var control = await _context.ElectionSendControls
            .FirstOrDefaultAsync(row => row.ElectionGuid == electionGuid, cancellationToken);
        var alreadyFlagged = control?.Flagged == true;
        if (control == null)
        {
            control = new ElectionSendControl { ElectionGuid = electionGuid };
            _context.ElectionSendControls.Add(control);
        }

        control.FlaggedEntryCount = distinct;
        if (!alreadyFlagged)
        {
            control.Flagged = true;
            control.FlaggedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);
        if (alreadyFlagged)
        {
            return;
        }

        await _audit.LogSecurityEventAsync(new CreateSecurityAuditLogDto
        {
            EventType = SecurityEventType.ElectionFlagged,
            ElectionGuid = electionGuid,
            Details = "Flagged election " + electionGuid + " with " + distinct + " voter-list entries",
            Severity = SecurityEventSeverity.Warning
        });

        var rows = flags
            .Select(flag => new AbuseFlaggedRow(flag.SourceRowNumber, flag.MaskedValue, flag.Reason))
            .ToList();
        await _alerts.NotifyElectionFlaggedAsync(
            new AbuseElectionFlaggedAlert(electionGuid, election.Name, distinct, rows),
            cancellationToken);
        _logger.LogWarning(
            "Flagged election {ElectionGuid} with {FlaggedEntryCount} voter-list entries",
            electionGuid,
            distinct);
    }

    private void CollectPhone(
        PersonContact person,
        string defaultRegion,
        IReadOnlySet<string> expected,
        IReadOnlyDictionary<Guid, int>? sourceRowNumbers,
        List<VoterContactFlag> flags,
        List<ParsedPhone> parsedPhones)
    {
        if (string.IsNullOrWhiteSpace(person.Phone))
        {
            return;
        }

        var raw = person.Phone.Trim();
        PhoneNumber parsed;
        try
        {
            parsed = _phones.Parse(raw, defaultRegion);
        }
        catch (NumberParseException)
        {
            flags.Add(MakeFlag(person.PersonGuid, sourceRowNumbers, DestinationMask.Mask(raw), VoterContactFlagReason.InvalidPhone, "phone:" + Digits(raw)));
            return;
        }

        if (!_phones.IsValidNumber(parsed))
        {
            flags.Add(MakeFlag(person.PersonGuid, sourceRowNumbers, DestinationMask.Mask(raw), VoterContactFlagReason.InvalidPhone, "phone:" + Digits(raw)));
            return;
        }

        var e164 = _phones.Format(parsed, PhoneNumberFormat.E164);
        var region = _phones.GetRegionCodeForNumber(parsed);
        if (string.IsNullOrEmpty(region) || !expected.Contains(region))
        {
            flags.Add(MakeFlag(person.PersonGuid, sourceRowNumbers, DestinationMask.Mask(e164), VoterContactFlagReason.UnexpectedCountry, "phone:" + e164));
        }

        if (long.TryParse(e164.TrimStart('+'), NumberStyles.None, CultureInfo.InvariantCulture, out var numeric))
        {
            parsedPhones.Add(new ParsedPhone(person.PersonGuid, e164, numeric));
        }
    }

    private void CollectConsecutiveRuns(
        List<ParsedPhone> parsedPhones,
        IReadOnlyDictionary<Guid, int>? sourceRowNumbers,
        List<VoterContactFlag> flags)
    {
        var length = _options.ResolvedConsecutivePhoneRunLength;
        var ordered = parsedPhones
            .GroupBy(phone => phone.Numeric)
            .OrderBy(group => group.Key)
            .ToList();
        var runStart = 0;
        for (var index = 1; index <= ordered.Count; index++)
        {
            var continues = index < ordered.Count && ordered[index].Key == ordered[index - 1].Key + 1;
            if (continues)
            {
                continue;
            }

            var runLength = index - runStart;
            if (runLength >= length)
            {
                for (var runIndex = runStart; runIndex < index; runIndex++)
                {
                    foreach (var phone in ordered[runIndex])
                    {
                        flags.Add(MakeFlag(
                            phone.PersonGuid,
                            sourceRowNumbers,
                            DestinationMask.Mask(phone.E164),
                            VoterContactFlagReason.ConsecutiveRun,
                            "phone:" + phone.E164));
                    }
                }
            }

            runStart = index;
        }
    }

    private async Task CollectEmailsAsync(
        List<PersonContact> people,
        IReadOnlyDictionary<Guid, int>? sourceRowNumbers,
        List<VoterContactFlag> flags,
        CancellationToken cancellationToken)
    {
        var pending = new List<(PersonContact Person, string Email, string Domain)>();
        var domains = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var person in people)
        {
            if (string.IsNullOrWhiteSpace(person.Email))
            {
                continue;
            }

            var email = person.Email.Trim();
            var at = email.LastIndexOf('@');
            if (at <= 0 || at == email.Length - 1)
            {
                continue;
            }

            var domain = email[(at + 1)..].Trim().TrimEnd('.').ToLowerInvariant();
            if (domain.Length == 0)
            {
                continue;
            }

            if (_disposableDomains.Contains(domain))
            {
                flags.Add(MakeFlag(person.PersonGuid, sourceRowNumbers, DestinationMask.Mask(email), VoterContactFlagReason.DisposableDomain, "email:" + email.ToLowerInvariant()));
                continue;
            }

            pending.Add((person, email, domain));
            domains.Add(domain);
        }

        var lookups = new System.Collections.Concurrent.ConcurrentDictionary<string, MxLookupResult>(StringComparer.OrdinalIgnoreCase);
        var parallel = new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.ResolvedMxLookupParallelism,
            CancellationToken = cancellationToken
        };
        await Parallel.ForEachAsync(domains, parallel, async (domain, token) =>
        {
            lookups[domain] = await _mailExchangers.LookupAsync(domain, token);
        });

        foreach (var item in pending)
        {
            if (!lookups.TryGetValue(item.Domain, out var result))
            {
                continue;
            }

            if (result is MxLookupResult.NoMx or MxLookupResult.NullMx)
            {
                flags.Add(MakeFlag(
                    item.Person.PersonGuid,
                    sourceRowNumbers,
                    DestinationMask.Mask(item.Email),
                    VoterContactFlagReason.NoMx,
                    "email:" + item.Email.ToLowerInvariant()));
            }
        }
    }

    private static VoterContactFlag MakeFlag(
        Guid personGuid,
        IReadOnlyDictionary<Guid, int>? sourceRowNumbers,
        string masked,
        string reason,
        string contactKey)
    {
        int? rowNumber = null;
        if (sourceRowNumbers != null && sourceRowNumbers.TryGetValue(personGuid, out var row))
        {
            rowNumber = row;
        }

        return new VoterContactFlag
        {
            PersonGuid = personGuid,
            SourceRowNumber = rowNumber,
            MaskedValue = masked,
            Reason = reason,
            ContactKey = contactKey
        };
    }

    private (string DefaultRegion, HashSet<string> Regions) ExpectedRegions(string? stored)
    {
        var regions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        string? first = null;
        if (!string.IsNullOrWhiteSpace(stored))
        {
            foreach (var part in stored.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var code = part.ToUpperInvariant();
                first ??= code;
                regions.Add(code);
            }
        }

        var fallback = string.IsNullOrWhiteSpace(_options.DefaultPhoneRegionCode)
            ? AntiAbuseOptions.DefaultPhoneRegion
            : _options.DefaultPhoneRegionCode.Trim().ToUpperInvariant();
        if (regions.Count == 0)
        {
            regions.Add(fallback);
        }

        return (first ?? fallback, regions);
    }

    private static string Digits(string value)
    {
        return new string(value.Where(char.IsDigit).ToArray());
    }

    private sealed record PersonContact(Guid PersonGuid, string? Phone, string? Email);

    private sealed record ParsedPhone(Guid PersonGuid, string E164, long Numeric);
}
