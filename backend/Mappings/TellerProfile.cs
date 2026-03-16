using Backend.Domain.Entities;
using Backend.DTOs.Tellers;
using Mapster;

namespace Backend.Mappings;

/// <summary>
/// AutoMapper profile for teller entity and DTO mappings.
/// </summary>
public class TellerProfile : IRegister
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TellerProfile"/> class.
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Teller, TellerDto>()
            .Map(dest => dest.IsHeadTeller, src => src.IsHeadTeller ?? false);

        config.NewConfig<CreateTellerDto, Teller>();

        config.NewConfig<UpdateTellerDto, Teller>();
    }
}
