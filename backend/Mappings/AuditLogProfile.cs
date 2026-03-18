using Backend.Domain.Entities;
using Backend.DTOs.AuditLogs;
using Mapster;

namespace Backend.Mappings;

/// <summary>
/// Mapster profile for audit log entity and DTO mappings.
/// </summary>
public class AuditLogProfile : IRegister
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogProfile"/> class.
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Log, AuditLogDto>();

        config.NewConfig<CreateAuditLogDto, Log>();
    }
}
