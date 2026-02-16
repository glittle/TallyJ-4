using AutoMapper;
using Backend.DTOs.FrontDesk;
using Backend.Domain.Entities;

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
        CreateMap<Person, FrontDeskVoterDto>();
    }
}



