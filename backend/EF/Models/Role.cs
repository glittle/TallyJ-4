using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace TallyJ4.EF.Models;

public partial class Role
{
    public Guid ApplicationId { get; set; }

    [Key]
    public Guid RoleId { get; set; }

    [StringLength(256)]
    public string RoleName { get; set; } = null!;

    [StringLength(256)]
    public string? Description { get; set; }

    [ForeignKey("ApplicationId")]
    [InverseProperty("Roles")]
    public virtual Application Application { get; set; } = null!;

    [ForeignKey("RoleId")]
    [InverseProperty("Roles")]
    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
