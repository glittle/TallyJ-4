using Mapster;
using Microsoft.EntityFrameworkCore;
using Backend.DTOs.Computers;
using Backend.Domain.Context;
using Backend.Domain.Entities;
using MapsterMapper;

namespace Backend.Services;

/// <summary>
/// Service implementation for managing computers registered for online voting.
/// </summary>
public class ComputerService : IComputerService
{
    private readonly MainDbContext _context;
    private readonly IMapper _mapper;
    private readonly ILogger<ComputerService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputerService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="mapper">The AutoMapper instance.</param>
    /// <param name="logger">The logger instance.</param>
    public ComputerService(MainDbContext context, IMapper mapper, ILogger<ComputerService> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ComputerDto>> GetComputersByLocationAsync(Guid locationGuid)
    {
        var computers = await _context.Computers
            .Where(c => c.LocationGuid == locationGuid)
            .OrderBy(c => c.ComputerCode)
            .ToListAsync();

        return _mapper.Map<List<ComputerDto>>(computers);
    }

    /// <inheritdoc />
    public async Task<List<ComputerDto>> GetComputersByElectionAsync(Guid electionGuid)
    {
        var computers = await _context.Computers
            .Where(c => c.ElectionGuid == electionGuid)
            .OrderBy(c => c.ComputerCode)
            .ToListAsync();

        return _mapper.Map<List<ComputerDto>>(computers);
    }

    /// <inheritdoc />
    public async Task<ComputerDto?> GetComputerByGuidAsync(Guid computerGuid)
    {
        var computer = await _context.Computers
            .Where(c => c.ComputerGuid == computerGuid)
            .FirstOrDefaultAsync();

        if (computer == null)
        {
            _logger.LogWarning("Computer with GUID {ComputerGuid} not found", computerGuid);
            return null;
        }

        return _mapper.Map<ComputerDto>(computer);
    }

    /// <inheritdoc />
    public async Task<ComputerDto?> GetComputerByCodeAsync(Guid electionGuid, string computerCode)
    {
        var computer = await _context.Computers
            .Where(c => c.ElectionGuid == electionGuid && c.ComputerCode == computerCode)
            .FirstOrDefaultAsync();

        if (computer == null)
        {
            return null;
        }

        return _mapper.Map<ComputerDto>(computer);
    }

    /// <inheritdoc />
    public async Task<ComputerDto> RegisterComputerAsync(RegisterComputerDto dto)
    {
        _logger.LogInformation("Registering computer for location {LocationGuid}", dto.LocationGuid);

        var computerCode = dto.ComputerCode;
        if (string.IsNullOrWhiteSpace(computerCode))
        {
            computerCode = await GenerateComputerCodeAsync(dto.ElectionGuid);
        }

        var existingComputer = await GetComputerByCodeAsync(dto.ElectionGuid, computerCode);
        if (existingComputer != null)
        {
            throw new InvalidOperationException($"Computer code '{computerCode}' is already in use for this election");
        }

        var computer = _mapper.Map<Computer>(dto);
        computer.ComputerGuid = Guid.NewGuid();
        computer.ComputerCode = computerCode;
        computer.IsActive = true;

        _context.Computers.Add(computer);
        await _context.SaveChangesAsync();

        var computerDto = _mapper.Map<ComputerDto>(computer);

        _logger.LogInformation("Successfully registered computer {ComputerCode} ({ComputerGuid})", computerCode, computer.ComputerGuid);

        return computerDto;
    }

    /// <inheritdoc />
    public async Task<ComputerDto?> UpdateComputerAsync(Guid computerGuid, UpdateComputerDto dto)
    {
        _logger.LogInformation("Updating computer {ComputerGuid}", computerGuid);

        var computer = await _context.Computers
            .Where(c => c.ComputerGuid == computerGuid)
            .FirstOrDefaultAsync();

        if (computer == null)
        {
            _logger.LogWarning("Computer with GUID {ComputerGuid} not found for update", computerGuid);
            return null;
        }

        _mapper.Map(dto, computer);
        await _context.SaveChangesAsync();

        var computerDto = _mapper.Map<ComputerDto>(computer);

        _logger.LogInformation("Successfully updated computer {ComputerGuid}", computerGuid);

        return computerDto;
    }

    /// <inheritdoc />
    public async Task<bool> DeleteComputerAsync(Guid computerGuid)
    {
        _logger.LogInformation("Deleting computer {ComputerGuid}", computerGuid);

        var computer = await _context.Computers
            .Where(c => c.ComputerGuid == computerGuid)
            .FirstOrDefaultAsync();

        if (computer == null)
        {
            _logger.LogWarning("Computer with GUID {ComputerGuid} not found for deletion", computerGuid);
            return false;
        }

        _context.Computers.Remove(computer);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Successfully deleted computer {ComputerCode} ({ComputerGuid})", computer.ComputerCode, computerGuid);

        return true;
    }

    /// <inheritdoc />
    public async Task<ComputerDto?> UpdateActivityAsync(Guid computerGuid)
    {
        var computer = await _context.Computers
            .Where(c => c.ComputerGuid == computerGuid)
            .FirstOrDefaultAsync();

        if (computer == null)
        {
            return null;
        }

        computer.LastActivity = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return _mapper.Map<ComputerDto>(computer);
    }

    /// <inheritdoc />
    public async Task<string> GenerateComputerCodeAsync(Guid electionGuid)
    {
        var existingCodes = await _context.Computers
            .Where(c => c.ElectionGuid == electionGuid)
            .Select(c => c.ComputerCode)
            .ToListAsync();

        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        for (int i = 0; i < chars.Length; i++)
        {
            for (int j = 0; j < chars.Length; j++)
            {
                var code = $"{chars[i]}{chars[j]}";
                if (!existingCodes.Contains(code))
                {
                    return code;
                }
            }
        }

        throw new InvalidOperationException("No available computer codes remaining for this election");
    }
}



