using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Requires the caller to be a Full Teller for the election in the route
/// (global Admin, or election Owner/Admin join record). Guest tellers are excluded.
/// </summary>
public class FullTellerAccessRequirement : IAuthorizationRequirement;