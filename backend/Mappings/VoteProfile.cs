using AutoMapper;
using TallyJ4.DTOs.Votes;
using TallyJ4.EF.Models;

namespace TallyJ4.Mappings;

public class VoteProfile : Profile
{
    public VoteProfile()
    {
        CreateMap<Vote, VoteDto>()
            .ForMember(dest => dest.PersonFullName, opt => opt.MapFrom(src => src.Person != null ? src.Person.FullName : null));
        
        CreateMap<CreateVoteDto, Vote>();
    }
}
