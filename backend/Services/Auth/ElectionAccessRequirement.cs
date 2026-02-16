using Microsoft.AspNetCore.Authorization;

namespace Backend.Services.Auth;

/// <summary>
/// Authorization requirement for election-specific access control.
/// Specifies the role required for accessing election resources.
/// </summary>
public class ElectionAccessRequirement : IAuthorizationRequirement
{
    /// <summary>
    /// Gets the required role for accessing the election resource.
    /// Empty string means any role is acceptable.
    /// </summary>
    public string RequiredRole { get; }

    /// <summary>
    /// Initializes a new instance of the ElectionAccessRequirement.
    /// </summary>
    /// <param name="requiredRole">The specific role required for access (optional).</param>
    public ElectionAccessRequirement(string requiredRole = "")
    {
        RequiredRole = requiredRole;
    }
}


