import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { authService, type LoginRequest, type RegisterRequest } from '../services/authService';
import { secureTokenService } from '../services/secureTokenService';
import { useApiErrorHandler } from '../composables/useApiErrorHandler';

export const useAuthStore = defineStore('auth', () => {
  const { handleApiError } = useApiErrorHandler();

  // Initialize from cookies instead of localStorage
  const authData = secureTokenService.getAuthData();
  const token = ref<string | null>(null); // Token is httpOnly, can't read from JS
  const email = ref<string | null>(authData.email);
  const name = ref<string | null>(authData.name);
  const authMethod = ref<string | null>(authData.authMethod);
  const requires2FA = ref(false);
  const pending2FAEmail = ref<string | null>(null);

  const isAuthenticated = computed(() => !!token.value);

  async function register(data: RegisterRequest) {
    try {
      const response = await authService.register(data);

      if (response.requires2FA) {
        requires2FA.value = true;
        pending2FAEmail.value = response.email;
      } else {
        // Tokens are now stored in httpOnly cookies by the backend
        // We can only read user info from non-httpOnly cookies
        token.value = response.token; // Keep for backward compatibility in store
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
        // Tokens are now stored in httpOnly cookies by the backend
        // We can only read user info from non-httpOnly cookies
        token.value = response.token; // Keep for backward compatibility in store
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
    try {
      // Call backend logout endpoint to clear httpOnly cookies
      await authService.logout();
    } catch (error) {
      // Continue with client-side cleanup even if backend call fails
      console.warn('Backend logout failed, clearing client-side data anyway:', error);
    }

    // Clear client-side state
    token.value = null;
    email.value = null;
    name.value = null;
    authMethod.value = null;
    requires2FA.value = false;
    pending2FAEmail.value = null;

    // Clear cookies (readable ones)
    secureTokenService.clearAuthData();
  }

  return {
    token,
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
