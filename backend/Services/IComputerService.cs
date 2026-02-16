using Backend.DTOs.Computers;

namespace Backend.Services;

/// <summary>
/// Service for managing computers registered for online voting.
/// </summary>
public interface IComputerService
{
    /// <summary>
    /// Retrieves all computers registered at a specific location.
    /// </summary>
    /// <param name="locationGuid">The unique identifier of the location.</param>
    /// <returns>A list of computer DTOs.</returns>
    Task<List<ComputerDto>> GetComputersByLocationAsync(Guid locationGuid);

    /// <summary>
    /// Retrieves all computers registered for a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A list of computer DTOs.</returns>
    Task<List<ComputerDto>> GetComputersByElectionAsync(Guid electionGuid);

    /// <summary>
    /// Retrieves a computer by its unique identifier.
    /// </summary>
    /// <param name="computerGuid">The unique identifier of the computer.</param>
    /// <returns>The computer DTO if found, otherwise null.</returns>
    Task<ComputerDto?> GetComputerByGuidAsync(Guid computerGuid);

    /// <summary>
    /// Retrieves a computer by its code within a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="computerCode">The computer code.</param>
    /// <returns>The computer DTO if found, otherwise null.</returns>
    Task<ComputerDto?> GetComputerByCodeAsync(Guid electionGuid, string computerCode);

    /// <summary>
    /// Registers a new computer for online voting.
    /// </summary>
    /// <param name="dto">The computer registration data.</param>
    /// <returns>The registered computer DTO.</returns>
    Task<ComputerDto> RegisterComputerAsync(RegisterComputerDto dto);

    /// <summary>
    /// Updates an existing computer's information.
    /// </summary>
    /// <param name="computerGuid">The unique identifier of the computer to update.</param>
    /// <param name="dto">The computer update data.</param>
    /// <returns>The updated computer DTO if found, otherwise null.</returns>
    Task<ComputerDto?> UpdateComputerAsync(Guid computerGuid, UpdateComputerDto dto);

    /// <summary>
    /// Deletes a computer registration.
    /// </summary>
    /// <param name="computerGuid">The unique identifier of the computer to delete.</param>
    /// <returns>True if the computer was deleted, false if not found.</returns>
    Task<bool> DeleteComputerAsync(Guid computerGuid);

    /// <summary>
    /// Updates the last activity timestamp for a computer.
    /// </summary>
    /// <param name="computerGuid">The unique identifier of the computer.</param>
    /// <returns>The updated computer DTO if found, otherwise null.</returns>
    Task<ComputerDto?> UpdateActivityAsync(Guid computerGuid);

    /// <summary>
    /// Generates a unique computer code for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A unique computer code.</returns>
    Task<string> GenerateComputerCodeAsync(Guid electionGuid);
}



