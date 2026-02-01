using AutoMapper;
using TallyJ4.Domain.Entities;
using TallyJ4.DTOs.Tellers;

namespace TallyJ4.Mappings;

public class TellerProfile : Profile
{
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
