namespace Backend.Enumerations;

/// <summary>
/// Codes stored in <c>Election.OnlineSelectionProcess</c>.
/// v4 uses A/B/C (not v3's L/R/B).
/// </summary>
public static class OnlineSelectionProcessEnum
{
    public static readonly OnlineSelectionProcess List = new("A", "List");
    public static readonly OnlineSelectionProcess Random = new("B", "Random");
    public static readonly OnlineSelectionProcess Both = new("C", "Both");

    public static readonly IReadOnlyList<OnlineSelectionProcess> All = new List<OnlineSelectionProcess>
    {
        List, Random, Both
    };

    public static readonly IReadOnlyList<string> AllCodes = All.Select(x => x.Code).ToList();

    public static bool IsValid(string? value) =>
        string.IsNullOrEmpty(value) || AllCodes.Contains(value);
}

public class OnlineSelectionProcess
{
    public OnlineSelectionProcess(string code, string description)
    {
        Code = code;
        Description = description;
    }

    public string Code { get; }
    public string Description { get; }
    public override string ToString() => Code;
}
