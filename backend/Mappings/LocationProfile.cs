using AutoMapper;
using TallyJ4.DTOs.Locations;
using TallyJ4.Domain.Entities;

namespace TallyJ4.Mappings;

/// <summary>
/// AutoMapper profile for location-related mappings.
/// Defines mappings between location entities and DTOs.
/// </summary>
public class LocationProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the LocationProfile.
    /// Configures mappings between Location entities and various DTOs.
    /// </summary>
    public LocationProfile()
    {
        CreateMap<Location, LocationDto>()
            .ForMember(dest => dest.Longitude, opt => opt.MapFrom(src => src.Long))
            .ForMember(dest => dest.Latitude, opt => opt.MapFrom(src => src.Lat));

        CreateMap<CreateLocationDto, Location>()
            .ForMember(dest => dest.LocationGuid, opt => opt.Ignore())
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.TallyStatus, opt => opt.Ignore())
            .ForMember(dest => dest.BallotsCollected, opt => opt.Ignore())
            .ForMember(dest => dest.Long, opt => opt.MapFrom(src => src.Longitude))
            .ForMember(dest => dest.Lat, opt => opt.MapFrom(src => src.Latitude))
            .ForMember(dest => dest.Election, opt => opt.Ignore())
            .ForMember(dest => dest.Ballots, opt => opt.Ignore());

        CreateMap<UpdateLocationDto, Location>()
            .ForMember(dest => dest.LocationGuid, opt => opt.Ignore())
            .ForMember(dest => dest.ElectionGuid, opt => opt.Ignore())
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.TallyStatus, opt => opt.Ignore())
            .ForMember(dest => dest.BallotsCollected, opt => opt.Ignore())
            .ForMember(dest => dest.Long, opt => opt.MapFrom(src => src.Longitude))
            .ForMember(dest => dest.Lat, opt => opt.MapFrom(src => src.Latitude))
            .ForMember(dest => dest.Election, opt => opt.Ignore())
            .ForMember(dest => dest.Ballots, opt => opt.Ignore());
    }
}
