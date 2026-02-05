using AutoMapper;
using TallyJ4.Domain.Entities;
using TallyJ4.DTOs.Tellers;

namespace TallyJ4.Mappings;

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
