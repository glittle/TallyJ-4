/**
 * Automatic token refresh service that keeps the user's session alive.
 * Refreshes JWT tokens at 75% of their lifetime to prevent expiration.
 */

import { postApiAuthRefreshToken } from "../api/gen/configService/sdk.gen";
import { secureTokenService } from "./secureTokenService";

export interface TokenRefreshConfig {
  /** Token expiry time in minutes (default: 60) */
  tokenExpiryMinutes?: number;
  /** When to refresh as a percentage of token lifetime (default: 0.75 = 75%) */
  refreshThreshold?: number;
  /** Whether to enable automatic refresh (default: true) */
  enabled?: boolean;
}

class TokenRefreshService {
  private refreshIntervalId: number | null = null;
  private config: Required<TokenRefreshConfig> = {
    tokenExpiryMinutes: 60,
    refreshThreshold: 0.75,
    enabled: true,
  };
  private refreshPromise: Promise<boolean> | null = null;
  private lastRefreshTime: number | null = null;

  /**
   * Initialize the token refresh service with optional configuration.
   */
  initialize(config?: TokenRefreshConfig): void {
    this.config = {
      ...this.config,
      ...config,
    };

    if (this.config.enabled && secureTokenService.isAuthenticated()) {
      this.startAutoRefresh();
    }
  }

  /**
   * Start automatic token refresh.
   * Refreshes at 75% of token lifetime (45 minutes for 60-minute tokens).
   */
  startAutoRefresh(): void {
    if (this.refreshIntervalId !== null) {
      return; // Already running
    }

    // Calculate refresh interval: tokenExpiry * threshold
    // For 60-minute tokens with 0.75 threshold: 60 * 0.75 = 45 minutes
    const refreshIntervalMs =
      this.config.tokenExpiryMinutes * this.config.refreshThreshold * 60 * 1000;

    console.log(
      `[TokenRefresh] Starting auto-refresh every ${refreshIntervalMs / 60000} minutes`,
    );

    // Refresh immediately if we haven't refreshed recently
    const timeSinceLastRefresh = this.lastRefreshTime
      ? Date.now() - this.lastRefreshTime
      : Infinity;
    if (timeSinceLastRefresh > refreshIntervalMs) {
      this.refreshToken();
    }

    // Set up periodic refresh
    this.refreshIntervalId = window.setInterval(() => {
      this.refreshToken();
    }, refreshIntervalMs);
  }

  /**
   * Stop automatic token refresh.
   */
  stopAutoRefresh(): void {
    if (this.refreshIntervalId !== null) {
      console.log("[TokenRefresh] Stopping auto-refresh");
      clearInterval(this.refreshIntervalId);
      this.refreshIntervalId = null;
    }
  }

  /**
   * Manually refresh the authentication token.
   * If a refresh is already in progress, returns the existing promise.
   * @returns Promise resolving to true if refresh succeeded, false otherwise.
   */
  async refreshToken(): Promise<boolean> {
    // If already refreshing, return the existing promise
    if (this.refreshPromise) {
      console.log("[TokenRefresh] Refresh already in progress, waiting...");
      return this.refreshPromise;
    }

    if (!secureTokenService.isAuthenticated()) {
      console.log("[TokenRefresh] User not authenticated, skipping refresh");
      this.stopAutoRefresh();
      return false;
    }

    // Create a new refresh promise
    this.refreshPromise = this.performRefresh();

    try {
      const result = await this.refreshPromise;
      return result;
    } finally {
      // Clear the promise when done
      this.refreshPromise = null;
    }
  }

  /**
   * Perform the actual token refresh operation.
   * @private
   */
  private async performRefresh(): Promise<boolean> {
    try {
      console.log("[TokenRefresh] Refreshing token...");

      // Call refresh endpoint - token is read from httpOnly cookie by backend
      const response = await postApiAuthRefreshToken({
        body: {}, // Empty body - token comes from cookie
        throwOnError: true,
      });

      if (response.data) {
        this.lastRefreshTime = Date.now();
        console.log("[TokenRefresh] Token refreshed successfully");
        return true;
      }

      console.warn("[TokenRefresh] Token refresh returned no data");
      return false;
    } catch (error) {
      console.error("[TokenRefresh] Token refresh failed:", error);

      // If refresh fails with 401, user is logged out, stop auto-refresh
      if (error && typeof error === "object" && "response" in error) {
        const errorWithResponse = error as { response?: { status?: number } };
        if (errorWithResponse.response?.status === 401) {
          this.stopAutoRefresh();
        }
      }

      return false;
    }
  }

  /**
   * Check if auto-refresh is currently running.
   */
  isRunning(): boolean {
    return this.refreshIntervalId !== null;
  }

  /**
   * Get the current configuration.
   */
  getConfig(): Required<TokenRefreshConfig> {
    return { ...this.config };
  }
}

// Export singleton instance
export const tokenRefreshService = new TokenRefreshService();
