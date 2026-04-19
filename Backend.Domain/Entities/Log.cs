using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Backend.Domain.Entities;

[Index("AsOf", Name = "IX__Log")]
[Index("ElectionGuid", "LocationGuid", Name = "nci_msft_1__Log_154BF30FBBDD3CC74014282844F74DFE")]
public partial class Log
{
    public Log()
    {
        AsOf = DateTimeOffset.UtcNow;
    }

    [Key]
    [Column("_RowId")]
    public int RowId { get; set; }

    [Precision(0)]
    public DateTimeOffset AsOf { get; set; }

    public Guid? ElectionGuid { get; set; }

    public Guid? LocationGuid { get; set; }

    [StringLength(250)]
    public string? VoterId { get; set; }

    [StringLength(2)]
    [Unicode(false)]
    public string? ComputerCode { get; set; }

    [Unicode(false)]
    public string? Details { get; set; }

    [Unicode(false)]
    public string? HostAndVersion { get; set; }
}


