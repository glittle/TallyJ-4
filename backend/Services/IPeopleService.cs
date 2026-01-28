using TallyJ4.DTOs.People;
using TallyJ4.Models;

namespace TallyJ4.Services;

/// <summary>
/// Service interface for managing people/voters operations including creation, retrieval, updates, and deletion.
/// Provides functionality to handle people within elections with search and filtering capabilities.
/// </summary>
public interface IPeopleService
{
    /// <summary>
    /// Retrieves people for a specific election with pagination, search, and filtering support.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election.</param>
    /// <param name="pageNumber">The page number to retrieve (1-based).</param>
    /// <param name="pageSize">The number of people per page.</param>
    /// <param name="search">Optional search query to filter people by name or other criteria.</param>
    /// <param name="canVote">Optional filter for people who can vote.</param>
    /// <param name="canReceiveVotes">Optional filter for people who can receive votes.</param>
    /// <returns>A paginated response containing person data.</returns>
    Task<PaginatedResponse<PersonDto>> GetPeopleByElectionAsync(Guid electionGuid, int pageNumber = 1, int pageSize = 50, string? search = null, bool? canVote = null, bool? canReceiveVotes = null);

    /// <summary>
    /// Retrieves a specific person by their unique identifier.
    /// </summary>
    /// <param name="personGuid">The unique identifier of the person.</param>
    /// <returns>The person data, or null if not found.</returns>
    Task<PersonDto?> GetPersonByGuidAsync(Guid personGuid);

    /// <summary>
    /// Creates a new person with the provided data.
    /// </summary>
    /// <param name="createDto">The person creation data.</param>
    /// <returns>The created person data.</returns>
    Task<PersonDto> CreatePersonAsync(CreatePersonDto createDto);

    /// <summary>
    /// Updates an existing person with the provided data.
    /// </summary>
    /// <param name="personGuid">The unique identifier of the person to update.</param>
    /// <param name="updateDto">The updated person data.</param>
    /// <returns>The updated person data, or null if the person was not found.</returns>
    Task<PersonDto?> UpdatePersonAsync(Guid personGuid, UpdatePersonDto updateDto);

    /// <summary>
    /// Deletes a person by their unique identifier.
    /// </summary>
    /// <param name="personGuid">The unique identifier of the person to delete.</param>
    /// <returns>True if the person was successfully deleted, false otherwise.</returns>
    Task<bool> DeletePersonAsync(Guid personGuid);

    /// <summary>
    /// Searches for people within an election based on a query string.
    /// </summary>
    /// <param name="electionGuid">The unique identifier of the election to search within.</param>
    /// <param name="query">The search query to match against person data.</param>
    /// <returns>A list of people matching the search criteria.</returns>
    Task<List<PersonDto>> SearchPeopleAsync(Guid electionGuid, string query);
}
