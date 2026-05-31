using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth;

public class KakaoLoginRequest
{
    [Required]
    public string AccessToken { get; set; } = null!;
}
