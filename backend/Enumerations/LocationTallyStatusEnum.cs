using System.Text.Json.Serialization;

namespace Backend.Enumerations;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LocationTallyStatus
{
    NotStarted,
    InProgress,
    Complete
}
