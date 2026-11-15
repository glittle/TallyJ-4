using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Caller is a <c>JoinElectionUsers</c> member of an election that has at
/// least one teller name. Not "this user is a teller". Guest tellers fail.
/// Registered policy name remains <c>TellerAccess</c>
/// (<see cref="ElectionAccessPolicies.TellerNamesMemberAccess"/>).
/// No action requires this policy today.
/// </summary>
public class TellerNamesMemberAccessRequirement : IAuthorizationRequirement
{
}
