namespace TallyJ4.DTOs.Account;

public class LoginResponseDto
{
    public string AccessToken { get; set; } = null!;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public string Email { get; set; } = null!;
    public string? UserName { get; set; }
}
