using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Owner or Admin <c>JoinElectionUsers</c> row, Identity role <c>Admin</c>,
/// or an address in <c>SuperAdmin:Emails</c>. Guest tellers and a role
/// <c>Teller</c> join are denied. "Full teller" is that known-account tier.
/// </summary>
public class FullTellerAccessRequirement : IAuthorizationRequirement;
