using Microsoft.AspNetCore.Authorization;

namespace TallyJ4.Authorization;

/// <summary>
/// Authorization requirement that checks if the authenticated user has access to the election
/// specified in the route parameter "guid".
/// </summary>
public class ElectionAccessRequirement : IAuthorizationRequirement
{
}