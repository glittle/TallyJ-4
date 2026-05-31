using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth;

public class TellerLoginRequest
{
    [Required]
    public Guid ElectionGuid { get; set; }

    [Required]
    public string AccessCode { get; set; } = null!;
}
