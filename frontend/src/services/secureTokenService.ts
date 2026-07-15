/**
 * Service for managing secure authentication cookies.
 * Provides methods to read authentication data from httpOnly cookies set by the backend,
 * and to set/clear client-readable identity cookies on the SPA origin.
 *
 * Identity cookies (`user_email`, `user_name`, `auth_method`) are intentionally NOT HttpOnly
 * so the SPA can read them for router guards and initial store state. Access/refresh tokens
 * are HttpOnly and are only set by the backend.
 *
 * All client-written cookies always include Secure + SameSite=Strict (production is HTTPS;
 * local HTTPS / modern localhost Secure exceptions are assumed for development).
 */

export interface AuthCookieData {
  token: string | null;
  refreshToken: string | null;
  email: string | null;
  name: string | null;
  authMethod: string | null;
}

export interface UserInfoCookieData {
  email?: string | null;
  name?: string | null;
  authMethod?: string | null;
}

/** Default lifetime for identity cookies (30 days), matching backend user cookie expiry. */
const DEFAULT_MAX_AGE_DAYS = 30;

export const secureTokenService = {
  /**
   * Cookie names used by the backend SecureCookieMiddleware
   */
  cookieNames: {
    accessToken: "auth_token",
    refreshToken: "refresh_token",
    userEmail: "user_email",
    userName: "user_name",
    authMethod: "auth_method",
  } as const,

  /**
   * Gets authentication data from cookies.
   * Note: Only non-httpOnly cookies (user info) can be read by JavaScript.
   * Access tokens are httpOnly and must be sent automatically by the browser.
   */
  getAuthData(): AuthCookieData {
    return {
      token: null, // Cannot read httpOnly cookie from JavaScript
      refreshToken: null, // Cannot read httpOnly cookie from JavaScript
      email: this.getCookie(this.cookieNames.userEmail),
      name: this.getCookie(this.cookieNames.userName),
      authMethod: this.getCookie(this.cookieNames.authMethod),
    };
  },

  /**
   * Checks if the user appears to be authenticated based on available cookie data.
   * Note: This is not foolproof since we can't read the actual tokens.
   */
  isAuthenticated(): boolean {
    const data = this.getAuthData();
    // Regular users have email and authMethod
    if (data.email && data.authMethod) {
      return true;
    }
    // Tellers have name="Teller" and authMethod="AccessCode"
    if (data.name === "Teller" && data.authMethod === "AccessCode") {
      return true;
    }
    return false;
  },

  /**
   * Clears authentication data by expiring cookies.
   * Note: This will trigger the browser to remove readable cookies.
   * HttpOnly cookies are cleared by the backend logout endpoint.
   */
  clearAuthData(): void {
    this.setCookie(this.cookieNames.userEmail, "", -1);
    this.setCookie(this.cookieNames.userName, "", -1);
    this.setCookie(this.cookieNames.authMethod, "", -1);
  },

  /**
   * Sets SPA-origin identity cookies after OAuth /me (or similar) when backend cookies
   * may be on a different host. Only non-empty string fields are written.
   */
  setUserInfoCookies(data: UserInfoCookieData): void {
    if (data.email) {
      this.setCookie(this.cookieNames.userEmail, data.email);
    }
    if (data.name) {
      this.setCookie(this.cookieNames.userName, data.name);
    }
    if (data.authMethod) {
      this.setCookie(this.cookieNames.authMethod, data.authMethod);
    }
  },

  /**
   * Sets or clears the user_name identity cookie (profile display-name updates).
   */
  setUserNameCookie(name: string | null): void {
    if (name) {
      this.setCookie(this.cookieNames.userName, name);
    } else {
      this.setCookie(this.cookieNames.userName, "", -1);
    }
  },

  /**
   * Sets or clears the user_email identity cookie (email-change confirmation).
   */
  setUserEmailCookie(email: string | null): void {
    if (email) {
      this.setCookie(this.cookieNames.userEmail, email);
    } else {
      this.setCookie(this.cookieNames.userEmail, "", -1);
    }
  },

  /**
   * Gets a cookie value by name.
   */
  getCookie(name: string): string | null {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) {
      const raw = parts.pop()?.split(";").shift() || null;
      return raw ? decodeURIComponent(raw) : null;
    }
    return null;
  },

  /**
   * Sets a cookie with Secure + SameSite=Strict always applied.
   * @param maxAgeDays Positive days for lifetime; use 0 or negative to expire immediately.
   */
  setCookie(
    name: string,
    value: string,
    maxAgeDays: number = DEFAULT_MAX_AGE_DAYS,
  ): void {
    const maxAgeSeconds =
      maxAgeDays > 0 ? Math.floor(maxAgeDays * 24 * 60 * 60) : 0;
    const encoded = encodeURIComponent(value);
    // Always Secure + SameSite=Strict so sensitive identity cookies are never sent over cleartext.
    // Local HTTP on non-localhost is unsupported; prefer HTTPS for local development.
    document.cookie = `${name}=${encoded}; path=/; samesite=strict; secure; max-age=${maxAgeSeconds}`;
  },

  /**
   * Refreshes authentication data from cookies after login/callback.
   * This should be called after successful authentication to sync the store.
   */
  refreshAuthData(): AuthCookieData {
    return this.getAuthData();
  },
};
