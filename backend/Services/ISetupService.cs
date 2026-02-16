using Backend.DTOs.Elections;
using Backend.DTOs.Setup;

namespace Backend.Services;

/// <summary>
/// Service interface for election setup operations including initial creation and configuration.
/// </summary>
public interface ISetupService
{
    /// <summary>
    /// Creates a new election with basic information in the first setup step.
    /// </summary>
    /// <param name="step1Dto">The election creation data for step 1.</param>
    /// <returns>The created election data.</returns>
    Task<ElectionDto> CreateElectionStep1Async(ElectionStep1Dto step1Dto);

    /// <summary>
    /// Configures additional election settings in the second setup step.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to configure.</param>
    /// <param name="step2Dto">The election configuration data for step 2.</param>
    /// <returns>The updated election data, or null if the election was not found.</returns>
    Task<ElectionDto?> ConfigureElectionStep2Async(Guid electionGuid, ElectionStep2Dto step2Dto);

    /// <summary>
    /// Retrieves the current setup status for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The election setup status, or null if the election was not found.</returns>
    Task<ElectionSetupStatusDto?> GetSetupStatusAsync(Guid electionGuid);
}



