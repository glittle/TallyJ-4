using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Authorization requirement that specifies a user must be a teller for the specified election.
/// </summary>
public class TellerAccessRequirement : IAuthorizationRequirement
{
}



