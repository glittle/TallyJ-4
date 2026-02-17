using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Backend.DTOs.Eligibility;
using Backend.Domain.Enumerations;
using Backend.Models;

namespace Backend.Controllers;

/// <summary>
/// Controller for managing eligibility reasons.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EligibilityController : ControllerBase
{
    /// <summary>
    /// Gets all eligibility reasons.
    /// </summary>
    /// <returns>A list of all eligibility reasons.</returns>
    [HttpGet("eligibility-reasons")]
    public ActionResult<ApiResponse<List<EligibilityReasonDto>>> GetEligibilityReasons()
    {
        var reasons = IneligibleReasonEnum.All.Select(reason => new EligibilityReasonDto
        {
            ReasonGuid = reason.ReasonGuid,
            Code = reason.Code,
            Description = reason.Description,
            CanVote = reason.CanVote,
            CanReceiveVotes = reason.CanReceiveVotes,
            InternalOnly = reason.InternalOnly
        }).ToList();

        return Ok(ApiResponse<List<EligibilityReasonDto>>.SuccessResponse(reasons));
    }
}