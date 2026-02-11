using AutoMapper;
using TallyJ4.Domain.Entities;
using TallyJ4.DTOs.Security;

namespace TallyJ4.Mappings;

/// <summary>
/// AutoMapper profile for security audit log entity and DTO mappings.
/// </summary>
public class SecurityAuditLogProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SecurityAuditLogProfile"/> class.
    /// </summary>
    public SecurityAuditLogProfile()
    {
        CreateMap<SecurityAuditLog, SecurityAuditLogDto>()
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src =>
                !string.IsNullOrEmpty(src.MetadataJson)
                    ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(src.MetadataJson)
                    : null));

        CreateMap<CreateSecurityAuditLogDto, SecurityAuditLog>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Timestamp, opt => opt.Ignore())
            .ForMember(dest => dest.MetadataJson, opt => opt.MapFrom(src =>
                src.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(src.Metadata) : null));
    }
}