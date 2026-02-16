using AutoMapper;
using Backend.Domain.Entities;
using Backend.DTOs.Tellers;

namespace Backend.Mappings;

/// <summary>
/// AutoMapper profile for teller entity and DTO mappings.
/// </summary>
public class TellerProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TellerProfile"/> class.
    /// </summary>
    public TellerProfile()
    {
        CreateMap<Teller, TellerDto>()
            .ForMember(dest => dest.IsHeadTeller, opt => opt.MapFrom(src => src.IsHeadTeller ?? false));

        CreateMap<CreateTellerDto, Teller>()
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Election, opt => opt.Ignore());

        CreateMap<UpdateTellerDto, Teller>()
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.ElectionGuid, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Election, opt => opt.Ignore());
    }
}



