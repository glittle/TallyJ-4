using Microsoft.AspNetCore.Authorization;

namespace TallyJ4.Authorization;

public class ElectionAccessRequirement : IAuthorizationRequirement
{
    // This requirement checks if the user has access to the election
    // specified in the route parameter "guid"
}