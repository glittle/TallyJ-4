using System;

namespace Backend.Domain.Interfaces;

public interface IElectionScoped
{
    Guid ElectionGuid { get; set; }
}


