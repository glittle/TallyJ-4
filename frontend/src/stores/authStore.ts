import { defineStore } from "pinia";
import { ref, computed } from "vue";
import {
  authService,
  type LoginRequest,
  type RegisterRequest,
} from "../services/authService";
import { secureTokenService } from "../services/secureTokenService";
import { tokenRefreshService } from "../services/tokenRefreshService";
import { TOKEN_REFRESH_CONFIG } from "../config/tokenRefreshConfig";
import { useApiErrorHandler } from "../composables/useApiErrorHandler";
import { SELECTED_LOCATION_KEY } from "./locationStore";

export const useAuthStore = defineStore("auth", () => {
  // Initialize from cookies instead of localStorage
  const authData = secureTokenService.getAuthData();
  const email = ref<string | null>(authData.email);
  const name = ref<string | null>(authData.name);
  const authMethod = ref<string | null>(authData.authMethod);
  const isSuperAdmin = ref<boolean>(false);
  const requires2FA = ref(false);
  const pending2FAEmail = ref<string | null>(null);

  // Check authentication based on cookie presence (not in-memory token)
  const isAuthenticated = computed(() => secureTokenService.isAuthenticated());

  // API base URL constant
  const API_URL = import.meta.env.VITE_API_URL || "http://localhost:5016";

  async function fetchUserInfo() {
    try {
      const meResponse = await fetch(`${API_URL}/api/auth/me`, {
        method: "GET",
        credentials: "include",
      });

      if (meResponse.ok) {
        const userData = await meResponse.json();
        email.value = userData.email;
        name.value = userData.name || null;
        authMethod.value = userData.authMethod || null;
        isSuperAdmin.value = userData.isSuperAdmin || false;
        return userData;
      }
    } catch (error) {
      console.error("Failed to fetch user info:", error);
    }
    return null;
  }

  async function register(data: RegisterRequest) {
    try {
      const response = await authService.register(data);

      if (response.requires2FA) {
        requires2FA.value = true;
        pending2FAEmail.value = response.email;
      } else {
        const cookieData = secureTokenService.refreshAuthData();
        email.value = cookieData.email || response.email;
        name.value = cookieData.name || response.name || null;
        authMethod.value =
          cookieData.authMethod || response.authMethod || "Local";

        // Fetch user info including isSuperAdmin
        await fetchUserInfo();

        // Start automatic token refresh
        tokenRefreshService.initialize(TOKEN_REFRESH_CONFIG);
      }

      return response;
    } catch (error) {
      const { handleApiError } = useApiErrorHandler();
      handleApiError(error as any);
      throw error;
    }
  }

  async function login(data: LoginRequest) {
    try {
      const response = await authService.login(data);

      if (response.requires2FA) {
        requires2FA.value = true;
        pending2FAEmail.value = data.email;
      } else {
        const cookieData = secureTokenService.refreshAuthData();
        email.value = cookieData.email || response.email;
        name.value = cookieData.name || response.name || null;
        authMethod.value =
          cookieData.authMethod || response.authMethod || "Local";

        requires2FA.value = false;
        pending2FAEmail.value = null;

        // Fetch user info including isSuperAdmin
        await fetchUserInfo();

        // Start automatic token refresh
        tokenRefreshService.initialize(TOKEN_REFRESH_CONFIG);
      }

      return response;
    } catch (error) {
      const { handleApiError } = useApiErrorHandler();
      handleApiError(error as any);
      throw error;
    }
  }

  async function googleOneTapLogin(credential: string) {
    try {
      const response = await authService.googleOneTap(credential);

      // For cross-origin deployments, cookies set by the backend may not be readable here
      // Fetch user info from /api/auth/me to ensure we have the latest data
      const userData = await fetchUserInfo();

      if (userData) {
        // Set readable cookies on the SPA origin for router guards
        const secure = window.location.protocol === "https:" ? "; secure" : "";
        document.cookie = `user_email=${encodeURIComponent(userData.email)}; path=/; samesite=strict${secure}; max-age=2592000`;
        if (userData.name) {
          document.cookie = `user_name=${encodeURIComponent(userData.name)}; path=/; samesite=strict${secure}; max-age=2592000`;
        }
        document.cookie = `auth_method=${encodeURIComponent(userData.authMethod)}; path=/; samesite=strict${secure}; max-age=2592000`;
      } else {
        // Fallback to response data and refresh cookies
        const cookieData = secureTokenService.refreshAuthData();
        email.value = cookieData.email || response.email;
        name.value = cookieData.name || response.name || null;
        authMethod.value =
          cookieData.authMethod || response.authMethod || "Google";
      }

      requires2FA.value = false;
      pending2FAEmail.value = null;

      // Start automatic token refresh
      tokenRefreshService.initialize(TOKEN_REFRESH_CONFIG);

      return response;
    } catch (error) {
      const { handleApiError } = useApiErrorHandler();
      handleApiError(error as any);
      throw error;
    }
  }

  async function tellerLogin(electionGuid: string, accessCode: string) {
    try {
      const response = await authService.tellerLogin(electionGuid, accessCode);

      // Update auth store state from cookies
      const cookieData = secureTokenService.refreshAuthData();
      email.value = cookieData.email;
      name.value = cookieData.name;
      authMethod.value = cookieData.authMethod || "AccessCode";

      requires2FA.value = false;
      pending2FAEmail.value = null;

      // For tellers, skip fetchUserInfo since they don't have user accounts
      // Tellers are identified by name="Teller" and authMethod="AccessCode"
      if (name.value !== "Teller" || authMethod.value !== "AccessCode") {
        // Fetch user info including isSuperAdmin for regular users
        await fetchUserInfo();
      } else {
        // For tellers, set default values
        isSuperAdmin.value = false;
      }

      // Start automatic token refresh
      tokenRefreshService.initialize(TOKEN_REFRESH_CONFIG);

      return response;
    } catch (error) {
      const { handleApiError } = useApiErrorHandler();
      handleApiError(error as any);
      throw error;
    }
  }

  async function logout() {
    // Stop automatic token refresh
    tokenRefreshService.stopAutoRefresh();

    // Clear client-side state first
    email.value = null;
    name.value = null;
    authMethod.value = null;
    isSuperAdmin.value = false;
    requires2FA.value = false;
    pending2FAEmail.value = null;

    // Clear cookies (readable ones)
    secureTokenService.clearAuthData();

    // Clear selected location from localStorage
    try {
      localStorage.removeItem(SELECTED_LOCATION_KEY);
    } catch (e) {
      console.error("Failed to clear selected location on logout:", e);
    }

    // Navigate to logout endpoint which will clear server-side cookies and redirect to login page
    // This ensures proper server-side logout and page refresh
    window.location.href = `${API_URL}/api/auth/logout`;
  }

  return {
    email,
    name,
    authMethod,
    isSuperAdmin,
    requires2FA,
    pending2FAEmail,
    isAuthenticated,
    fetchUserInfo,
    register,
    login,
    googleOneTapLogin,
    tellerLogin,
    logout,
  };
});
