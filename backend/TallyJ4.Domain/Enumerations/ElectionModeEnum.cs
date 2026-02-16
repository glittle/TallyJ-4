using System.Text.Json.Serialization;

namespace TallyJ4.Domain.Enumerations;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ElectionModeCode
{
    N,
    T,
    B
}

public static class ElectionModeEnum
{
    public static readonly ElectionMode Normal = new("N", "Normal Election");
    public static readonly ElectionMode Tie = new("T", "Tie-Break");
    public static readonly ElectionMode ByElection = new("B", "By-election");

    public static readonly IReadOnlyList<ElectionMode> All = new List<ElectionMode>
    {
        Normal, Tie, ByElection
    };

    public static readonly IReadOnlyList<string> AllCodes = All.Select(x => x.Code).ToList();

    public static ElectionModeCode? ParseCode(string? value) =>
        Enum.TryParse<ElectionModeCode>(value, out var result) ? result : null;

    public static string? ToCodeString(ElectionModeCode? value) =>
        value?.ToString();
}

public class ElectionMode
{
    public ElectionMode(string code, string description)
    {
        Code = code;
        Description = description;
    }

    public string Code { get; }
    public string Description { get; }
    public override string ToString() => Code;
}