namespace Backend.Services.Auth;

/// <summary>
/// Validates a Google ID token credential and returns the verified email.
/// </summary>
public interface IGoogleIdTokenValidator
{
    /// <summary>
    /// Validates the credential. Returns null when invalid.
    /// </summary>
    Task<GoogleIdTokenValidationResult?> ValidateAsync(string credential, CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of a successful Google ID token validation.
/// </summary>
public class GoogleIdTokenValidationResult
{
    /// <summary>
    /// Verified email address from the token.
    /// </summary>
    public string Email { get; set; } = null!;

    /// <summary>
    /// Whether Google marked the email as verified.
    /// </summary>
    public bool EmailVerified { get; set; }
}