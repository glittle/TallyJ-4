using Backend.DTOs.Locations;
using Backend.Domain.Entities;
using Mapster;

namespace Backend.Mappings;

/// <summary>
/// AutoMapper profile for location-related mappings.
/// Defines mappings between location entities and DTOs.
/// </summary>
public class LocationProfile : IRegister
{
    /// <summary>
    /// Initializes a new instance of the LocationProfile.
    /// Configures mappings between Location entities and various DTOs.
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Location, LocationDto>()
            .Map(dest => dest.Longitude, src => src.Long)
            .Map(dest => dest.Latitude, src => src.Lat);

        config.NewConfig<CreateLocationDto, Location>()
            .Map(dest => dest.Long, src => src.Longitude)
            .Map(dest => dest.Lat, src => src.Latitude);

        config.NewConfig<UpdateLocationDto, Location>()
            .Map(dest => dest.Long, src => src.Longitude)
            .Map(dest => dest.Lat, src => src.Latitude);
    }
}
