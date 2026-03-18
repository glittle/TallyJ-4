using Backend.DTOs.Votes;
using Backend.Domain.Entities;
using Mapster;

namespace Backend.Mappings;

/// <summary>
/// Mapster profile for vote-related mappings.
/// Defines mappings between vote entities and DTOs.
/// </summary>
public class VoteProfile : IRegister
{
    /// <summary>
    /// Initializes a new instance of the VoteProfile.
    /// Configures mappings between Vote entities and DTOs.
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Vote, VoteDto>()
            .Map(dest => dest.PersonFullName, src => src.Person != null ? src.Person.FullName : null);

        config.NewConfig<CreateVoteDto, Vote>();
    }
}
