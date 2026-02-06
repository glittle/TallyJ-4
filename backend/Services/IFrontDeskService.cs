using TallyJ4.DTOs.FrontDesk;

namespace TallyJ4.Services;

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
}
