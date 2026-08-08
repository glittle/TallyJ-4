using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Backend.DTOs.Auth;
using Backend.Services.Auth;
using Backend.Authorization;
using Backend;
using Backend.Configuration;
using Backend.Context;
using Backend.Identity;
using Backend.DTOs.Security;
using Backend.Helpers;
using Backend.Middleware;
using Backend.Services;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Backend.Controllers;

public partial class AuthController
{
    [HttpGet("getUserRoles")]
    public async Task<IActionResult> GetUserRoles()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Unauthorized();
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound();
        }

        var roles = await _userManager.GetRolesAsync(user);
        return Ok(new { userId = user.Id, email = user.Email, roles });
    }

    /// <summary>
    /// Assigns a role to a user. Requires admin privileges.
    /// </summary>
    /// <param name="userId">The ID of the user to assign the role to.</param>
    /// <param name="request">The assign role request containing the role name.</param>
    /// <returns>A success message if the role was assigned, or an error if the operation fails.</returns>
    [Authorize(Policy = "AdminOnly")]
    [HttpPost("{userId}/assignRole")]
    public async Task<IActionResult> AssignRole(string userId, [FromBody] AssignRoleRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        if (!await _roleManager.RoleExistsAsync(request.RoleName))
        {
            return BadRequest(new { error = "Role does not exist" });
        }

        var result = await _userManager.AddToRoleAsync(user, request.RoleName);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        return Ok(new { message = $"Role {request.RoleName} assigned to user {user.Email}" });
    }

    /// <summary>
    /// Removes a role from a user. Requires admin privileges.
    /// </summary>
    /// <param name="userId">The ID of the user to remove the role from.</param>
    /// <param name="roleName">The name of the role to remove.</param>
    /// <returns>A success message if the role was removed, or an error if the operation fails.</returns>
    [Authorize(Policy = "AdminOnly")]
    [HttpDelete("{userId}/{roleName}/removeRole")]
    public async Task<IActionResult> RemoveRole(string userId, string roleName)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return NotFound(new { error = "User not found" });
        }

        var result = await _userManager.RemoveFromRoleAsync(user, roleName);
        if (!result.Succeeded)
        {
            return BadRequest(new { error = string.Join(", ", result.Errors.Select(e => e.Description)) });
        }

        return Ok(new { message = $"Role {roleName} removed from user {user.Email}" });
    }

    /// <summary>
    /// Gets all available roles in the system. Requires admin privileges.
    /// </summary>
    /// <returns>A list of all available roles.</returns>
    [Authorize(Policy = "AdminOnly")]
    [HttpGet("getAllRoles")]
    public IActionResult GetAllRoles()
    {
        var roles = _roleManager.Roles.Select(r => r.Name).ToList();
        return Ok(new { roles });
    }

    /// <summary>
    /// Initiates Google OAuth login flow with PKCE (Proof Key for Code Exchange) and state parameter for CSRF protection.
    /// </summary>
    /// <param name="returnUrl">The URL to redirect to after successful authentication (default: frontend origin).</param>

}
