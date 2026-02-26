using Backend.DTOs.FrontDesk;

namespace Backend.Services;

/// <summary>
/// Service for managing front desk operations including voter check-in and roll call.
/// </summary>
public interface IFrontDeskService
{
    /// <summary>
    /// Retrieves all eligible voters for a specific election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>A list of eligible voter DTOs.</returns>
    Task<List<FrontDeskVoterDto>> GetEligibleVotersAsync(Guid electionGuid);

    /// <summary>
    /// Checks in a voter for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="checkInDto">The voter check-in data.</param>
    /// <returns>The updated voter DTO.</returns>
    Task<FrontDeskVoterDto> CheckInVoterAsync(Guid electionGuid, CheckInVoterDto checkInDto);

    /// <summary>
    /// Retrieves the roll call information including voters and statistics.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The roll call DTO.</returns>
    Task<RollCallDto> GetRollCallAsync(Guid electionGuid);

    /// <summary>
    /// Retrieves front desk statistics for an election.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <returns>The front desk statistics DTO.</returns>
    Task<FrontDeskStatsDto> GetStatsAsync(Guid electionGuid);

    /// <summary>
    /// Unregisters a voter (removes their check-in status).
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="unregisterDto">The unregister data.</param>
    /// <returns>The updated voter DTO.</returns>
    Task<FrontDeskVoterDto> UnregisterVoterAsync(Guid electionGuid, UnregisterVoterDto unregisterDto);

    /// <summary>
    /// Updates the flags for a person.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="updateFlagsDto">The flags update data.</param>
    /// <returns>The updated voter DTO.</returns>
    Task<FrontDeskVoterDto> UpdatePersonFlagsAsync(Guid electionGuid, UpdatePersonFlagsDto updateFlagsDto);
}



