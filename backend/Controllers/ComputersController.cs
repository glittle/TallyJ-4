using Backend.Models;
using Backend.DTOs.Computers;
using Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Backend.Controllers;

/// <summary>
/// Active teller workstation tracking for an election.
/// Computer codes are assigned via MainHub JoinElection (SignalR), not HTTP.
/// </summary>
[ApiController]
[Route("api/{electionGuid}/computers")]
[Authorize]
public class ComputersController : ControllerBase
{
    private readonly IComputerAssignmentService _assignmentService;

    public ComputersController(IComputerAssignmentService assignmentService)
    {
        _assignmentService = assignmentService;
    }

    /// <summary>
    /// Lists workstations currently connected to the election.
    /// </summary>
    [HttpGet("active")]
    public ActionResult<ApiResponse<List<ActiveComputerDto>>> GetActiveComputers(Guid electionGuid)
    {
        var computers = _assignmentService.GetActiveComputers(electionGuid);
        return Ok(ApiResponse<List<ActiveComputerDto>>.SuccessResponse(computers.ToList()));
    }
}