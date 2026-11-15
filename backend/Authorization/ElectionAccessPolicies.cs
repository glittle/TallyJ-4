namespace Backend.Authorization;

/// <summary>
/// Policy names registered in <c>ProgramAuthSetup.ConfigureAuthorization</c>.
/// The string values are the names <c>[Authorize(Policy = …)]</c> and
/// <c>IAuthorizationService.AuthorizeAsync</c> already use. Two of those
/// strings predate the check they perform; the C# identifiers name the check.
/// </summary>
public static class ElectionAccessPolicies
{
    /// <summary>
    /// Guest teller for the election on the token, any <c>JoinElectionUsers</c>
    /// row, Identity role <c>Admin</c>, or an address in <c>SuperAdmin:Emails</c>.
    /// </summary>
    public const string ElectionAccess = "ElectionAccess";

    /// <summary>
    /// A <c>JoinElectionUsers</c> member of an election that has at least one
    /// teller name. Guest tellers are denied. The teller name is not this
    /// user's account. Registered name remains <c>TellerAccess</c>. No action
    /// requires this policy today.
    /// </summary>
    public const string TellerNamesMemberAccess = "TellerAccess";

    /// <summary>
    /// Any <c>JoinElectionUsers</c> row. Guest tellers are denied, which is
    /// the only difference from <see cref="ElectionAccess"/>. There is no
    /// head-teller role. Registered name remains <c>HeadTellerAccess</c>.
    /// </summary>
    public const string ElectionMemberAccess = "HeadTellerAccess";

    /// <summary>
    /// Owner or Admin join, Identity role <c>Admin</c>, or a super-admin email.
    /// A role <c>Teller</c> join and guest tellers are denied. "Full teller"
    /// is that known-account tier, not a row in the Teller table.
    /// </summary>
    public const string FullTellerAccess = "FullTellerAccess";
}
