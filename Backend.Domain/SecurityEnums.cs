namespace Backend.Domain;

/// <summary>
/// Enumeration of security event types for audit logging.
/// </summary>
public enum SecurityEventType
{
    // Authentication Events
    LoginSuccess,
    LoginFailure,
    Logout,
    LoginAttemptBlocked, // Rate limiting or account lockout

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

    // Rate Limiting Events
    RateLimitExceeded,

    // Security Configuration Events
    SecuritySettingsChanged,
    EncryptionKeyRotated
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

