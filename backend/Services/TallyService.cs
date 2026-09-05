using Backend.Context;
using Microsoft.Extensions.Localization;

namespace Backend.Services;

/// <summary>
/// Service for managing election tally calculations, results, and reporting.
/// Provides functionality to calculate election results, generate reports, and manage tie-breaking processes.
/// </summary>
public partial class TallyService : ITallyService
{
    private readonly MainDbContext _context;
    private readonly ILogger<TallyService> _logger;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly IComputerAssignmentService _computerAssignmentService;
    private readonly IStringLocalizer<TallyService> _localizer;

    private const string UnknownFallbackValue = "Unknown";
    private const string UnknownElectionName = "Unknown Election";
    private const string UnknownLocationName = "Unknown Location";

    private string FormatLocationName(string? storedName, string? locationTypeCode)
    {
        if (Backend.Helpers.LocationDisplayHelper.IsOnlineLocationType(locationTypeCode))
        {
            return _localizer[Backend.Helpers.LocationDisplayHelper.TypeOnlineKey];
        }

        return string.IsNullOrWhiteSpace(storedName) ? UnknownLocationName : storedName.Trim();
    }

    private string FormatLocationName(Backend.Entities.Location location) =>
        FormatLocationName(location.Name, location.LocationTypeCode);

    // Section constants - localized
    private string SectionElected => _localizer["tally.section.elected"];
    private string SectionExtra => _localizer["tally.section.extra"];
    private string SectionOther => _localizer["tally.section.other"];

    /// <summary>
    /// Initializes a new instance of the TallyService.
    /// </summary>
    /// <param name="context">The main database context for accessing election and tally data.</param>
    /// <param name="logger">Logger for recording tally service operations.</param>
    /// <param name="signalRNotificationService">Service for sending real-time notifications about tally progress.</param>
    /// <param name="localizer">Localizer for retrieving localized strings.</param>
    public TallyService(
        MainDbContext context,
        ILogger<TallyService> logger,
        ISignalRNotificationService signalRNotificationService,
        IComputerAssignmentService computerAssignmentService,
        IStringLocalizer<TallyService> localizer)
    {
        _context = context;
        _logger = logger;
        _signalRNotificationService = signalRNotificationService;
        _computerAssignmentService = computerAssignmentService;
        _localizer = localizer;
    }
}