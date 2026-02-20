using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs.Auth;

public class GoogleOneTapRequest
{
    [Required]
    public string Credential { get; set; } = null!;
}
