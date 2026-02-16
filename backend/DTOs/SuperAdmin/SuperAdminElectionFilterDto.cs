using Backend.Domain.Enumerations;

namespace Backend.DTOs.SuperAdmin;

/// <summary>
/// Data transfer object for filtering and paginating elections in the super admin dashboard.
/// </summary>
public class SuperAdminElectionFilterDto
{
    /// <summary>
    /// Search term to filter elections by name or convenor.
    /// </summary>
    public string? Search { get; set; }

    /// <summary>
    /// Filter elections by their tally status.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Filter elections by their type.
    /// </summary>
    public ElectionTypeCode? ElectionType { get; set; }

    /// <summary>
    /// Field to sort elections by. Defaults to "dateOfElection".
    /// </summary>
    public string SortBy { get; set; } = "dateOfElection";

    /// <summary>
    /// Sort direction: "asc" or "desc". Defaults to "desc".
    /// </summary>
    public string SortDirection { get; set; } = "desc";

    /// <summary>
    /// Page number for pagination. Defaults to 1.
    /// </summary>
    public int Page { get; set; } = 1;

    /// <summary>
    /// Number of items per page. Defaults to 25.
    /// </summary>
    public int PageSize { get; set; } = 25;
}



