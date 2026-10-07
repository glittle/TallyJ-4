namespace Backend;

/// <summary>
/// Enumeration of security event types for audit logging.
/// </summary>
public enum SecurityEventType
{
    // Authentication Events
    LoginSuccess,
    LoginFailure,
    Logout,
    LoginAttemptBlocked, // Rate limiting, account lockout, or rejected open register

    // Authorization Events
    AuthorizationFailure,
    AccessDenied,

    // Password Events
    PasswordChange,
    PasswordResetRequested,
    PasswordResetCompleted,
    PasswordResetFailed,

    // 2FA Events
    TwoFactorSetup,
    TwoFactorEnabled,
    TwoFactorDisabled,
    TwoFactorVerificationSuccess,
    TwoFactorVerificationFailure,

    // Account Events
    AccountCreated,
    AccountLocked,
    AccountUnlocked,
    AccountDeleted,
    EmailVerificationSent,
    EmailVerified,
    EmailChangeRequested,
    EmailChanged,

    // OAuth Events
    OAuthLoginInitiated,
    OAuthLoginSuccess,
    OAuthLoginFailure,
    OAuthCallbackValidationFailure,

    // Session Events
    SessionCreated,
    SessionRefreshed,
    SessionExpired,
    SessionRevoked,

    // Suspicious Activity
    SuspiciousLoginPattern, // Multiple failures from same IP
    BruteForceAttemptDetected,
    UnusualLoginLocation,

    // Teller Events
    TellerLoginSuccess,
    TellerLoginFailure,

    // Rate Limiting Events
    RateLimitExceeded,

    // Security Configuration Events
    SecuritySettingsChanged,
    EncryptionKeyRotated,

    /// <summary>
    /// General application / election operational activity (replaces the former Logs table).
    /// </summary>
    OperationalActivity,

    /// <summary>
    /// Shared-passcode guest teller login locked for one election after consecutive failures.
    /// </summary>
    TellerLoginLocked,

    /// <summary>
    /// An owner or admin cleared the guest teller login lockout, or changed the passcode.
    /// </summary>
    TellerLoginUnlocked,

    /// <summary>
    /// A super admin approved an owner for SMS, voice, and WhatsApp login codes.
    /// </summary>
    PaidSendOwnerApproved,

    /// <summary>
    /// A super admin froze login codes for an election or an owner.
    /// </summary>
    PaidSendFrozen,

    /// <summary>
    /// A super admin lifted a login-code freeze.
    /// </summary>
    PaidSendUnfrozen,

    /// <summary>
    /// A super admin raised an election allowance or an owner daily cap.
    /// </summary>
    PaidSendCapRaised,

    /// <summary>
    /// A voter-list upload flagged an election. Online voting is suspended.
    /// </summary>
    ElectionFlagged,

    /// <summary>
    /// A super admin cleared an election flag. Online voting can be used again.
    /// </summary>
    ElectionFlagCleared
}

/// <summary>
/// Severity levels for security events.
/// </summary>
public enum SecurityEventSeverity
{
    Debug,
    Info,
    Warning,
    Error,
    Critical
}

