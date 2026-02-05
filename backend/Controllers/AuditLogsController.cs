using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TallyJ4.DTOs.AuditLogs;
using TallyJ4.Models;
using TallyJ4.Services;

namespace TallyJ4.Backend.Controllers;

/// <summary>
/// Controller for managing audit logs.
/// </summary>
[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;
    private readonly ILogger<AuditLogsController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogsController"/> class.
    /// </summary>
    /// <param name="auditLogService">The audit log service.</param>
    /// <param name="logger">The logger.</param>
    public AuditLogsController(IAuditLogService auditLogService, ILogger<AuditLogsController> logger)
    {
        _auditLogService = auditLogService;
        _logger = logger;
    }

    /// <summary>
    /// Gets a paginated list of audit logs with optional filtering.
    /// </summary>
    /// <param name="electionGuid">Optional election GUID filter.</param>
    /// <param name="locationGuid">Optional location GUID filter.</param>
    /// <param name="voterId">Optional voter ID filter.</param>
    /// <param name="computerCode">Optional computer code filter.</param>
    /// <param name="startDate">Optional start date filter.</param>
    /// <param name="endDate">Optional end date filter.</param>
    /// <param name="searchTerm">Optional search term filter.</param>
    /// <param name="pageNumber">The page number (default: 1).</param>
    /// <param name="pageSize">The page size (default: 50, max: 200).</param>
    /// <returns>A paginated response containing audit logs.</returns>
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

    /// <summary>
    /// Gets a specific audit log by ID.
    /// </summary>
    /// <param name="rowId">The audit log row ID.</param>
    /// <returns>The audit log details.</returns>
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

    /// <summary>
    /// Creates a new audit log entry.
    /// </summary>
    /// <param name="createDto">The audit log creation data.</param>
    /// <returns>The created audit log.</returns>
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
