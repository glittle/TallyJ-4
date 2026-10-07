namespace Backend.Helpers;

/// <summary>
/// The one voter-facing phrase for <c>POST /api/online-voting/requestCode</c>.
/// Listed and unlisted identifiers share it so the call is not a membership check.
/// </summary>
public static class RequestCodeReply
{
    /// <summary>
    /// "If you are on the voter list, a code has been sent."
    /// </summary>
    public const string NeutralMessageKey = "voting.auth.requestCode.sentIfListed";
}
