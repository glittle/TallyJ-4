using AutoMapper;
using Backend.DTOs.FrontDesk;
using Backend.Domain.Entities;
using System.Text.Json;

namespace Backend.Mappings;

/// <summary>
/// AutoMapper profile for front desk entity and DTO mappings.
/// </summary>
public class FrontDeskProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FrontDeskProfile"/> class.
    /// </summary>
    public FrontDeskProfile()
    {
        CreateMap<Person, FrontDeskVoterDto>()
            .ForMember(dest => dest.RegistrationHistory,
                opt => opt.MapFrom(src => string.IsNullOrEmpty(src.RegistrationHistory)
                    ? null
                    : JsonSerializer.Deserialize<List<RegistrationHistoryEntryDto>>(src.RegistrationHistory)));
    }
}



