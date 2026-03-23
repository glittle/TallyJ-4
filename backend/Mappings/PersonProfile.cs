using System.Text.Json;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Backend.DTOs.FrontDesk;
using Backend.DTOs.People;
using Mapster;

namespace Backend.Mappings;

/// <summary>
/// Mapping registration for person-related mappings using Mapster.
/// Defines mappings between person entities and DTOs.
/// </summary>
public class PersonProfile : IRegister
{
    /// <summary>
    /// Registers mappings between Person entities and various DTOs.
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Person, PersonDto>()
            .Map(dest => dest.IneligibleReasonCode, src => GetIneligibleReasonCode(src.IneligibleReasonGuid));

        config.NewConfig<Person, PersonListDto>()
            .Map(dest => dest.IneligibleReasonCode, src => GetIneligibleReasonCode(src.IneligibleReasonGuid));

        config.NewConfig<Person, PersonDetailDto>()
            .Map(dest => dest.IneligibleReasonCode, src => GetIneligibleReasonCode(src.IneligibleReasonGuid));

        config.NewConfig<CreatePersonDto, Person>();

        config.NewConfig<UpdatePersonDto, Person>();

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

    private static string? GetIneligibleReasonCode(Guid? guid)
    {
        return guid.HasValue ? IneligibleReasonEnum.GetByGuid(guid.Value)?.Code : null;
    }
}
