using Backend.Context;
using Backend.DTOs.Computers;
using Backend.Entities;
using Backend.Helpers;
using Microsoft.EntityFrameworkCore;

namespace Backend.Services;

/// <summary>
/// Service implementation for managing computers registered for online voting.
/// </summary>
public class ComputerService : IComputerService
{
    private readonly MainDbContext _context;
    private readonly ILogger<ComputerService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ComputerService"/> class.
    /// </summary>
    public ComputerService(MainDbContext context, ILogger<ComputerService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ComputerDto>> GetComputersByLocationAsync(Guid locationGuid)
    {
        var computers = await _context.Computers
            .Where(c => c.LocationGuid == locationGuid)
            .OrderBy(c => c.ComputerCode)
            .ToListAsync();

        return computers.Select(c => c.CopyMatchingPropertiesToNew<ComputerDto>()).ToList();
    }

    /// <inheritdoc />
    public async Task<List<ComputerDto>> GetComputersByElectionAsync(Guid electionGuid)
    {
        var computers = await _context.Computers
            .Where(c => c.ElectionGuid == electionGuid)
            .OrderBy(c => c.ComputerCode)
            .ToListAsync();

        return computers.Select(c => c.CopyMatchingPropertiesToNew<ComputerDto>()).ToList();
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

        return computer.CopyMatchingPropertiesToNew<ComputerDto>();
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

        return computer.CopyMatchingPropertiesToNew<ComputerDto>();
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

        dto.CopyMatchingPropertiesTo(computer);
        await _context.SaveChangesAsync();

        var computerDto = computer.CopyMatchingPropertiesToNew<ComputerDto>();

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

        computer.LastActivity = DateTimeOffset.UtcNow;
        await _context.SaveChangesAsync();

        return computer.CopyMatchingPropertiesToNew<ComputerDto>();
    }

    /// <inheritdoc />
    public async Task<string> GenerateComputerCodeAsync(Guid electionGuid)
    {
        var existingCodes = await _context.Computers
            .Where(c => c.ElectionGuid == electionGuid)
            .Select(c => c.ComputerCode)
            .ToListAsync();

        for (var index = 0; index <= ComputerCodeHelper.CodeToIndex("ZZ"); index++)
        {
            var code = ComputerCodeHelper.IndexToCode(index);
            if (!existingCodes.Contains(code))
            {
                return code;
            }
        }

        throw new InvalidOperationException("No available computer codes remaining for this election");
    }
}



