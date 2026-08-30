using Backend.Middleware;
using Backend.Services;
using Backend.Services.Auth;

/// <summary>
/// DI registration for application, auth, and background services.
/// Extracted from Program.cs top-level local functions — no behavior change.
/// </summary>
public static class ProgramServiceRegistration
{
    public static void RegisterApplicationServices(IServiceCollection services)
    {
        services.AddScoped<IElectionService, ElectionService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IComputerService, ComputerService>();
        services.AddSingleton<IComputerAssignmentService, ComputerAssignmentService>();
        services.AddScoped<ITellerService, TellerService>();
        services.AddScoped<IPeopleService, PeopleService>();
        services.AddScoped<IBallotService, BallotService>();
        services.AddScoped<IVoteService, VoteService>();
        services.AddScoped<IDashboardService, DashboardService>();
        services.AddScoped<ISetupService, SetupService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<IPublicService, PublicService>();
        services.AddScoped<ITallyService, TallyService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IFrontDeskService, FrontDeskService>();
        services.AddScoped<IOnlineVotingService, OnlineVotingService>();
        services.AddScoped<ISuperAdminService, SuperAdminService>();
        services.AddScoped<ImportService>();
        services.AddScoped<IPeopleImportService, PeopleImportService>();
        services.AddScoped<CdnBallotImportService>();
        services.AddScoped<TallyJv3ElectionImportService>();
        services.AddScoped<JsonElectionImportExportService>();
        services.AddScoped<ElectionExportImportService>();
        services.AddSingleton<IRemoteLogService, RemoteLogService>();
    }

    public static void RegisterAuthServices(IServiceCollection services)
    {
        services.AddSingleton<ProductionGoogleIdTokenValidator>();
        services.AddSingleton<IGoogleIdTokenValidator>(sp =>
        {
            var env = sp.GetRequiredService<IWebHostEnvironment>();
            if (env.IsDevelopment() || env.IsEnvironment("Testing"))
            {
                return new DevelopmentGoogleIdTokenValidator(sp.GetRequiredService<ProductionGoogleIdTokenValidator>());
            }

            return sp.GetRequiredService<ProductionGoogleIdTokenValidator>();
        });

        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IPaidVerificationSender, PaidVerificationSender>();
        services.AddScoped<ITwilioSmsStatusService, TwilioSmsStatusService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<EmailService>();
        services.AddScoped<ILocalAuthService, LocalAuthService>();
        services.AddScoped<IPasswordResetService, PasswordResetService>();
        services.AddScoped<ITwoFactorService, TwoFactorService>();
        services.AddScoped<EncryptionService>();

        services.AddScoped<ISecurityAuditService, SecurityAuditService>();
    }

    public static void RegisterBackgroundServices(IServiceCollection services, bool isTesting)
    {
        services.AddSingleton<RateLimitStore>();

        if (isTesting) // don't register the real broadcast service during testing, to avoid interference with tests and allow testing of the broadcast mechanism itself
        {
            services.AddSingleton<IVoteCountBroadcastService, NullVoteCountBroadcastService>();
            return;
        }

        services.AddHostedService<RefreshTokenCleanupService>();
        services.AddSingleton<VoteCountBroadcastService>();
        services.AddSingleton<IVoteCountBroadcastService>(sp => sp.GetRequiredService<VoteCountBroadcastService>());
        services.AddHostedService(sp => sp.GetRequiredService<VoteCountBroadcastService>());
    }
}
