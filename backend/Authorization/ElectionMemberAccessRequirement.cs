using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Any <c>JoinElectionUsers</c> member of the election. Guest tellers and
/// online voters fail. This is not a head-teller role. The registered policy
/// name is still <c>HeadTellerAccess</c>
/// (<see cref="ElectionAccessPolicies.ElectionMemberAccess"/>).
/// </summary>
public class ElectionMemberAccessRequirement : IAuthorizationRequirement
{
}
