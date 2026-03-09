using System.ComponentModel.DataAnnotations;

namespace Backend.Application.DTOs.Auth;

/// <summary>
/// Request model for Telegram Login Widget authentication for officers and full tellers.
/// Contains the fields returned by the Telegram Login Widget callback.
/// </summary>
public class TelegramLoginRequest
{
    /// <summary>
    /// The Telegram user's numeric ID.
    /// </summary>
    [Required]
    public long Id { get; set; }

    /// <summary>
    /// The Telegram user's first name.
    /// </summary>
    [Required]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>
    /// The Telegram user's last name (optional).
    /// </summary>
    public string? LastName { get; set; }

    /// <summary>
    /// The Telegram user's username (optional).
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// URL of the Telegram user's profile photo (optional).
    /// </summary>
    public string? PhotoUrl { get; set; }

    /// <summary>
    /// Unix timestamp of when the authorization was made.
    /// </summary>
    [Required]
    public long AuthDate { get; set; }

    /// <summary>
    /// HMAC-SHA256 hash used to verify the authenticity of the data.
    /// </summary>
    [Required]
    public string Hash { get; set; } = string.Empty;
}
