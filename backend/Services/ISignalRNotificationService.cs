using TallyJ4.DTOs.SignalR;
using TallyJ4.DTOs.Results;

namespace TallyJ4.Services;

public interface ISignalRNotificationService
{
    Task SendElectionUpdateAsync(ElectionUpdateDto update);
    Task SendTallyProgressAsync(TallyProgressDto progress);
    Task SendImportProgressAsync(ImportProgressDto progress);
    Task SendPersonUpdateAsync(PersonUpdateDto update);
    Task SendPublicElectionListUpdateAsync();
    Task SendMonitorUpdateAsync(MonitorInfoDto monitorInfo);
}
