import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { authService, type LoginRequest, type RegisterRequest } from '../services/authService';
import { secureTokenService } from '../services/secureTokenService';
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
        // Tokens are stored in httpOnly cookies by the backend
        // We only read and update user info from non-httpOnly cookies
        email.value = response.email;
        name.value = response.name || null;
        authMethod.value = response.authMethod || 'Local';

        // Refresh cookie data to ensure sync
        const cookieData = secureTokenService.refreshAuthData();
        email.value = cookieData.email || response.email;
        name.value = cookieData.name || response.name;
        authMethod.value = cookieData.authMethod || response.authMethod || 'Local';
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
        // Tokens are stored in httpOnly cookies by the backend
        // We only read and update user info from non-httpOnly cookies
        email.value = response.email;
        name.value = response.name || null;
        authMethod.value = response.authMethod || 'Local';

        // Refresh cookie data to ensure sync
        const cookieData = secureTokenService.refreshAuthData();
        email.value = cookieData.email || response.email;
        name.value = cookieData.name || response.name;
        authMethod.value = cookieData.authMethod || response.authMethod || 'Local';

        requires2FA.value = false;
        pending2FAEmail.value = null;
      }

      return response;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    }
  }

  async function logout() {
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
    logout
  };
});
