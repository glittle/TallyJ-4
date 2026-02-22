import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { authService, type LoginRequest, type RegisterRequest } from '../services/authService';
import { secureTokenService } from '../services/secureTokenService';
import { tokenRefreshService } from '../services/tokenRefreshService';
import { TOKEN_REFRESH_CONFIG } from '../config/tokenRefreshConfig';
import { useApiErrorHandler } from '../composables/useApiErrorHandler';

export const useAuthStore = defineStore('auth', () => {
  const { handleApiError } = useApiErrorHandler();

  // Initialize from cookies instead of localStorage
  const authData = secureTokenService.getAuthData();
  const email = ref<string | null>(authData.email);
  const name = ref<string | null>(authData.name);
  const authMethod = ref<string | null>(authData.authMethod);
  const requires2FA = ref(false);
  const pending2FAEmail = ref<string | null>(null);

  // Check authentication based on cookie presence (not in-memory token)
  const isAuthenticated = computed(() => secureTokenService.isAuthenticated());

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
        authMethod.value = cookieData.authMethod || response.authMethod || 'Local';
        
        // Start automatic token refresh
        tokenRefreshService.initialize(TOKEN_REFRESH_CONFIG);
      }

      return response;
    } catch (error) {
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
        authMethod.value = cookieData.authMethod || response.authMethod || 'Local';

        requires2FA.value = false;
        pending2FAEmail.value = null;
        
        // Start automatic token refresh
        tokenRefreshService.initialize(TOKEN_REFRESH_CONFIG);
      }

      return response;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    }
  }

  async function googleOneTapLogin(credential: string) {
    try {
      const response = await authService.googleOneTap(credential);

      // For cross-origin deployments, cookies set by the backend may not be readable here
      // Fetch user info from /api/auth/me to ensure we have the latest data
      const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5016';
      const meResponse = await fetch(`${apiUrl}/api/auth/me`, {
        method: 'GET',
        credentials: 'include',
      });

      if (meResponse.ok) {
        const userData = await meResponse.json();
        
        // Set readable cookies on the SPA origin for router guards
        const secure = window.location.protocol === 'https:' ? '; secure' : '';
        document.cookie = `user_email=${encodeURIComponent(userData.email)}; path=/; samesite=strict${secure}; max-age=2592000`;
        if (userData.name) {
          document.cookie = `user_name=${encodeURIComponent(userData.name)}; path=/; samesite=strict${secure}; max-age=2592000`;
        }
        document.cookie = `auth_method=${encodeURIComponent(userData.authMethod)}; path=/; samesite=strict${secure}; max-age=2592000`;

        // Update store state
        email.value = userData.email;
        name.value = userData.name || null;
        authMethod.value = userData.authMethod || 'Google';
      } else {
        // Fallback to response data and refresh cookies
        const cookieData = secureTokenService.refreshAuthData();
        email.value = cookieData.email || response.email;
        name.value = cookieData.name || response.name || null;
        authMethod.value = cookieData.authMethod || response.authMethod || 'Google';
      }

      requires2FA.value = false;
      pending2FAEmail.value = null;
      
      // Start automatic token refresh
      tokenRefreshService.initialize(TOKEN_REFRESH_CONFIG);

      return response;
    } catch (error) {
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
    requires2FA.value = false;
    pending2FAEmail.value = null;

    // Clear cookies (readable ones)
    secureTokenService.clearAuthData();

    // Navigate to logout endpoint which will clear server-side cookies and redirect to login page
    // This ensures proper server-side logout and page refresh
    const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5016';
    window.location.href = `${apiUrl}/api/auth/logout`;
  }

  return {
    email,
    name,
    authMethod,
    requires2FA,
    pending2FAEmail,
    isAuthenticated,
    register,
    login,
    googleOneTapLogin,
    logout
  };
});
