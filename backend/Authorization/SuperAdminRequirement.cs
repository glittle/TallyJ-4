using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Caller email is in <c>SuperAdmin:Emails</c>. Site-wide; not an election role.
/// </summary>
public class SuperAdminRequirement : IAuthorizationRequirement
{
}
