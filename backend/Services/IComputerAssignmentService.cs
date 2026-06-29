using Backend.DTOs.Computers;

namespace Backend.Services;

/// <summary>
/// Tracks active teller workstation connections and assigns sequential computer codes per election.
/// </summary>
public interface IComputerAssignmentService
{
    /// <summary>
    /// Assigns or re-assigns a computer code for a client joining an election.
    /// </summary>
    string AssignCode(Guid electionGuid, string clientId, string connectionId, bool isMainTeller);

    /// <summary>
    /// Releases a connection and updates active/main-teller tracking.
    /// </summary>
    void ReleaseConnection(string connectionId);

    /// <summary>
    /// Returns currently active computers for monitoring.
    /// </summary>
    IReadOnlyList<ActiveComputerDto> GetActiveComputers(Guid electionGuid);

    /// <summary>
    /// Whether at least one main (full) teller is connected to the election.
    /// </summary>
    bool HasActiveMainTeller(Guid electionGuid);

    /// <summary>
    /// Whether the post-main-disconnect guest grace period is active (60-minute close-out timer armed).
    /// </summary>
    bool IsGuestGracePeriodActive(Guid electionGuid);

    /// <summary>
    /// Whether a guest teller may join or reconnect (main present, or grace period after last main left).
    /// </summary>
    bool CanGuestJoin(Guid electionGuid);
}