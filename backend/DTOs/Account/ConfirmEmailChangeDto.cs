namespace Backend.DTOs.Account;

/// <summary>
/// Completes a pending email change using either the link token or the short code.
/// </summary>
public class ConfirmEmailChangeDto
{
    /// <summary>
    /// Opaque token from the confirmation email link.
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// Six-digit code from the confirmation email (used while signed in).
    /// </summary>
    public string? Code { get; set; }
}
