using AutoMapper;
using Backend.DTOs.Votes;
using Backend.Domain.Entities;

namespace Backend.Mappings;

/// <summary>
/// AutoMapper profile for vote-related mappings.
/// Defines mappings between vote entities and DTOs.
/// </summary>
public class VoteProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the VoteProfile.
    /// Configures mappings between Vote entities and DTOs.
    /// </summary>
    public VoteProfile()
    {
        CreateMap<Vote, VoteDto>()
            .ForMember(dest => dest.PersonFullName, opt => opt.MapFrom(src => src.Person != null ? src.Person.FullName : null));

        CreateMap<CreateVoteDto, Vote>();
    }
}



