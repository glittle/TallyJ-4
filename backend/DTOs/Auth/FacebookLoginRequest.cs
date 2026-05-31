using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth;

public class FacebookLoginRequest
{
    [Required]
    public string AccessToken { get; set; } = null!;
}
