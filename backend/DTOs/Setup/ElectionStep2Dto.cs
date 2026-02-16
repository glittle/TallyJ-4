using Backend.Domain.Enumerations;

namespace Backend.DTOs.Setup;

/// <summary>
/// Data transfer object for election setup step 2.
/// Contains configuration details for election type, mode, and number of positions to elect.
/// </summary>
public class ElectionStep2Dto
{
    /// <summary>
    /// The unique identifier for the election.
    /// </summary>
    public Guid ElectionGuid { get; set; }

    /// <summary>
    /// The number of positions to be elected in this election.
    /// </summary>
    public int NumberToElect { get; set; }

    /// <summary>
    /// The type of election (LSA, LSA1, LSA2, NSA, Con, Reg, Oth).
    /// </summary>
    public ElectionTypeCode ElectionType { get; set; }

    /// <summary>
    /// The mode of the election (N=Normal, T=Tie-Break, B=By-election).
    /// </summary>
    public ElectionModeCode ElectionMode { get; set; }
}



