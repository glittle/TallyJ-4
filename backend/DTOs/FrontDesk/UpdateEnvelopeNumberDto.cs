using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.FrontDesk;

/// <summary>
/// Data transfer object for setting or clearing a voter's envelope number.
/// </summary>
public class UpdateEnvelopeNumberDto
{
    /// <summary>
    /// The unique identifier of the person.
    /// </summary>
    [Required]
    public Guid PersonGuid { get; set; }

    /// <summary>
    /// The envelope number to assign (whole number greater than zero).
    /// When null, any existing envelope number is cleared.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int? EnvNum { get; set; }
}