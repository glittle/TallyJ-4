using AutoMapper;
using TallyJ4.Domain.Entities;
using TallyJ4.DTOs.AuditLogs;

namespace TallyJ4.Mappings;

/// <summary>
/// AutoMapper profile for audit log entity and DTO mappings.
/// </summary>
public class AuditLogProfile : Profile
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AuditLogProfile"/> class.
    /// </summary>
    public AuditLogProfile()
    {
        CreateMap<Log, AuditLogDto>();

        CreateMap<CreateAuditLogDto, Log>()
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.AsOf, opt => opt.Ignore());
    }
}
