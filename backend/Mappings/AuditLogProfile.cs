using AutoMapper;
using Backend.Domain.Entities;
using Backend.DTOs.AuditLogs;

namespace Backend.Mappings;

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



