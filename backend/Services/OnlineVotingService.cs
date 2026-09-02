using Backend.Context;
using Backend.Helpers;
using Backend.Services.Auth;
using Microsoft.Extensions.Hosting;

namespace Backend.Services;

/// <summary>
/// Service for managing online voting operations.
/// </summary>
public partial class OnlineVotingService : IOnlineVotingService
{
    private const string NotifyProcessedCode = "P";

    private readonly MainDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IHostEnvironment _hostEnvironment;
    private readonly ILogger<OnlineVotingService> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IEmailSender _emailSender;
    private readonly IPaidVerificationSender _paidVerificationSender;
    private readonly IGoogleIdTokenValidator _googleIdTokenValidator;
    private readonly ISignalRNotificationService _signalRNotificationService;
    private readonly IOnlineBallotAcceptLock _acceptLock;

    /// <summary>
    /// Initializes a new instance of the <see cref="OnlineVotingService"/> class.
    /// </summary>
    /// <param name="context">The database context.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <param name="hostEnvironment">The hosting environment.</param>
    /// <param name="logger">The logger instance.</param>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="emailSender">The email sender service.</param>
    /// <param name="paidVerificationSender">Paid SMS / voice / WhatsApp delivery.</param>
    /// <param name="googleIdTokenValidator">The Google ID token validator.</param>
    /// <param name="signalRNotificationService">Realtime notifications for connected voter sessions.</param>
    /// <param name="acceptLock">Process-wide election-scoped lock for overlapping Accept-all (409).</param>
    public OnlineVotingService(
        MainDbContext context,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<OnlineVotingService> logger,
        IHttpClientFactory httpClientFactory,
        IEmailSender emailSender,
        IPaidVerificationSender paidVerificationSender,
        IGoogleIdTokenValidator googleIdTokenValidator,
        ISignalRNotificationService signalRNotificationService,
        IOnlineBallotAcceptLock acceptLock)
    {
        _context = context;
        _configuration = configuration;
        _hostEnvironment = hostEnvironment;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        _emailSender = emailSender;
        _paidVerificationSender = paidVerificationSender;
        _googleIdTokenValidator = googleIdTokenValidator;
        _signalRNotificationService = signalRNotificationService;
        _acceptLock = acceptLock;
    }
}
