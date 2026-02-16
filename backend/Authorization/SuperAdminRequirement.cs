using Microsoft.AspNetCore.Authorization;

namespace Backend.Authorization;

/// <summary>
/// Authorization requirement that checks if the authenticated user is a super admin
/// based on the configured email list in appsettings.
/// </summary>
public class SuperAdminRequirement : IAuthorizationRequirement
{
}



