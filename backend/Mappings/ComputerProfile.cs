using AutoMapper;
using Backend.DTOs.Computers;
using Backend.Domain.Entities;

namespace Backend.Mappings;

/// <summary>
/// AutoMapper profile for computer entity and DTO mappings.
/// </summary>
public class ComputerProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ComputerProfile"/> class.
    /// </summary>
    public ComputerProfile()
    {
        CreateMap<Computer, ComputerDto>();

        CreateMap<RegisterComputerDto, Computer>()
            .ForMember(dest => dest.ComputerGuid, opt => opt.Ignore())
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.LastActivity, opt => opt.Ignore())
            .ForMember(dest => dest.RegisteredAt, opt => opt.Ignore())
            .ForMember(dest => dest.IsActive, opt => opt.Ignore())
            .ForMember(dest => dest.Election, opt => opt.Ignore())
            .ForMember(dest => dest.Location, opt => opt.Ignore());

        CreateMap<UpdateComputerDto, Computer>()
            .ForMember(dest => dest.ComputerGuid, opt => opt.Ignore())
            .ForMember(dest => dest.ElectionGuid, opt => opt.Ignore())
            .ForMember(dest => dest.LocationGuid, opt => opt.Ignore())
            .ForMember(dest => dest.ComputerCode, opt => opt.Ignore())
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.LastActivity, opt => opt.Ignore())
            .ForMember(dest => dest.RegisteredAt, opt => opt.Ignore())
            .ForMember(dest => dest.Election, opt => opt.Ignore())
            .ForMember(dest => dest.Location, opt => opt.Ignore());
    }
}



