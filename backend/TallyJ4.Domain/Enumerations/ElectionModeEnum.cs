namespace TallyJ4.Domain.Enumerations;

/// <summary>
/// Enumeration of election modes used in TallyJ.
/// </summary>
public static class ElectionModeEnum
{
    /// <summary>
    /// Normal Election
    /// </summary>
    public static readonly ElectionMode Normal = new("N", "Normal Election");

    /// <summary>
    /// Tie-Break
    /// </summary>
    public static readonly ElectionMode Tie = new("T", "Tie-Break");

    /// <summary>
    /// By-election
    /// </summary>
    public static readonly ElectionMode ByElection = new("B", "By-election");

    /// <summary>
    /// All election modes
    /// </summary>
    public static readonly IReadOnlyList<ElectionMode> All = new List<ElectionMode>
    {
        Normal, Tie, ByElection
    };

    /// <summary>
    /// All election mode codes
    /// </summary>
    public static readonly IReadOnlyList<string> AllCodes = All.Select(x => x.Code).ToList();
}

/// <summary>
/// Represents an election mode with a code and description.
/// </summary>
public class ElectionMode
{
    /// <summary>
    /// Initializes a new instance of the ElectionMode class.
    /// </summary>
    /// <param name="code">The election mode code.</param>
    /// <param name="description">The election mode description.</param>
    public ElectionMode(string code, string description)
    {
        Code = code;
        Description = description;
    }

    /// <summary>
    /// The election mode code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// The election mode description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Returns the election mode code.
    /// </summary>
    public override string ToString() => Code;
}