using AutoMapper;
using TallyJ4.DTOs.Ballots;
using TallyJ4.Domain.Entities;

namespace TallyJ4.Mappings;

/// <summary>
/// AutoMapper profile for ballot-related mappings.
/// Defines mappings between ballot entities and DTOs.
/// </summary>
public class BallotProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the BallotProfile.
    /// Configures mappings between Ballot entities and various DTOs.
    /// </summary>
    public BallotProfile()
    {
        CreateMap<Ballot, BallotDto>()
            .ForMember(dest => dest.LocationName, opt => opt.Ignore())
            .ForMember(dest => dest.BallotCode, opt => opt.Ignore())
            .ForMember(dest => dest.VoteCount, opt => opt.Ignore())
            .ForMember(dest => dest.Votes, opt => opt.Ignore());

        CreateMap<CreateBallotDto, Ballot>()
            .ForMember(dest => dest.BallotGuid, opt => opt.Ignore())
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.StatusCode, opt => opt.Ignore())
            .ForMember(dest => dest.BallotNumAtComputer, opt => opt.Ignore())
            .ForMember(dest => dest.BallotCode, opt => opt.Ignore())
            .ForMember(dest => dest.Teller1, opt => opt.Ignore())
            .ForMember(dest => dest.Teller2, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Location, opt => opt.Ignore())
            .ForMember(dest => dest.Votes, opt => opt.Ignore());

        CreateMap<UpdateBallotDto, Ballot>()
            .ForMember(dest => dest.BallotGuid, opt => opt.Ignore())
            .ForMember(dest => dest.LocationGuid, opt => opt.Ignore())
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.ComputerCode, opt => opt.Ignore())
            .ForMember(dest => dest.BallotNumAtComputer, opt => opt.Ignore())
            .ForMember(dest => dest.BallotCode, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.Location, opt => opt.Ignore())
            .ForMember(dest => dest.Votes, opt => opt.Ignore());
    }
}
