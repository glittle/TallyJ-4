using Backend.Domain.Entities;
using Backend.DTOs.Security;
using Mapster;
using System.Text.Json;

namespace Backend.Mappings;

/// <summary>
/// Mapster profile for security audit log entity and DTO mappings.
/// </summary>
public class SecurityAuditLogProfile : IRegister
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityAuditLogProfile"/> class.
    /// </summary>
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<SecurityAuditLog, SecurityAuditLogDto>()
            .Map(dest => dest.Metadata, src =>
                !string.IsNullOrEmpty(src.MetadataJson)
                    ? JsonSerializer.Deserialize<Dictionary<string, string>>(src.MetadataJson)
                    : null);

        config.NewConfig<CreateSecurityAuditLogDto, SecurityAuditLog>()
            .Map(dest => dest.MetadataJson, src =>
                src.Metadata != null ? JsonSerializer.Serialize(src.Metadata) : null);
    }
}
