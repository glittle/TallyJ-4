namespace Backend.Services;

/// <summary>
/// Checks phones and emails already stored on an election and flags the election
/// when too many contacts fail.
/// </summary>
public interface IVoterContactReviewer
{
    /// <summary>
    /// Replaces the active flag rows for the election from its current people.
    /// <paramref name="sourceRowNumbers"/> maps a person to the file row that added them.
    /// </summary>
    Task ReviewElectionAsync(
        Guid electionGuid,
        IReadOnlyDictionary<Guid, int>? sourceRowNumbers = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Why one phone or email was flagged.
/// </summary>
public static class VoterContactFlagReason
{
    /// <summary>
    /// The phone did not parse as a valid number.
    /// </summary>
    public const string InvalidPhone = "invalid-phone";

    /// <summary>
    /// The phone is valid and its country is not one this election expects.
    /// </summary>
    public const string UnexpectedCountry = "unexpected-country";

    /// <summary>
    /// The phone sits in a run of sequential numbers.
    /// </summary>
    public const string ConsecutiveRun = "consecutive-run";

    /// <summary>
    /// The email domain is on the disposable-domain list.
    /// </summary>
    public const string DisposableDomain = "disposable-domain";

    /// <summary>
    /// The email domain has no MX record, or its MX is the Null MX in RFC 7505.
    /// </summary>
    public const string NoMx = "no-mx";

    /// <summary>
    /// The review threw. The election is flagged so codes and online voting stay stopped.
    /// </summary>
    public const string ReviewFailed = "review-failed";
}

/// <summary>
/// Result of one MX lookup. Unknown is a timeout or a DNS failure, not a missing record.
/// </summary>
public enum MxLookupResult
{
    /// <summary>
    /// The domain has at least one MX that is not the Null MX.
    /// </summary>
    HasMx,

    /// <summary>
    /// The lookup succeeded and returned no MX records.
    /// </summary>
    NoMx,

    /// <summary>
    /// The only MX is "." (RFC 7505).
    /// </summary>
    NullMx,

    /// <summary>
    /// The lookup timed out or the DNS server failed. The domain is not flagged.
    /// </summary>
    Unknown
}

/// <summary>
/// Looks up MX records for one domain.
/// </summary>
public interface IMailExchangerLookup
{
    /// <summary>
    /// Returns whether the domain accepts mail. A timeout is <see cref="MxLookupResult.Unknown"/>.
    /// </summary>
    Task<MxLookupResult> LookupAsync(string domain, CancellationToken cancellationToken = default);
}

/// <summary>
/// Disposable email domains loaded from the vendored list.
/// </summary>
public interface IDisposableDomainList
{
    /// <summary>
    /// True when the domain, or a parent domain, is on the list.
    /// </summary>
    bool Contains(string domain);
}
