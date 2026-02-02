using TallyJ4.DTOs.AuditLogs;
using TallyJ4.Models;

namespace TallyJ4.Services;

public interface IAuditLogService
{
    Task<PaginatedResponse<AuditLogDto>> GetAuditLogsAsync(
        AuditLogFilterDto? filter = null,
        int pageNumber = 1,
        int pageSize = 50);
    
    Task<AuditLogDto?> GetAuditLogByIdAsync(int rowId);
    
    Task<AuditLogDto> CreateAuditLogAsync(CreateAuditLogDto createDto);
}
