using System.Security.Cryptography;
using System.Text;

namespace Backend.Helpers;

/// <summary>
/// Compares teller passcodes with <see cref="CryptographicOperations.FixedTimeEquals"/>
/// on UTF-8 bytes. A length mismatch still runs that compare (stored bytes against
/// themselves) because FixedTimeEquals rejects unequal lengths, then returns false.
/// </summary>
public static class TellerPasscodeComparer
{
    /// <summary>
    /// Stand-in stored value used when no election row exists, so that path still
    /// runs the fixed-time compare before the same invalid response is returned.
    /// </summary>
    public const string MissingElectionPlaceholder = "missing-election";

    /// <summary>
    /// Returns true when <paramref name="stored"/> and <paramref name="submitted"/>
    /// are the same non-empty UTF-8 sequence.
    /// An empty stored passcode does not match, including an empty submission.
    /// </summary>
    public static bool EqualsUtf8(string? stored, string? submitted)
    {
        var storedBytes = Encoding.UTF8.GetBytes(stored ?? string.Empty);
        var submittedBytes = Encoding.UTF8.GetBytes(submitted ?? string.Empty);

        if (storedBytes.Length != submittedBytes.Length)
        {
            CryptographicOperations.FixedTimeEquals(storedBytes, storedBytes);
            return false;
        }

        var equal = CryptographicOperations.FixedTimeEquals(storedBytes, submittedBytes);
        return storedBytes.Length > 0 && equal;
    }
}
