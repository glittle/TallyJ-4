using TallyJ4.Domain.Enumerations;

namespace TallyJ4.DTOs.SuperAdmin;

public class SuperAdminElectionFilterDto
{
    public string? Search { get; set; }
    public string? Status { get; set; }
    public ElectionTypeCode? ElectionType { get; set; }
    public string SortBy { get; set; } = "dateOfElection";
    public string SortDirection { get; set; } = "desc";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
