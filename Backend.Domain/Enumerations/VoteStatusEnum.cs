using System.Text.Json.Serialization;

namespace Backend.Domain.Enumerations;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VoteStatus
{
    Ok,
    Changed, // the person's name or other info has changed since this vote was recorded
    Spoiled, // the associated person cannot receive votes, or there is no associated person
    Raw // this vote was received via import or online and has not been finalized by a teller
}

public static class VoteStatusEnum
{
    public static readonly VoteStatusInfo Ok = new(VoteStatus.Ok, "Ok");
    public static readonly VoteStatusInfo Changed = new(VoteStatus.Changed, "Changed");
    public static readonly VoteStatusInfo Spoiled = new(VoteStatus.Spoiled, "Spoiled");
    public static readonly VoteStatusInfo Raw = new(VoteStatus.Raw, "Raw");

    public static readonly IReadOnlyList<VoteStatusInfo> All = new List<VoteStatusInfo>
    {
        Ok, Changed, Spoiled, Raw
    };

    public static VoteStatus? ParseCode(string? value) =>
        Enum.TryParse<VoteStatus>(value, out var result) ? result : null;

    public static string? ToCodeString(VoteStatus? value) =>
        value?.ToString();
}

public class VoteStatusInfo
{
    public VoteStatusInfo(VoteStatus code, string description)
    {
        Code = code;
        Description = description;
    }

    public VoteStatus Code { get; }
    public string Description { get; }
    public override string ToString() => Code.ToString();
}
