using AutoMapper;
using TallyJ4.DTOs.People;
using TallyJ4.Domain.Entities;

namespace TallyJ4.Mappings;

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
            .ForMember(dest => dest.VoteCount, opt => opt.Ignore());

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
            .ForMember(dest => dest.Votes, opt => opt.Ignore());

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
            .ForMember(dest => dest.Votes, opt => opt.Ignore());
    }
}
