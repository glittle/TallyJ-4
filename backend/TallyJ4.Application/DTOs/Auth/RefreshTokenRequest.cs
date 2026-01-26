using System.ComponentModel.DataAnnotations;

namespace TallyJ4.Application.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required]
    public string RefreshToken { get; set; } = null!;
}