using AutoMapper;
using Backend.DTOs.Elections;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;

namespace Backend.Mappings;

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
            .ForMember(dest => dest.ElectionType, opt => opt.MapFrom(src => ElectionTypeEnum.ParseCode(src.ElectionType)))
            .ForMember(dest => dest.ElectionMode, opt => opt.MapFrom(src => ElectionModeEnum.ParseCode(src.ElectionMode)))
            .ForMember(dest => dest.VoterCount, opt => opt.Ignore())
            .ForMember(dest => dest.BallotCount, opt => opt.Ignore())
            .ForMember(dest => dest.LocationCount, opt => opt.Ignore())
            .ForMember(dest => dest.IsTellerAccessOpen, opt => opt.MapFrom(src => src.ListedForPublicAsOf != null))
            .ForMember(dest => dest.TellerAccessOpenedAt, opt => opt.MapFrom(src => src.ListedForPublicAsOf));

        CreateMap<Election, ElectionSummaryDto>()
            .ForMember(dest => dest.ElectionType, opt => opt.MapFrom(src => ElectionTypeEnum.ParseCode(src.ElectionType)))
            .ForMember(dest => dest.ElectionMode, opt => opt.MapFrom(src => ElectionModeEnum.ParseCode(src.ElectionMode)))
            .ForMember(dest => dest.VoterCount, opt => opt.Ignore())
            .ForMember(dest => dest.BallotCount, opt => opt.Ignore())
            .ForMember(dest => dest.IsTellerAccessOpen, opt => opt.MapFrom(src => src.ListedForPublicAsOf != null))
            .ForMember(dest => dest.IsOnlineVotingEnabled, opt => opt.MapFrom(src => src.OnlineWhenOpen != null && src.OnlineWhenClose != null));

        CreateMap<CreateElectionDto, Election>()
            .ForMember(dest => dest.ElectionType, opt => opt.MapFrom(src => ElectionTypeEnum.ToCodeString(src.ElectionType)))
            .ForMember(dest => dest.ElectionMode, opt => opt.MapFrom(src => ElectionModeEnum.ToCodeString(src.ElectionMode)))
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
            .ForMember(dest => dest.ElectionType, opt => opt.MapFrom(src => ElectionTypeEnum.ToCodeString(src.ElectionType)))
            .ForMember(dest => dest.ElectionMode, opt => opt.MapFrom(src => ElectionModeEnum.ToCodeString(src.ElectionMode)))
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



