using System.Security.Claims;
using Backend.DTOs.Auth;
using Backend.DTOs.SuperAdmin;
using Backend.Models;
using Backend.Services;
using Backend.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Controller for super admin functionality, providing dashboard and management capabilities.
/// </summary>
[ApiController]
[Route("api/superadmin")]
[Authorize]
public class SuperAdminController : ControllerBase
{
    private readonly ISuperAdminService _superAdminService;
    private readonly IAccountInviteService _accountInviteService;
    private readonly IPaidSendAdminService _paidSendAdminService;
    private readonly ILogger<SuperAdminController> _logger;

    /// <summary>
    /// Initializes a new instance of the SuperAdminController.
    /// </summary>
    /// <param name="superAdminService">The super admin service for business logic.</param>
    /// <param name="logger">The logger for diagnostic output.</param>
    public SuperAdminController(
        ISuperAdminService superAdminService,
        IAccountInviteService accountInviteService,
        IPaidSendAdminService paidSendAdminService,
        ILogger<SuperAdminController> logger)
    {
        _superAdminService = superAdminService;
        _accountInviteService = accountInviteService;
        _paidSendAdminService = paidSendAdminService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a summary of system-wide election statistics for the super admin dashboard.
    /// </summary>
    /// <returns>An ApiResponse containing the super admin summary data.</returns>
    [HttpGet("dashboard/summary")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SuperAdminSummaryDto>>> GetSummary()
    {
        var summary = await _superAdminService.GetSummaryAsync();
        return Ok(ApiResponse<SuperAdminSummaryDto>.SuccessResponse(summary));
    }

    /// <summary>
    /// Gets a paginated list of elections for the super admin dashboard.
    /// </summary>
    /// <param name="filter">The filter criteria for querying elections.</param>
    /// <returns>An ApiResponse containing paginated election data.</returns>
    [HttpGet("dashboard/elections")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<SuperAdminElectionDto>>>> GetElections(
        [FromQuery] SuperAdminElectionFilterDto filter)
    {
        var result = await _superAdminService.GetElectionsAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<SuperAdminElectionDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Gets detailed information about a specific election for the super admin dashboard.
    /// </summary>
    /// <param name="guid">The unique identifier of the election.</param>
    /// <returns>An ApiResponse containing detailed election information, or NotFound if the election doesn't exist.</returns>
    [HttpGet("dashboard/elections/{guid:guid}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SuperAdminElectionDetailDto>>> GetElectionDetail(Guid guid)
    {
        var detail = await _superAdminService.GetElectionDetailAsync(guid);
        if (detail == null)
        {
            return NotFound(ApiResponse<SuperAdminElectionDetailDto>.ErrorResponse("Election not found"));
        }

        return Ok(ApiResponse<SuperAdminElectionDetailDto>.SuccessResponse(detail));
    }

    /// <summary>
    /// Lists login accounts for SuperAdmin management.
    /// </summary>
    [HttpGet("users")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<SuperAdminUserDto>>>> GetUsers(
        [FromQuery] SuperAdminUserFilterDto filter)
    {
        var result = await _superAdminService.GetUsersAsync(filter);
        return Ok(ApiResponse<PaginatedResponse<SuperAdminUserDto>>.SuccessResponse(result));
    }

    /// <summary>
    /// Gets a login account including recursive email-change history.
    /// </summary>
    [HttpGet("users/{userId}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SuperAdminUserDetailDto>>> GetUser(string userId)
    {
        var detail = await _superAdminService.GetUserDetailAsync(userId);
        if (detail == null)
        {
            return NotFound(ApiResponse<SuperAdminUserDetailDto>.ErrorResponse("User not found"));
        }

        return Ok(ApiResponse<SuperAdminUserDetailDto>.SuccessResponse(detail));
    }

    /// <summary>
    /// Updates display name and/or email for a login account (immediate, audited).
    /// </summary>
    [HttpPut("users/{userId}")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<SuperAdminUserDetailDto>>> UpdateUser(
        string userId,
        [FromBody] SuperAdminUpdateUserDto dto)
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub") ?? "";
        try
        {
            var detail = await _superAdminService.UpdateUserAsync(userId, dto, adminId);
            if (detail == null)
            {
                return NotFound(ApiResponse<SuperAdminUserDetailDto>.ErrorResponse("User not found"));
            }

            return Ok(ApiResponse<SuperAdminUserDetailDto>.SuccessResponse(detail, "User updated"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SuperAdminUserDetailDto>.ErrorResponse(ex.Message));
        }
    }

    /// <summary>
    /// Issues a one-time invite URL that allows creating a single local email/password account.
    /// Head Teller is election-scoped and cannot issue site-wide account invites.
    /// </summary>
    [HttpPost("account-invites")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<AccountInviteCreatedDto>>> CreateAccountInvite()
    {
        var adminId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
        if (string.IsNullOrEmpty(adminId))
        {
            return Unauthorized(ApiResponse<AccountInviteCreatedDto>.ErrorResponse("Not authenticated"));
        }

        var created = await _accountInviteService.CreateAsync(adminId);
        return Ok(ApiResponse<AccountInviteCreatedDto>.SuccessResponse(created));
    }

    /// <summary>
    /// Lists owners waiting for paid-send approval, cap hits, freezes, and flagged elections.
    /// </summary>
    [HttpGet("paid-sends")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<PaidSendAdminOverviewDto>>> GetPaidSends()
    {
        var overview = await _paidSendAdminService.GetOverviewAsync();
        return Ok(ApiResponse<PaidSendAdminOverviewDto>.SuccessResponse(overview));
    }

    /// <summary>
    /// Approves one owner for SMS, voice, and WhatsApp login codes.
    /// </summary>
    [HttpPost("paid-sends/owners/{userId:guid}/approve")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> ApprovePaidSends(Guid userId)
    {
        var adminId = CurrentAdminId();
        if (adminId == null)
        {
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Not authenticated"));
        }

        var approved = await _paidSendAdminService.ApproveOwnerAsync(userId, adminId);
        if (!approved)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse("Owner not found"));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    /// <summary>
    /// Stops every login code, including email, on each election where this account is an owner or admin.
    /// One frozen owner or admin stops those elections even when a co-owner is not frozen.
    /// </summary>
    [HttpPost("paid-sends/owners/{userId:guid}/freeze")]
    [Authorize(Policy = "SuperAdmin")]
    public Task<ActionResult<ApiResponse<bool>>> FreezeOwner(Guid userId) =>
        SetOwnerFrozen(userId, true);

    /// <summary>
    /// Lifts the freeze for one owner or admin. An election stays frozen while any other owner or admin on it is frozen.
    /// Paid channels still need every owner and admin approved, and the caps.
    /// </summary>
    [HttpPost("paid-sends/owners/{userId:guid}/unfreeze")]
    [Authorize(Policy = "SuperAdmin")]
    public Task<ActionResult<ApiResponse<bool>>> UnfreezeOwner(Guid userId) =>
        SetOwnerFrozen(userId, false);

    /// <summary>
    /// Raises the owner's daily SMS, voice, and WhatsApp cap.
    /// </summary>
    [HttpPost("paid-sends/owners/{userId:guid}/daily-cap")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> RaiseOwnerDailyCap(
        Guid userId,
        [FromBody] RaiseOwnerDailyCapDto dto)
    {
        var adminId = CurrentAdminId();
        if (adminId == null)
        {
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Not authenticated"));
        }

        var raised = await _paidSendAdminService.RaiseOwnerDailyCapAsync(userId, dto.DailyCap, adminId);
        if (!raised)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse("Daily cap was not raised"));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    /// <summary>
    /// Stops every login code for one election, including email.
    /// </summary>
    [HttpPost("paid-sends/elections/{guid:guid}/freeze")]
    [Authorize(Policy = "SuperAdmin")]
    public Task<ActionResult<ApiResponse<bool>>> FreezeElection(Guid guid) =>
        SetElectionFrozen(guid, true);

    /// <summary>
    /// Allows login codes for one election again.
    /// </summary>
    [HttpPost("paid-sends/elections/{guid:guid}/unfreeze")]
    [Authorize(Policy = "SuperAdmin")]
    public Task<ActionResult<ApiResponse<bool>>> UnfreezeElection(Guid guid) =>
        SetElectionFrozen(guid, false);

    /// <summary>
    /// Clears the voter-list flag so paid channels and online voting can run again.
    /// </summary>
    [HttpPost("paid-sends/elections/{guid:guid}/clear-flag")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> ClearElectionFlag(Guid guid)
    {
        var adminId = CurrentAdminId();
        if (adminId == null)
        {
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Not authenticated"));
        }

        var cleared = await _paidSendAdminService.ClearElectionFlagAsync(guid, adminId);
        if (!cleared)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse("Flagged election not found"));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    /// <summary>
    /// Raises the election SMS, voice, and WhatsApp allowance.
    /// </summary>
    [HttpPost("paid-sends/elections/{guid:guid}/allowance")]
    [Authorize(Policy = "SuperAdmin")]
    public async Task<ActionResult<ApiResponse<bool>>> RaiseElectionAllowance(
        Guid guid,
        [FromBody] RaiseElectionAllowanceDto dto)
    {
        var adminId = CurrentAdminId();
        if (adminId == null)
        {
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Not authenticated"));
        }

        var raised = await _paidSendAdminService.RaiseElectionAllowanceAsync(guid, dto.Allowance, adminId);
        if (!raised)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse("Allowance was not raised"));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    private async Task<ActionResult<ApiResponse<bool>>> SetOwnerFrozen(Guid userId, bool frozen)
    {
        var adminId = CurrentAdminId();
        if (adminId == null)
        {
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Not authenticated"));
        }

        var updated = await _paidSendAdminService.SetOwnerFrozenAsync(userId, frozen, adminId);
        if (!updated)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse("Owner not found"));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    private async Task<ActionResult<ApiResponse<bool>>> SetElectionFrozen(Guid electionGuid, bool frozen)
    {
        var adminId = CurrentAdminId();
        if (adminId == null)
        {
            return Unauthorized(ApiResponse<bool>.ErrorResponse("Not authenticated"));
        }

        var updated = await _paidSendAdminService.SetElectionFrozenAsync(electionGuid, frozen, adminId);
        if (!updated)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse("Election not found"));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true));
    }

    private string? CurrentAdminId() =>
        User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub")?.Value;
}



