using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.Tellers;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/elections/{electionGuid}/tellers")]
[Authorize]
public class TellersController : ControllerBase
{
    private readonly ITellerService _tellerService;
    private readonly ILogger<TellersController> _logger;

    public TellersController(ITellerService tellerService, ILogger<TellersController> logger)
    {
        _tellerService = tellerService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<TellerDto>>> GetTellersByElection(
        Guid electionGuid,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 200)
        {
            return BadRequest(new { message = "Invalid pagination parameters. PageNumber must be >= 1, PageSize must be between 1 and 200." });
        }

        var result = await _tellerService.GetTellersByElectionAsync(electionGuid, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{rowId}")]
    public async Task<ActionResult<ApiResponse<TellerDto>>> GetTeller(Guid electionGuid, int rowId)
    {
        var teller = await _tellerService.GetTellerByIdAsync(rowId);

        if (teller == null)
        {
            return NotFound(ApiResponse<TellerDto>.ErrorResponse("Teller not found"));
        }

        if (teller.ElectionGuid != electionGuid)
        {
            return BadRequest(ApiResponse<TellerDto>.ErrorResponse("Teller does not belong to the specified election"));
        }

        return Ok(ApiResponse<TellerDto>.SuccessResponse(teller));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TellerDto>>> CreateTeller(Guid electionGuid, CreateTellerDto createDto)
    {
        if (createDto.ElectionGuid != electionGuid)
        {
            return BadRequest(ApiResponse<TellerDto>.ErrorResponse("Election GUID in the route must match the one in the request body"));
        }

        try
        {
            var teller = await _tellerService.CreateTellerAsync(createDto);

            return CreatedAtAction(
                nameof(GetTeller),
                new { electionGuid = teller.ElectionGuid, rowId = teller.RowId },
                ApiResponse<TellerDto>.SuccessResponse(teller, "Teller created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<TellerDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("{rowId}")]
    public async Task<ActionResult<ApiResponse<TellerDto>>> UpdateTeller(
        Guid electionGuid,
        int rowId,
        UpdateTellerDto updateDto)
    {
        try
        {
            var teller = await _tellerService.UpdateTellerAsync(rowId, updateDto);

            if (teller == null)
            {
                return NotFound(ApiResponse<TellerDto>.ErrorResponse("Teller not found"));
            }

            if (teller.ElectionGuid != electionGuid)
            {
                return BadRequest(ApiResponse<TellerDto>.ErrorResponse("Teller does not belong to the specified election"));
            }

            return Ok(ApiResponse<TellerDto>.SuccessResponse(teller, "Teller updated successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<TellerDto>.ErrorResponse(ex.Message));
        }
    }

    [HttpDelete("{rowId}")]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteTeller(Guid electionGuid, int rowId)
    {
        var teller = await _tellerService.GetTellerByIdAsync(rowId);

        if (teller == null)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse("Teller not found"));
        }

        if (teller.ElectionGuid != electionGuid)
        {
            return BadRequest(ApiResponse<bool>.ErrorResponse("Teller does not belong to the specified election"));
        }

        var result = await _tellerService.DeleteTellerAsync(rowId);

        if (!result)
        {
            return NotFound(ApiResponse<bool>.ErrorResponse("Teller not found"));
        }

        return Ok(ApiResponse<bool>.SuccessResponse(true, "Teller deleted successfully"));
    }
}
