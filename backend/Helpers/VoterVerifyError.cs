namespace Backend.Helpers;

/// <summary>
/// Stable i18n keys for <c>POST /api/online-voting/verifyCode</c> failures.
/// The 400 body is <c>{ error }</c>, plus <c>attempts</c> only for a mismatch.
/// </summary>
public static class VoterVerifyError
{
    public const string CodeExpired = "voting.auth.verify.codeExpired";
    public const string AlreadyUsed = "voting.auth.verify.alreadyUsed";
    public const string TooManyAttempts = "voting.auth.verify.tooManyAttempts";
    public const string InvalidCode = "voting.auth.verify.invalidCode";
    public const string NoCodeFound = "voting.auth.verify.noCodeFound";
    public const string VoterNotFound = "voting.auth.verify.voterNotFound";
    public const string Error = "voting.auth.verify.error";

    public const int MaxAttempts = 5;

    /// <summary>
    /// Service-layer mismatch payload: stable key plus remaining attempts.
    /// <see cref="ToBadRequestBody"/> splits this back into <c>error</c> + <c>attempts</c>.
    /// </summary>
    public static string InvalidCodeWithAttempts(int remaining) => $"{InvalidCode}:{remaining}";

    /// <summary>
    /// A consumed OTP leaves <c>VerifyCode</c> cleared and <c>VerifyCodeDate</c> set.
    /// A never-issued row has neither.
    /// </summary>
    public static string MissingCodeKey(DateTimeOffset? verifyCodeDate) =>
        verifyCodeDate != null ? AlreadyUsed : NoCodeFound;

    public static object ToBadRequestBody(string? error)
    {
        if (TrySplitInvalidCode(error, out var remaining))
        {
            return new { error = InvalidCode, attempts = remaining };
        }

        return new { error };
    }

    public static bool TrySplitInvalidCode(string? error, out int remaining)
    {
        remaining = 0;
        const string prefix = InvalidCode + ":";
        if (error == null || !error.StartsWith(prefix, StringComparison.Ordinal))
        {
            return false;
        }

        return int.TryParse(error.AsSpan(prefix.Length), out remaining);
    }
}
