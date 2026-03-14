using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs.Auth;

public class KakaoLoginRequest
{
    [Required]
    public string AccessToken { get; set; } = null!;
}
