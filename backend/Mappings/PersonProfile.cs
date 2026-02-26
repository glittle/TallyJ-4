using AutoMapper;
using Backend.DTOs.People;
using Backend.Domain.Entities;
using Backend.Domain.Enumerations;

namespace Backend.Mappings;

/// <summary>
/// AutoMapper profile for person-related mappings.
/// Defines mappings between person entities and DTOs.
/// </summary>
public class PersonProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the PersonProfile.
    /// Configures mappings between Person entities and various DTOs.
    /// </summary>
    public PersonProfile()
    {
        CreateMap<Person, PersonDto>()
            .ForMember(dest => dest.VoteCount, opt => opt.Ignore())
            .ForMember(dest => dest.IneligibleReasonCode, opt => opt.MapFrom(src => GetIneligibleReasonCode(src.IneligibleReasonGuid)));

        CreateMap<Person, PersonListDto>()
            .ForMember(dest => dest.IneligibleReasonCode, opt => opt.MapFrom(src => GetIneligibleReasonCode(src.IneligibleReasonGuid)));

        CreateMap<Person, PersonDetailDto>()
            .ForMember(dest => dest.VoteCount, opt => opt.Ignore())
            .ForMember(dest => dest.VoteHistory, opt => opt.Ignore())
            .ForMember(dest => dest.IneligibleReasonCode, opt => opt.MapFrom(src => GetIneligibleReasonCode(src.IneligibleReasonGuid)));

        CreateMap<Vote, VoteHistoryDto>()
            .ForMember(dest => dest.PersonName, opt => opt.MapFrom(src => src.Person != null ? src.Person.FullName : null))
            .ForMember(dest => dest.BallotNumber, opt => opt.MapFrom(src => src.Ballot != null ? src.Ballot.BallotNumAtComputer : (int?)null))
            .ForMember(dest => dest.BallotStatusCode, opt => opt.MapFrom(src => src.Ballot != null ? src.Ballot.StatusCode : null));

        CreateMap<CreatePersonDto, Person>()
            .ForMember(dest => dest.PersonGuid, opt => opt.Ignore())
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.FullName, opt => opt.Ignore())
            .ForMember(dest => dest.FullNameFl, opt => opt.Ignore())
            .ForMember(dest => dest.CombinedInfo, opt => opt.Ignore())
            .ForMember(dest => dest.CombinedSoundCodes, opt => opt.Ignore())
            .ForMember(dest => dest.CombinedInfoAtStart, opt => opt.Ignore())
            .ForMember(dest => dest.RegistrationTime, opt => opt.Ignore())
            .ForMember(dest => dest.VotingLocationGuid, opt => opt.Ignore())
            .ForMember(dest => dest.VotingMethod, opt => opt.Ignore())
            .ForMember(dest => dest.EnvNum, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersionInt, opt => opt.Ignore())
            .ForMember(dest => dest.Teller1, opt => opt.Ignore())
            .ForMember(dest => dest.Teller2, opt => opt.Ignore())
            .ForMember(dest => dest.HasOnlineBallot, opt => opt.Ignore())
            .ForMember(dest => dest.Flags, opt => opt.Ignore())
            .ForMember(dest => dest.UnitName, opt => opt.Ignore())
            .ForMember(dest => dest.KioskCode, opt => opt.Ignore())
            .ForMember(dest => dest.Election, opt => opt.Ignore())
            .ForMember(dest => dest.Results, opt => opt.Ignore())
            .ForMember(dest => dest.Votes, opt => opt.Ignore())
            .ForMember(dest => dest.CanVote, opt => opt.Ignore())
            .ForMember(dest => dest.CanReceiveVotes, opt => opt.Ignore());

        CreateMap<UpdatePersonDto, Person>()
            .ForMember(dest => dest.PersonGuid, opt => opt.Ignore())
            .ForMember(dest => dest.ElectionGuid, opt => opt.Ignore())
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersion, opt => opt.Ignore())
            .ForMember(dest => dest.FullName, opt => opt.Ignore())
            .ForMember(dest => dest.FullNameFl, opt => opt.Ignore())
            .ForMember(dest => dest.CombinedInfo, opt => opt.Ignore())
            .ForMember(dest => dest.CombinedSoundCodes, opt => opt.Ignore())
            .ForMember(dest => dest.CombinedInfoAtStart, opt => opt.Ignore())
            .ForMember(dest => dest.RegistrationTime, opt => opt.Ignore())
            .ForMember(dest => dest.VotingLocationGuid, opt => opt.Ignore())
            .ForMember(dest => dest.VotingMethod, opt => opt.Ignore())
            .ForMember(dest => dest.EnvNum, opt => opt.Ignore())
            .ForMember(dest => dest.RowVersionInt, opt => opt.Ignore())
            .ForMember(dest => dest.Teller1, opt => opt.Ignore())
            .ForMember(dest => dest.Teller2, opt => opt.Ignore())
            .ForMember(dest => dest.HasOnlineBallot, opt => opt.Ignore())
            .ForMember(dest => dest.Flags, opt => opt.Ignore())
            .ForMember(dest => dest.UnitName, opt => opt.Ignore())
            .ForMember(dest => dest.KioskCode, opt => opt.Ignore())
            .ForMember(dest => dest.Election, opt => opt.Ignore())
            .ForMember(dest => dest.Results, opt => opt.Ignore())
            .ForMember(dest => dest.Votes, opt => opt.Ignore())
            .ForMember(dest => dest.CanVote, opt => opt.Ignore())
            .ForMember(dest => dest.CanReceiveVotes, opt => opt.Ignore());
    }

    private static string? GetIneligibleReasonCode(Guid? guid)
    {
        return guid.HasValue ? IneligibleReasonEnum.GetByGuid(guid.Value)?.Code : null;
    }
}



