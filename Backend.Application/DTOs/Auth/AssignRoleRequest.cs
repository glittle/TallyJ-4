using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs.Auth;

public class AssignRoleRequest
{
    [Required]
    public string RoleName { get; set; } = null!;
}

