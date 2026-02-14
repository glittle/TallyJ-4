namespace TallyJ4.Domain.Enumerations;

/// <summary>
/// Enumeration of election types used in TallyJ.
/// </summary>
public static class ElectionTypeEnum
{
    /// <summary>
    /// Local Spiritual Assembly
    /// </summary>
    public static readonly ElectionType LSA = new("LSA", "Local Spiritual Assembly");

    /// <summary>
    /// Local Spiritual Assembly (Two-Stage) Local Unit
    /// </summary>
    public static readonly ElectionType LSA1 = new("LSA1", "Local Spiritual Assembly (Two-Stage) Local Unit");

    /// <summary>
    /// Local Spiritual Assembly (Two-Stage) Final
    /// </summary>
    public static readonly ElectionType LSA2 = new("LSA2", "Local Spiritual Assembly (Two-Stage) Final");

    /// <summary>
    /// National Spiritual Assembly
    /// </summary>
    public static readonly ElectionType NSA = new("NSA", "National Spiritual Assembly");

    /// <summary>
    /// Unit Convention
    /// </summary>
    public static readonly ElectionType Con = new("Con", "Unit Convention");

    /// <summary>
    /// Regional Council
    /// </summary>
    public static readonly ElectionType Reg = new("Reg", "Regional Council");

    /// <summary>
    /// Other
    /// </summary>
    public static readonly ElectionType Oth = new("Oth", "Other");

    /// <summary>
    /// All election types
    /// </summary>
    public static readonly IReadOnlyList<ElectionType> All = new List<ElectionType>
    {
        LSA, LSA1, LSA2, NSA, Con, Reg, Oth
    };

    /// <summary>
    /// All election type codes
    /// </summary>
    public static readonly IReadOnlyList<string> AllCodes = All.Select(x => x.Code).ToList();
}

/// <summary>
/// Represents an election type with a code and description.
/// </summary>
public class ElectionType
{
    /// <summary>
    /// Initializes a new instance of the ElectionType class.
    /// </summary>
    /// <param name="code">The election type code.</param>
    /// <param name="description">The election type description.</param>
    public ElectionType(string code, string description)
    {
        Code = code;
        Description = description;
    }

    /// <summary>
    /// The election type code.
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// The election type description.
    /// </summary>
    public string Description { get; }

    /// <summary>
    /// Returns the election type code.
    /// </summary>
    public override string ToString() => Code;
}