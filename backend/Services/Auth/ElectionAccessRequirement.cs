using Microsoft.AspNetCore.Authorization;

namespace TallyJ4.Services.Auth;

public class ElectionAccessRequirement : IAuthorizationRequirement
{
    public string RequiredRole { get; }

    public ElectionAccessRequirement(string requiredRole = "")
    {
        RequiredRole = requiredRole;
    }
}