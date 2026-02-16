using Backend.DTOs.AuditLogs;
using Backend.Models;

namespace Backend.Services;

/// <summary>
/// Service for managing audit logs.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Retrieves a paginated list of audit logs with optional filtering.
    /// </summary>
    /// <param name="filter">Optional filter criteria for audit logs.</param>
    /// <param name="pageNumber">The page number (1-based).</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <returns>A paginated response containing audit log DTOs.</returns>
    Task<PaginatedResponse<AuditLogDto>> GetAuditLogsAsync(
        AuditLogFilterDto? filter = null,
        int pageNumber = 1,
        int pageSize = 50);

    /// <summary>
    /// Retrieves an audit log entry by its unique row identifier.
    /// </summary>
    /// <param name="rowId">The unique row identifier of the audit log.</param>
    /// <returns>The audit log DTO if found, otherwise null.</returns>
    Task<AuditLogDto?> GetAuditLogByIdAsync(int rowId);

    /// <summary>
    /// Creates a new audit log entry.
    /// </summary>
    /// <param name="createDto">The audit log creation data.</param>
    /// <returns>The created audit log DTO.</returns>
    Task<AuditLogDto> CreateAuditLogAsync(CreateAuditLogDto createDto);
}



