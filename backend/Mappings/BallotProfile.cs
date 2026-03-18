using Backend.DTOs.Ballots;
using Backend.Domain.Entities;
using Mapster;

namespace Backend.Mappings;

/// <summary>
/// Mapster profile for ballot-related mappings.
/// Defines mappings between ballot entities and DTOs.
/// </summary>
public class BallotProfile : IRegister
{
    /// <summary>
    /// Initializes a new instance of the BallotProfile.
    /// Configures mappings between Ballot entities and various DTOs.
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Ballot, BallotDto>();

        config.NewConfig<CreateBallotDto, Ballot>();

        config.NewConfig<UpdateBallotDto, Ballot>();
    }
}
