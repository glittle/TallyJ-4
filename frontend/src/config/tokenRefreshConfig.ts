/**
 * Shared configuration for token refresh across the application.
 * Keep tokenExpiryMinutes aligned with backend Jwt:ExpiryMinutes (appsettings.json).
 */
export const TOKEN_REFRESH_CONFIG = {
  /** Access-token lifetime in minutes (must match backend Jwt:ExpiryMinutes) */
  tokenExpiryMinutes: 60,
  /** Refresh proactively at 75% of token lifetime (45 min for 60-min tokens) */
  refreshThreshold: 0.75,
  /** Whether to enable automatic refresh for accounts with refresh tokens */
  enabled: true,
} as const;
