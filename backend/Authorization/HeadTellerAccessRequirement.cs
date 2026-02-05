using Microsoft.AspNetCore.Authorization;

namespace TallyJ4.Authorization;

/// <summary>
/// Authorization requirement that specifies a user must be a head teller for the specified election.
/// </summary>
public class HeadTellerAccessRequirement : IAuthorizationRequirement
{
}
