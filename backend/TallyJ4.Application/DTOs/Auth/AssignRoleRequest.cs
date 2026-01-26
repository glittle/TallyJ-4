using System.ComponentModel.DataAnnotations;

namespace TallyJ4.Application.DTOs.Auth;

public class AssignRoleRequest
{
    [Required]
    public string RoleName { get; set; } = null!;
}