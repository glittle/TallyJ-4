using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ4.Domain.Entities;

[Table("TwoFactorToken")]
public class TwoFactorToken
{
    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }
    
    public Guid TokenGuid { get; set; }
    
    [Required]
    public string UserId { get; set; } = null!;
    
    [Required]
    [StringLength(200)]
    public string Secret { get; set; } = null!;
    
    public bool IsEnabled { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime? VerifiedAt { get; set; }
    
    [Column("_RowVersion")]
    [Timestamp]
    public byte[] RowVersion { get; set; } = null!;
}
