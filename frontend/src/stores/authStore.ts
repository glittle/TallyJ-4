import { defineStore } from "pinia";
import { computed, ref } from "vue";
import { getApiAuthMe } from "../api/gen/configService/sdk.gen";
import { useApiErrorHandler } from "../composables/useApiErrorHandler";
import { TOKEN_REFRESH_CONFIG } from "../config/tokenRefreshConfig";
import {
  authService,
  type LoginRequest,
  type RegisterRequest,
} from "../services/authService";
import { secureTokenService } from "../services/secureTokenService";
import { tokenRefreshService } from "../services/tokenRefreshService";
import type { TelegramLoginRequest } from "../types";
import { clearActiveTellers } from "@/utils/activeTellerStorage";
import {
  getActiveElectionHubGuid,
  setActiveElectionHubGuid,
} from "@/utils/activeElectionHubStorage";
import { signalrService } from "@/services/signalrService";
import { SELECTED_LOCATION_KEY } from "./locationStore";

function clearClientSessionSelections() {
  try {
    localStorage.removeItem(SELECTED_LOCATION_KEY);
    clearActiveTellers();
  } catch (e) {
    console.error("Failed to clear client session selections:", e);
  }
}

export const useAuthStore = defineStore("auth", () => {
  // Initialize from cookies instead of localStorage
  const authData = secureTokenService.getAuthData();
  const email = ref<string | null>(authData.email);
  const name = ref<string | null>(authData.name);
  const authMethod = ref<string | null>(authData.authMethod);
  const isSuperAdmin = ref<boolean>(false);
  /** True after a successful `/api/Auth/me` (or explicit guest default). Not derived from cookies. */
  const userInfoLoaded = ref(false);
  const requires2FA = ref(false);
  const pending2FAEmail = ref<string | null>(null);

  // Check authentication based on cookie presence (not in-memory token)
  const isAuthenticated = computed(() => secureTokenService.isAuthenticated());

  /** In-flight dedupe so concurrent router guards share one /me request. */
  let fetchUserInfoPromise: Promise<{
    email: string;
    name?: string | null;
    authMethod?: string | null;
    isSuperAdmin?: boolean;
  } | null> | null = null;

  async function fetchUserInfo() {
    if (fetchUserInfoPromise) {
      return fetchUserInfoPromise;
    }

    fetchUserInfoPromise = (async () => {
      try {
        const meResponse = await getApiAuthMe();

        if (meResponse.response?.ok && meResponse.data) {
          const userData = meResponse.data as {
            email: string;
            name?: string | null;
            authMethod?: string | null;
            isSuperAdmin?: boolean;
          };
          email.value = userData.email;
          name.value = userData.name || null;
          authMethod.value = userData.authMethod || null;
          // API omits isSuperAdmin for non–super-admins; only explicit true grants the flag.
          isSuperAdmin.value = userData.isSuperAdmin === true;
          userInfoLoaded.value = true;
          return userData;
        }
      } catch (error) {
        console.error("Failed to fetch user info:", error);
      }
      return null;
    })();

    try {
      return await fetchUserInfoPromise;
    } finally {
      fetchUserInfoPromise = null;
    }
  }

  /**
   * Ensures /me has been loaded for non-guest sessions (isSuperAdmin is not in cookies).
   * Safe to call from the router on every navigation; no-ops once loaded.
   */
  async function ensureUserInfoLoaded(): Promise<boolean> {
    if (
      isAuthenticated.value &&
      !userInfoLoaded.value &&
      authMethod.value !== "AccessCode"
    ) {
      await fetchUserInfo();
    }
    return isSuperAdmin.value;
  }

  async function register(data: RegisterRequest) {
    clearClientSessionSelections();
    try {
      const response = await authService.register(data);

      if (response.requiresEmailVerification) {
        // No session until email is verified — do not start token refresh.
        requires2FA.value = false;
        pending2FAEmail.value = null;
        return response;
      }

      if (response.requires2FA) {
        requires2FA.value = true;
        pending2FAEmail.value = response.email;
      } else {
        const cookieData = secureTokenService.refreshAuthData();
        email.value = cookieData.email || response.email;
        name.value = cookieData.name || response.name || null;
        authMethod.value =
          cookieData.authMethod || response.authMethod || "Local";

        await fetchUserInfo();
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
    clearClientSessionSelections();
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
    clearClientSessionSelections();
    try {
      const response = await authService.googleOneTap(credential);

      // For cross-origin deployments, cookies set by the backend may not be readable here
      // Fetch user info from /api/auth/me to ensure we have the latest data
      const userData = await fetchUserInfo();

      if (userData) {
        // Set readable cookies on the SPA origin for router guards
        secureTokenService.setUserInfoCookies({
          email: userData.email,
          name: userData.name,
          authMethod: userData.authMethod,
        });
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

  async function telegramLogin(data: TelegramLoginRequest) {
    clearClientSessionSelections();
    try {
      const response = await authService.telegramLogin(data);

      const userData = await fetchUserInfo();

      if (userData) {
        secureTokenService.setUserInfoCookies({
          email: userData.email,
          name: userData.name,
          authMethod: userData.authMethod,
        });
      } else {
        const cookieData = secureTokenService.refreshAuthData();
        email.value = cookieData.email || response.email;
        name.value = cookieData.name || response.name || null;
        authMethod.value =
          cookieData.authMethod || response.authMethod || "Telegram";
      }

      requires2FA.value = false;
      pending2FAEmail.value = null;

      tokenRefreshService.initialize(TOKEN_REFRESH_CONFIG);

      return response;
    } catch (error) {
      const { handleApiError } = useApiErrorHandler();
      handleApiError(error as any);
      throw error;
    }
  }

  async function facebookLogin(accessToken: string) {
    clearClientSessionSelections();
    try {
      const response = await authService.facebookLogin(accessToken);

      const userData = await fetchUserInfo();

      if (userData) {
        secureTokenService.setUserInfoCookies({
          email: userData.email,
          name: userData.name,
          authMethod: userData.authMethod,
        });
      } else {
        const cookieData = secureTokenService.refreshAuthData();
        email.value = cookieData.email || response.email;
        name.value = cookieData.name || response.name || null;
        authMethod.value =
          cookieData.authMethod || response.authMethod || "Facebook";
      }

      requires2FA.value = false;
      pending2FAEmail.value = null;

      tokenRefreshService.initialize(TOKEN_REFRESH_CONFIG);

      return response;
    } catch (error) {
      const { handleApiError } = useApiErrorHandler();
      handleApiError(error as any);
      throw error;
    }
  }

  async function processOAuthLogin(
    serviceFn: (accessToken: string) => Promise<any>,
    accessToken: string,
    fallbackAuthMethod: string,
  ) {
    clearClientSessionSelections();
    try {
      const response = await serviceFn(accessToken);

      const userData = await fetchUserInfo();

      if (userData) {
        secureTokenService.setUserInfoCookies({
          email: userData.email,
          name: userData.name,
          authMethod: userData.authMethod,
        });
      } else {
        const cookieData = secureTokenService.refreshAuthData();
        email.value = cookieData.email || response.email;
        name.value = cookieData.name || response.name || null;
        authMethod.value =
          cookieData.authMethod || response.authMethod || fallbackAuthMethod;
      }

      requires2FA.value = false;
      pending2FAEmail.value = null;

      tokenRefreshService.initialize(TOKEN_REFRESH_CONFIG);

      return response;
    } catch (error) {
      const { handleApiError } = useApiErrorHandler();
      handleApiError(error as any);
      throw error;
    }
  }

  async function kakaoLogin(accessToken: string) {
    return processOAuthLogin(authService.kakaoLogin, accessToken, "Kakao");
  }

  async function tellerLogin(electionGuid: string, accessCode: string) {
    clearClientSessionSelections();
    try {
      const response = await authService.tellerLogin(electionGuid, accessCode);

      // Update auth store state from cookies
      const cookieData = secureTokenService.refreshAuthData();
      email.value = cookieData.email;
      name.value = cookieData.name;
      authMethod.value = cookieData.authMethod || "AccessCode";

      requires2FA.value = false;
      pending2FAEmail.value = null;

      // GuestTellers skip fetchUserInfo since they don't have user accounts.
      // GuestTellers are identified by name="Teller" and authMethod="AccessCode".
      if (name.value !== "Teller" || authMethod.value !== "AccessCode") {
        // Fetch user info including isSuperAdmin for FullTellers and officers
        await fetchUserInfo();
      } else {
        // GuestTeller session defaults (no user account /me)
        isSuperAdmin.value = false;
        userInfoLoaded.value = true;
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

  function setDisplayName(newName: string | null) {
    name.value = newName;
    secureTokenService.setUserNameCookie(newName);
  }

  function setEmail(newEmail: string | null) {
    email.value = newEmail;
    secureTokenService.setUserEmailCookie(newEmail);
  }

  async function updateDisplayName(displayName: string) {
    try {
      const profile = await authService.updateDisplayName(displayName);
      setDisplayName(profile.displayName ?? displayName.trim());
      return profile;
    } catch (error) {
      const { handleApiError } = useApiErrorHandler();
      handleApiError(error as any);
      throw error;
    }
  }

  async function requestEmailChange(newEmail: string, currentPassword: string) {
    try {
      await authService.requestEmailChange(newEmail, currentPassword);
    } catch (error) {
      const { handleApiError } = useApiErrorHandler();
      handleApiError(error as any);
      throw error;
    }
  }

  async function confirmEmailChange(params: { token?: string; code?: string }) {
    try {
      await authService.confirmEmailChange(params);
      // Refresh session identity after successful change
      await fetchUserInfo();
      if (email.value) {
        setEmail(email.value);
      }
    } catch (error) {
      const { handleApiError } = useApiErrorHandler();
      handleApiError(error as any);
      throw error;
    }
  }

  async function logout(redirectPath = "/") {
    // Stop automatic token refresh
    tokenRefreshService.stopAutoRefresh();

    // Clear client-side state first
    email.value = null;
    name.value = null;
    authMethod.value = null;
    isSuperAdmin.value = false;
    userInfoLoaded.value = false;
    requires2FA.value = false;
    pending2FAEmail.value = null;

    // Clear cookies (readable ones)
    secureTokenService.clearAuthData();

    clearClientSessionSelections();

    const activeElectionHubGuid = getActiveElectionHubGuid();
    if (activeElectionHubGuid) {
      try {
        await signalrService.leaveElection(activeElectionHubGuid);
      } catch {
        // Best-effort hub cleanup before redirect.
      }
      setActiveElectionHubGuid(null);
    }

    try {
      await authService.logout();
    } catch {
      // Even if logout API call fails, we have already cleared client state and cookies
    }

    globalThis.location.href = redirectPath;
  }

  return {
    email,
    name,
    authMethod,
    isSuperAdmin,
    userInfoLoaded,
    requires2FA,
    pending2FAEmail,
    isAuthenticated,
    fetchUserInfo,
    ensureUserInfoLoaded,
    register,
    login,
    googleOneTapLogin,
    telegramLogin,
    facebookLogin,
    kakaoLogin,
    tellerLogin,
    updateDisplayName,
    requestEmailChange,
    confirmEmailChange,
    setEmail,
    logout,
  };
});
