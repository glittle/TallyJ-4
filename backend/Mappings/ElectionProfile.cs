using Backend.DTOs.Elections;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;
using Mapster;

namespace Backend.Mappings;

/// <summary>
/// Mapster profile for election-related mappings.
/// Defines mappings between election entities and DTOs.
/// </summary>
public class ElectionProfile : IRegister
{
    /// <summary>
    /// Initializes a new instance of the ElectionProfile.
    /// Configures mappings between Election entities and various DTOs.
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Election, ElectionDto>()
            .Map(dest => dest.ElectionType, src => ElectionTypeEnum.ParseCode(src.ElectionType))
            .Map(dest => dest.ElectionMode, src => ElectionModeEnum.ParseCode(src.ElectionMode))
            .Map(dest => dest.IsTellerAccessOpen, src => src.ListedForPublicAsOf != null)
            .Map(dest => dest.TellerAccessOpenedAt, src => src.ListedForPublicAsOf);

        config.NewConfig<Election, ElectionSummaryDto>()
            .Map(dest => dest.ElectionType, src => ElectionTypeEnum.ParseCode(src.ElectionType))
            .Map(dest => dest.ElectionMode, src => ElectionModeEnum.ParseCode(src.ElectionMode))
            .Map(dest => dest.IsTellerAccessOpen, src => src.ListedForPublicAsOf != null)
            .Map(dest => dest.IsOnlineVotingEnabled, src => src.OnlineWhenOpen != null && src.OnlineWhenClose != null);

        config.NewConfig<CreateElectionDto, Election>()
            .Map(dest => dest.ElectionType, src => ElectionTypeEnum.ToCodeString(src.ElectionType))
            .Map(dest => dest.ElectionMode, src => ElectionModeEnum.ToCodeString(src.ElectionMode));

        config.NewConfig<UpdateElectionDto, Election>()
            .Map(dest => dest.ElectionType, src => ElectionTypeEnum.ToCodeString(src.ElectionType))
            .Map(dest => dest.ElectionMode, src => ElectionModeEnum.ToCodeString(src.ElectionMode));
    }
}
