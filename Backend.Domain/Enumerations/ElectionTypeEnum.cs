using System.Text.Json.Serialization;

namespace Backend.Domain.Enumerations;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ElectionTypeCode
{
    LSA,
    LSA1,
    LSA2,
    NSA,
    Con,
    Reg,
    Oth
}

public static class ElectionTypeEnum
{
    public static readonly ElectionType LSA = new("LSA", "Local Spiritual Assembly");
    // public static readonly ElectionType LSA1 = new("LSA1", "Local Spiritual Assembly (Two-Stage) Local Unit");
    // public static readonly ElectionType LSA2 = new("LSA2", "Local Spiritual Assembly (Two-Stage) Final");
    public static readonly ElectionType NSA = new("NSA", "National Spiritual Assembly");
    public static readonly ElectionType Con = new("Con", "Unit Convention");
    public static readonly ElectionType Reg = new("Reg", "Regional Council");
    public static readonly ElectionType Oth = new("Oth", "Other");

    public static readonly IReadOnlyList<ElectionType> All = new List<ElectionType>
    {
        LSA, /*LSA1, LSA2,*/ NSA, Con, Reg, Oth
    };

    public static readonly IReadOnlyList<string> AllCodes = All.Select(x => x.Code).ToList();

    public static ElectionTypeCode? ParseCode(string? value) =>
        Enum.TryParse<ElectionTypeCode>(value, out var result) ? result : null;

    public static string? ToCodeString(ElectionTypeCode? value) =>
        value?.ToString();
}

public class ElectionType
{
    public ElectionType(string code, string description)
    {
        Code = code;
        Description = description;
    }

    public string Code { get; }
    public string Description { get; }
    public override string ToString() => Code;
}

