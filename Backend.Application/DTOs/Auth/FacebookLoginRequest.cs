using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs.Auth;

public class FacebookLoginRequest
{
    [Required]
    public string AccessToken { get; set; } = null!;
}
