using System.Text.Json.Serialization;

namespace Backend.Domain.Enumerations;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LocationTallyStatus
{
    NotStarted,
    InProgress,
    Complete
}
