using System.Security.Cryptography;
using System.Text;

namespace Backend.Helpers;

/// <summary>
/// Compares voter login codes with <see cref="CryptographicOperations.FixedTimeEquals"/>.
/// A length mismatch still runs a compare (the stored bytes against themselves)
/// because FixedTimeEquals rejects unequal lengths, then returns false.
/// </summary>
public static class VoterCodeComparer
{
    /// <summary>
    /// Returns true when both values are the same non-empty UTF-8 sequence.
    /// Comparison is ordinal, matching the previous <c>!=</c> check.
    /// </summary>
    public static bool FixedTimeEquals(string? stored, string? submitted)
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
