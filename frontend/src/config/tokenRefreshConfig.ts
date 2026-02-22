/**
 * Shared configuration for token refresh across the application.
 */
export const TOKEN_REFRESH_CONFIG = {
  /** Token expiry time in minutes */
  tokenExpiryMinutes: 60,
  /** When to refresh as a percentage of token lifetime (0.75 = 75%) */
  refreshThreshold: 0.75,
  /** Whether to enable automatic refresh */
  enabled: true
} as const;
