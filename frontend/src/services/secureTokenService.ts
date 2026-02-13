/**
 * Service for managing secure authentication cookies.
 * Provides methods to read authentication data from httpOnly cookies set by the backend.
 */
export interface AuthCookieData {
  token: string | null;
  refreshToken: string | null;
  email: string | null;
  name: string | null;
  authMethod: string | null;
}

export const secureTokenService = {
  /**
   * Cookie names used by the backend SecureCookieMiddleware
   */
  cookieNames: {
    accessToken: 'auth_token',
    refreshToken: 'refresh_token',
    userEmail: 'user_email',
    userName: 'user_name',
    authMethod: 'auth_method'
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
      authMethod: this.getCookie(this.cookieNames.authMethod)
    };
  },

  /**
   * Checks if the user appears to be authenticated based on available cookie data.
   * Note: This is not foolproof since we can't read the actual tokens.
   */
  isAuthenticated(): boolean {
    const data = this.getAuthData();
    return !!(data.email && data.authMethod);
  },

  /**
   * Clears authentication data by expiring cookies.
   * Note: This will trigger the browser to remove readable cookies.
   * HttpOnly cookies are cleared by the backend logout endpoint.
   */
  clearAuthData(): void {
    // Clear readable cookies by setting them to expire
    this.setCookie(this.cookieNames.userEmail, '', -1);
    this.setCookie(this.cookieNames.userName, '', -1);
    this.setCookie(this.cookieNames.authMethod, '', -1);
  },

  /**
   * Gets a cookie value by name.
   */
  getCookie(name: string): string | null {
    const value = `; ${document.cookie}`;
    const parts = value.split(`; ${name}=`);
    if (parts.length === 2) {
      const raw = parts.pop()?.split(';').shift() || null;
      return raw ? decodeURIComponent(raw) : null;
    }
    return null;
  },

  /**
   * Sets a cookie with the given name, value, and max age in days.
   */
  setCookie(name: string, value: string, maxAgeDays: number = 30): void {
    const maxAge = maxAgeDays > 0 ? `; max-age=${maxAgeDays * 24 * 60 * 60}` : '; max-age=0';
    const secure = window.location.protocol === 'https:' ? '; secure' : '';
    document.cookie = `${name}=${value}; path=/; samesite=strict${secure}${maxAge}`;
  },

  /**
   * Refreshes authentication data from cookies after login/callback.
   * This should be called after successful authentication to sync the store.
   */
  refreshAuthData(): AuthCookieData {
    return this.getAuthData();
  }
};