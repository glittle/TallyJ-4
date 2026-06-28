using System.Text.Json.Serialization;

namespace Backend.Enumerations;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BallotStatus
{
    /// <summary>
    /// The ballot is valid and has no issues.
    /// </summary>
    Ok,
    /// <summary>
    /// The ballot has issues that require review.
    /// </summary>
    Review,
    /// <summary>
    /// The ballot has issues that require verification.
    /// </summary>
    Verify,
    /// <summary>
    /// The ballot has too many names.
    /// </summary>
    TooMany,
    /// <summary>
    /// The ballot has too few names.
    /// </summary>
    TooFew,
    /// <summary>
    /// The ballot has duplicate names.
    /// </summary>
    Dup,
    /// <summary>
    /// The ballot is empty.
    /// </summary>
    Empty,
    /// <summary>
    /// The ballot is raw (from an online or imported source) and needs to be finished by the teller.
    /// </summary>
    Raw
}

public static class BallotStatusEnum
{
    public static readonly BallotStatusInfo Ok = new(BallotStatus.Ok, "Ok");
    public static readonly BallotStatusInfo Review = new(BallotStatus.Review, "Needs Review");
    public static readonly BallotStatusInfo Verify = new(BallotStatus.Verify, "Needs Verification");
    public static readonly BallotStatusInfo TooMany = new(BallotStatus.TooMany, "Too Many");
    public static readonly BallotStatusInfo TooFew = new(BallotStatus.TooFew, "Too Few");
    public static readonly BallotStatusInfo Dup = new(BallotStatus.Dup, "Duplicate names");
    public static readonly BallotStatusInfo Empty = new(BallotStatus.Empty, "Empty");
    public static readonly BallotStatusInfo Raw = new(BallotStatus.Raw, "Raw - Teller to Finish");

    public static readonly IReadOnlyList<BallotStatusInfo> All = new List<BallotStatusInfo>
    {
        Ok, Review, Verify, TooMany, TooFew, Dup, Empty, Raw
    };

    /// <summary>
    /// Legacy v3/v4 status stored in older databases and imports.
    /// </summary>
    public const string LegacyNewCode = "New";

    public static BallotStatus? ParseCode(string? value)
    {
        if (string.Equals(value, LegacyNewCode, StringComparison.OrdinalIgnoreCase))
        {
            return BallotStatus.Empty;
        }

        return Enum.TryParse<BallotStatus>(value, out var result) ? result : null;
    }

    public static string? ToCodeString(BallotStatus? value) =>
        value?.ToString();

    public static string? GetDescription(BallotStatus? value) =>
        value == null ? null : All.FirstOrDefault(x => x.Code == value)?.Description;
}

public class BallotStatusInfo
{
    public BallotStatusInfo(BallotStatus code, string description)
    {
        Code = code;
        Description = description;
    }

    public BallotStatus Code { get; }
    public string Description { get; }
    public override string ToString() => Code.ToString();
}
