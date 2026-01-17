using System;

namespace TallyJ4.Domain.Interfaces;

public interface IElectionScoped
{
    Guid ElectionGuid { get; set; }
}
