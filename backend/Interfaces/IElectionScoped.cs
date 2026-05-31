using System;

namespace Backend.Interfaces;

public interface IElectionScoped
{
    Guid ElectionGuid { get; set; }
}


