using System.Text.Json.Serialization;

namespace Backend.Domain.Enumerations;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ResultSection
{
    Elected,
    Extra,
    Other
}

public static class ResultSectionEnum
{
    public const string Elected = "E";
    public const string Extra = "X";
    public const string Other = "O";

    public static readonly IReadOnlyList<string> AllCodes = new List<string>
    {
        Elected, Extra, Other
    };

    public static ResultSection? ParseCode(string? value) =>
        value switch
        {
            "E" => ResultSection.Elected,
            "X" => ResultSection.Extra,
            "O" => ResultSection.Other,
            _ => null
        };

    public static string? ToCodeString(ResultSection? value) =>
        value switch
        {
            ResultSection.Elected => "E",
            ResultSection.Extra => "X",
            ResultSection.Other => "O",
            _ => null
        };
}