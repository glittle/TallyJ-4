using AutoMapper;
using TallyJ4.Domain.Entities;
using TallyJ4.DTOs.AuditLogs;

namespace TallyJ4.Mappings;

public class AuditLogProfile : Profile
{
    public AuditLogProfile()
    {
        CreateMap<Log, AuditLogDto>();
        
        CreateMap<CreateAuditLogDto, Log>()
            .ForMember(dest => dest.RowId, opt => opt.Ignore())
            .ForMember(dest => dest.AsOf, opt => opt.Ignore());
    }
}
