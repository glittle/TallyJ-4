using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Caller may use this election: guest teller for the token's election, any
/// <c>JoinElectionUsers</c> row, Identity role <c>Admin</c>, or a super-admin email.
/// The election id comes from the route (<c>guid</c>, <c>electionGuid</c>,
/// <c>electionId</c>, or a guid <c>id</c>) or from a <see cref="Guid"/> resource.
/// </summary>
public class ElectionAccessRequirement : IAuthorizationRequirement
{
}
