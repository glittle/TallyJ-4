using AutoMapper;
using Microsoft.EntityFrameworkCore;
using TallyJ4.DTOs.Tellers;
using TallyJ4.Domain.Context;
using TallyJ4.Domain.Entities;
using TallyJ4.Models;

namespace TallyJ4.Services;

public class TellerService : ITellerService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<TellerService> _logger;

    public TellerService(MainDbContext context, IMapper mapper, ILogger<TellerService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<PaginatedResponse<TellerDto>> GetTellersByElectionAsync(
        Guid electionGuid,
        int pageNumber = 1,
        int pageSize = 50)
    {
        var query = _context.Tellers
            .Where(t => t.ElectionGuid == electionGuid)
            .AsQueryable();

        var totalCount = await query.CountAsync();

        var tellers = await query
            .OrderBy(t => t.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var tellerDtos = _mapper.Map<List<TellerDto>>(tellers);

        _logger.LogInformation(
            "Retrieved {Count} tellers for election {ElectionGuid} (page {PageNumber} of {TotalPages})",
            tellerDtos.Count,
            electionGuid,
            pageNumber,
            (totalCount + pageSize - 1) / pageSize);

        return PaginatedResponse<TellerDto>.Create(tellerDtos, pageNumber, pageSize, totalCount);
    }

    public async Task<TellerDto?> GetTellerByIdAsync(int rowId)
    {
        var teller = await _context.Tellers
            .Where(t => t.RowId == rowId)
            .FirstOrDefaultAsync();

        if (teller == null)
        {
            _logger.LogWarning("Teller with RowId {RowId} not found", rowId);
            return null;
        }

        var tellerDto = _mapper.Map<TellerDto>(teller);

        _logger.LogInformation("Retrieved teller {RowId}: {TellerName}", rowId, teller.Name);

        return tellerDto;
    }

    public async Task<TellerDto> CreateTellerAsync(CreateTellerDto createDto)
    {
        _logger.LogInformation("Creating new teller: {TellerName} for election {ElectionGuid}", createDto.Name, createDto.ElectionGuid);

        if (!await IsTellerNameUniqueAsync(createDto.ElectionGuid, createDto.Name))
        {
            throw new InvalidOperationException($"A teller with the name '{createDto.Name}' already exists for this election");
        }

        var teller = _mapper.Map<Teller>(createDto);

        _context.Tellers.Add(teller);
        await _context.SaveChangesAsync();

        var tellerDto = _mapper.Map<TellerDto>(teller);

        _logger.LogInformation("Successfully created teller {RowId}: {TellerName}", teller.RowId, teller.Name);

        return tellerDto;
    }

    public async Task<TellerDto?> UpdateTellerAsync(int rowId, UpdateTellerDto updateDto)
    {
        _logger.LogInformation("Updating teller {RowId}", rowId);

        var teller = await _context.Tellers
            .Where(t => t.RowId == rowId)
            .FirstOrDefaultAsync();

        if (teller == null)
        {
            _logger.LogWarning("Teller with RowId {RowId} not found for update", rowId);
            return null;
        }

        if (!await IsTellerNameUniqueAsync(teller.ElectionGuid, updateDto.Name, rowId))
        {
            throw new InvalidOperationException($"A teller with the name '{updateDto.Name}' already exists for this election");
        }

        _mapper.Map(updateDto, teller);
        await _context.SaveChangesAsync();

        var tellerDto = _mapper.Map<TellerDto>(teller);

        _logger.LogInformation("Successfully updated teller {RowId}: {TellerName}", teller.RowId, teller.Name);

        return tellerDto;
    }

    public async Task<bool> DeleteTellerAsync(int rowId)
    {
        _logger.LogInformation("Deleting teller {RowId}", rowId);

        var teller = await _context.Tellers
            .Where(t => t.RowId == rowId)
            .FirstOrDefaultAsync();

        if (teller == null)
        {
            _logger.LogWarning("Teller with RowId {RowId} not found for deletion", rowId);
            return false;
        }

        _context.Tellers.Remove(teller);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted teller {RowId}: {TellerName}", rowId, teller.Name);

        return true;
    }

    public async Task<bool> IsTellerNameUniqueAsync(Guid electionGuid, string name, int? excludeRowId = null)
    {
        var query = _context.Tellers
            .Where(t => t.ElectionGuid == electionGuid && t.Name == name);

        if (excludeRowId.HasValue)
        {
            query = query.Where(t => t.RowId != excludeRowId.Value);
        }

        return !await query.AnyAsync();
    }
}
