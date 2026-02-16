using System;

namespace Backend.Domain.Interfaces;

public interface IEntity
{
    int RowId { get; set; }
    Guid Guid { get; }
    byte[] RowVersion { get; set; }
}


