using System.ComponentModel.DataAnnotations;

namespace TallyJ4.Application.DTOs.Auth;

public class Verify2FARequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = null!;
    
    [Required]
    [StringLength(6, MinimumLength = 6)]
    public string Code { get; set; } = null!;
}
