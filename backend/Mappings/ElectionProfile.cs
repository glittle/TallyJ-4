using AutoMapper;
using TallyJ4.DTOs.Elections;
using TallyJ4.Domain.Entities;

namespace TallyJ4.Mappings;

/// <summary>
/// AutoMapper profile for election-related mappings.
/// Defines mappings between election entities and DTOs.
/// </summary>
public class ElectionProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the ElectionProfile.
    /// Configures mappings between Election entities and various DTOs.
    /// </summary>
    public ElectionProfile()
    {
        CreateMap<Election, ElectionDto>()
            .ForMember(dest => dest.VoterCount, opt => opt.Ignore())
            .ForMember(dest => dest.BallotCount, opt => opt.Ignore())
            .ForMember(dest => dest.LocationCount, opt => opt.Ignore());

        CreateMap<Election, ElectionSummaryDto>()
            .ForMember(dest => dest.VoterCount, opt => opt.Ignore())
            .ForMember(dest => dest.BallotCount, opt => opt.Ignore());

        CreateMap<CreateElectionDto, Election>()
            .ForMember(dest => dest.ElectionGuid, opt => opt.Ignore())
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.TallyStatus, opt => opt.Ignore())
            .ForMember(dest => dest.LastEnvNum, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerLoginId, opt => opt.Ignore())
            .ForMember(dest => dest.ListedForPublicAsOf, opt => opt.Ignore())
            .ForMember(dest => dest.ImportFiles, opt => opt.Ignore())
            .ForMember(dest => dest.JoinElectionUsers, opt => opt.Ignore())
            .ForMember(dest => dest.Locations, opt => opt.Ignore())
            .ForMember(dest => dest.Messages, opt => opt.Ignore())
            .ForMember(dest => dest.People, opt => opt.Ignore())
            .ForMember(dest => dest.ResultSummaries, opt => opt.Ignore())
            .ForMember(dest => dest.ResultTies, opt => opt.Ignore())
            .ForMember(dest => dest.Results, opt => opt.Ignore())
            .ForMember(dest => dest.Tellers, opt => opt.Ignore());

        CreateMap<UpdateElectionDto, Election>()
            .ForMember(dest => dest.ElectionGuid, opt => opt.Ignore())
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.LastEnvNum, opt => opt.Ignore())
            .ForMember(dest => dest.OwnerLoginId, opt => opt.Ignore())
            .ForMember(dest => dest.ListedForPublicAsOf, opt => opt.Ignore())
            .ForMember(dest => dest.ImportFiles, opt => opt.Ignore())
            .ForMember(dest => dest.JoinElectionUsers, opt => opt.Ignore())
            .ForMember(dest => dest.Locations, opt => opt.Ignore())
            .ForMember(dest => dest.Messages, opt => opt.Ignore())
            .ForMember(dest => dest.People, opt => opt.Ignore())
            .ForMember(dest => dest.ResultSummaries, opt => opt.Ignore())
            .ForMember(dest => dest.ResultTies, opt => opt.Ignore())
            .ForMember(dest => dest.Results, opt => opt.Ignore())
            .ForMember(dest => dest.Tellers, opt => opt.Ignore());
    }
}
