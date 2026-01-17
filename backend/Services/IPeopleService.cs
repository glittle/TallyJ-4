using TallyJ4.DTOs.People;
using TallyJ4.Models;

namespace TallyJ4.Services;

public interface IPeopleService
{
    Task<PaginatedResponse<PersonDto>> GetPeopleByElectionAsync(Guid electionGuid, int pageNumber = 1, int pageSize = 50, string? search = null, bool? canVote = null, bool? canReceiveVotes = null);
    Task<PersonDto?> GetPersonByGuidAsync(Guid personGuid);
    Task<PersonDto> CreatePersonAsync(CreatePersonDto createDto);
    Task<PersonDto?> UpdatePersonAsync(Guid personGuid, UpdatePersonDto updateDto);
    Task<bool> DeletePersonAsync(Guid personGuid);
    Task<List<PersonDto>> SearchPeopleAsync(Guid electionGuid, string query);
}
