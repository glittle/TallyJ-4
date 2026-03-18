using Backend.DTOs.FrontDesk;
using Backend.Domain.Entities;
using Mapster;
using System.Text.Json;

namespace Backend.Mappings;

/// <summary>
/// Mapster profile for front desk entity and DTO mappings.
/// </summary>
public class FrontDeskProfile : IRegister
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FrontDeskProfile"/> class.
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Person, FrontDeskVoterDto>()
            .Map(dest => dest.RegistrationHistory, src => DeserializeRegistrationHistory(src.RegistrationHistory, src.PersonGuid));
    }

    private static List<RegistrationHistoryEntryDto>? DeserializeRegistrationHistory(string? json, Guid personGuid)
    {
        if (string.IsNullOrEmpty(json))
            return null;

        try
        {
            return JsonSerializer.Deserialize<List<RegistrationHistoryEntryDto>>(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Failed to deserialize RegistrationHistory for Person {personGuid}: {ex.Message}. JSON: {json}", ex);
        }
    }
}
