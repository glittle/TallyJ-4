namespace TallyJ4.DTOs.Account;

/// <summary>
/// Data transfer object for login response containing authentication tokens and user information.
/// </summary>
public class LoginResponseDto
{
    /// <summary>
    /// The JWT access token for authentication.
    /// </summary>
    public string AccessToken { get; set; } = null!;

    /// <summary>
    /// The type of token (defaults to "Bearer").
    /// </summary>
    public string TokenType { get; set; } = "Bearer";

    /// <summary>
    /// The number of seconds until the token expires.
    /// </summary>
    public int ExpiresIn { get; set; }

    /// <summary>
    /// The email address of the authenticated user.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// The username of the authenticated user.
    /// </summary>
    public string? UserName { get; set; }
}
