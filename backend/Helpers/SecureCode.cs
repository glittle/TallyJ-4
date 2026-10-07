using System.Security.Cryptography;

namespace Backend.Helpers;

/// <summary>
/// Draws one-time codes from <see cref="RandomNumberGenerator"/>.
/// <see cref="System.Random"/> is not used: it is predictable from a small seed.
/// </summary>
public static class SecureCode
{
    /// <summary>
    /// <see cref="RandomNumberGenerator.GetInt32(int)"/>. Tests check this delegate
    /// so a later edit cannot swap the source back to <see cref="System.Random"/>.
    /// </summary>
    public static readonly Func<int, int> NextInt32 = RandomNumberGenerator.GetInt32;

    /// <summary>
    /// Builds a code of <paramref name="length"/> characters from <paramref name="alphabet"/>.
    /// Each index comes from <paramref name="next"/>, or from <see cref="NextInt32"/> when it is omitted.
    /// </summary>
    public static string FromAlphabet(string alphabet, int length, Func<int, int>? next = null)
    {
        if (string.IsNullOrEmpty(alphabet))
        {
            throw new ArgumentException("Alphabet is required.", nameof(alphabet));
        }

        if (length < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(length), "Length must be at least 1.");
        }

        next ??= NextInt32;
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = alphabet[next(alphabet.Length)];
        }

        return new string(chars);
    }
}
