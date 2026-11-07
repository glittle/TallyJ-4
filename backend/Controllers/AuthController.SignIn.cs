using Backend.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

public partial class AuthController
{
    /// <summary>
    /// Effective sign-in methods for this host. Public so the login pages can hide
    /// what the server will reject. Does not change sessions that already exist.
    /// </summary>
    [AllowAnonymous]
    [HttpGet("sign-in-capabilities")]
    public IActionResult GetSignInCapabilities()
    {
        return Ok(SignInCapabilities.Evaluate(_configuration, _hostEnvironment));
    }

    private SignInCapabilitySet CurrentSignIn() =>
        SignInCapabilities.Evaluate(_configuration, _hostEnvironment);

    private IActionResult? RejectOfficerSignIn(bool offered)
    {
        if (offered)
        {
            return null;
        }

        return BadRequest(new { error = SignInCapabilities.OfficerDisabledKey });
    }
}
