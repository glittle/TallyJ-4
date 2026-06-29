using System.ComponentModel.DataAnnotations;

namespace Backend.DTOs.Votes;

/// <summary>
/// Data transfer object for reordering votes on a ballot.
/// Vote row IDs must include every vote on the ballot in the desired order.
/// </summary>
public class ReorderVotesDto
{
    /// <summary>
    /// The unique identifier of the ballot whose votes are being reordered.
    /// </summary>
    [Required]
    public Guid BallotGuid { get; set; }

    /// <summary>
    /// Vote row IDs in the desired order (position 1 through N).
    /// </summary>
    [Required]
    public List<int> VoteRowIds { get; set; } = [];
}