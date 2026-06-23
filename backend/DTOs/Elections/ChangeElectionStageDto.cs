using System.ComponentModel.DataAnnotations;
using Backend.Enumerations;

namespace Backend.DTOs.Elections;

public class ChangeElectionStageDto
{
    [Required]
    public ElectionStage ElectionStage { get; set; }

    /// <summary>
    /// When reverting from Finalized, the client must send true after the user confirms the action.
    /// </summary>
    public bool ConfirmLeavingFinalized { get; set; }
}
