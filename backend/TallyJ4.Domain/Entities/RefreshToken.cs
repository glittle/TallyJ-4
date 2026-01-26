using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ4.Domain.Entities;

[Table("RefreshToken")]
public class RefreshToken
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    [Required]
    public string UserId { get; set; } = null!;

    [Required]
    [StringLength(200)]
    public string Token { get; set; } = null!;

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool IsRevoked { get; set; }

    public string? RevokedReason { get; set; }

    public string? ReplacedByToken { get; set; }

    [Column("_RowVersion")]
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}