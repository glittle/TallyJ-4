using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.AuditLogs;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogsController> _logger;

    public AuditLogsController(IAuditLogService auditLogService, ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AuditLogDto>>> GetAuditLogs(
        [FromQuery] Guid? electionGuid = null,
        [FromQuery] Guid? locationGuid = null,
        [FromQuery] string? voterId = null,
        [FromQuery] string? computerCode = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null,
        [FromQuery] string? searchTerm = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 50)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 200)
        {
            return BadRequest(new { message = "Invalid pagination parameters. PageNumber must be >= 1, PageSize must be between 1 and 200." });
        }

        var filter = new AuditLogFilterDto
        {
            ElectionGuid = electionGuid,
            LocationGuid = locationGuid,
            VoterId = voterId,
            ComputerCode = computerCode,
            StartDate = startDate,
            EndDate = endDate,
            SearchTerm = searchTerm
        };

        var result = await _auditLogService.GetAuditLogsAsync(filter, pageNumber, pageSize);
        return Ok(result);
    }

    [HttpGet("{rowId}")]
    public async Task<ActionResult<ApiResponse<AuditLogDto>>> GetAuditLog(int rowId)
    {
        var log = await _auditLogService.GetAuditLogByIdAsync(rowId);

        if (log == null)
        {
            return NotFound(ApiResponse<AuditLogDto>.ErrorResponse("Audit log not found"));
        }

        return Ok(ApiResponse<AuditLogDto>.SuccessResponse(log));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<AuditLogDto>>> CreateAuditLog(CreateAuditLogDto createDto)
    {
        var log = await _auditLogService.CreateAuditLogAsync(createDto);

        return CreatedAtAction(
            nameof(GetAuditLog),
            new { rowId = log.RowId },
            ApiResponse<AuditLogDto>.SuccessResponse(log, "Audit log created successfully"));
    }
}
