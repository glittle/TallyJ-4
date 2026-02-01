using AutoMapper;
using TallyJ4.DTOs.FrontDesk;
using TallyJ4.Domain.Entities;

namespace TallyJ4.Mappings;

public class FrontDeskProfile : Profile
{
    public FrontDeskProfile()
    {
        CreateMap<Person, FrontDeskVoterDto>();
    }
}
