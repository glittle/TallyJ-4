using Backend.DTOs.FrontDesk;
using Backend.Domain.Entities;
using Mapster;
using System.Text.Json;

namespace Backend.Mappings;

/// <summary>
/// AutoMapper profile for front desk entity and DTO mappings.
/// </summary>
public class FrontDeskProfile : IRegister
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FrontDeskProfile"/> class.
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Person, FrontDeskVoterDto>()
            .Map(dest => dest.RegistrationHistory, src =>
                string.IsNullOrEmpty(src.RegistrationHistory)
                    ? null
                    : JsonSerializer.Deserialize<List<RegistrationHistoryEntryDto>>(src.RegistrationHistory));
    }
}
