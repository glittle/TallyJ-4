using AutoMapper;
using TallyJ4.DTOs.FrontDesk;
using TallyJ4.Domain.Entities;

namespace TallyJ4.Mappings;

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
