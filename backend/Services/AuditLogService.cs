using Backend.Context;
using Backend.Entities;
using Backend.DTOs.AuditLogs;
using Backend.Helpers;
using Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service implementation for managing audit logs.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly MainDbContext _context;
    private readonly ILogger<AuditLogService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogService"/> class.
    /// </summary>
    public AuditLogService(MainDbContext context, ILogger<AuditLogService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<PaginatedResponse<AuditLogDto>> GetAuditLogsAsync(
        AuditLogFilterDto? filter = null,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var query = _context.Logs.AsQueryable();

        if (filter != null)
        {
            if (filter.ElectionGuid.HasValue)
            {
                query = query.Where(l => l.ElectionGuid == filter.ElectionGuid.Value);
            }

            if (filter.LocationGuid.HasValue)
            {
                query = query.Where(l => l.LocationGuid == filter.LocationGuid.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.VoterId))
            {
                query = query.Where(l => l.VoterId == filter.VoterId);
            }

            if (!string.IsNullOrWhiteSpace(filter.ComputerCode))
            {
                query = query.Where(l => l.ComputerCode == filter.ComputerCode);
            }

            if (filter.StartDate.HasValue)
            {
                query = query.Where(l => l.AsOf >= filter.StartDate.Value);
            }

            if (filter.EndDate.HasValue)
            {
                query = query.Where(l => l.AsOf <= filter.EndDate.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(l =>
                    (l.Details != null && l.Details.ToLower().Contains(searchTerm)) ||
                    (l.VoterId != null && l.VoterId.ToLower().Contains(searchTerm)) ||
                    (l.ComputerCode != null && l.ComputerCode.ToLower().Contains(searchTerm)) ||
                    (l.HostAndVersion != null && l.HostAndVersion.ToLower().Contains(searchTerm)));
            }
        }

        var totalCount = await query.CountAsync();

        var logs = await query
            .OrderByDescending(l => l.AsOf)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var logDtos = logs.Select(l => l.CopyMatchingPropertiesToNew<AuditLogDto>()).ToList();

        _logger.LogInformation(
            "Retrieved {Count} audit logs (page {PageNumber} of {TotalPages})",
            logDtos.Count,
            pageNumber,
            (totalCount + pageSize - 1) / pageSize);

        return PaginatedResponse<AuditLogDto>.Create(logDtos, pageNumber, pageSize, totalCount);
    }

    /// <inheritdoc />
    public async Task<AuditLogDto?> GetAuditLogByIdAsync(int rowId)
    {
        var log = await _context.Logs
            .Where(l => l.RowId == rowId)
            .FirstOrDefaultAsync();

        if (log == null)
        {
            _logger.LogWarning("Audit log with RowId {RowId} not found", rowId);
            return null;
        }

        var logDto = log.CopyMatchingPropertiesToNew<AuditLogDto>();

        _logger.LogInformation("Retrieved audit log {RowId}", rowId);

        return logDto;
    }

    /// <inheritdoc />
    public async Task<AuditLogDto> CreateAuditLogAsync(CreateAuditLogDto createDto)
    {
        // _logger.LogInformation("Creating new audit log entry");

        var log = createDto.CopyMatchingPropertiesToNew<Log>();
        log.AsOf = DateTimeOffset.UtcNow;

        _context.Logs.Add(log);
        await _context.SaveChangesAsync();

        var logDto = log.CopyMatchingPropertiesToNew<AuditLogDto>();

        // _logger.LogInformation("Successfully created audit log {RowId}", log.RowId);

        return logDto;
    }
}



