using System.Security.Claims;
using Backend.DTOs.Elections;

namespace Backend.Services;

/// <summary>
/// Lists, adds, and removes election co-admins. A co-admin is an existing
/// officer account on <c>JoinElectionUsers</c> with role <c>Admin</c>.
/// </summary>
public interface IElectionAdminService
{
    Task<ElectionAdminsListResult> ListAsync(
        Guid electionGuid,
        ClaimsPrincipal caller,
        CancellationToken cancellationToken = default);

    Task<AddElectionAdminResult> AddAsync(
        Guid electionGuid,
        string email,
        ClaimsPrincipal caller,
        CancellationToken cancellationToken = default);

    Task<RemoveElectionAdminResult> RemoveAsync(
        Guid electionGuid,
        Guid userId,
        ClaimsPrincipal caller,
        CancellationToken cancellationToken = default);
}
