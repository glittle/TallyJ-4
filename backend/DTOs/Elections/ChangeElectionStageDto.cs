using System.ComponentModel.DataAnnotations;
using Backend.Enumerations;

namespace Backend.DTOs.Elections;

public class ChangeElectionStageDto
{
    [Required]
    public ElectionStage ElectionStage { get; set; }
}
