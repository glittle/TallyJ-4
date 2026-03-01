using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs.Auth;

public class TellerLoginRequest
{
    [Required]
    public Guid ElectionGuid { get; set; }

    [Required]
    public string AccessCode { get; set; } = null!;
}
