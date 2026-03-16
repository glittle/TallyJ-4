using Backend.DTOs.People;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
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
    }

    private static string? GetIneligibleReasonCode(Guid? guid)
    {
        return guid.HasValue ? IneligibleReasonEnum.GetByGuid(guid.Value)?.Code : null;
    }
}
