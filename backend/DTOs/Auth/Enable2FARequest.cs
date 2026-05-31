using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth;

public class Enable2FARequest
{
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = null!;
}


