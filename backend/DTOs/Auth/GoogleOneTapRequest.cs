using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Auth;

public class GoogleOneTapRequest
{
    [Required]
    public string Credential { get; set; } = null!;
}
