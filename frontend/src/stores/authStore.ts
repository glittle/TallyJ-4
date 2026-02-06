import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { authService, type LoginRequest, type RegisterRequest } from '../services/authService';
import { useApiErrorHandler } from '../composables/useApiErrorHandler';

export const useAuthStore = defineStore('auth', () => {
  const { handleApiError } = useApiErrorHandler();

  const token = ref<string | null>(localStorage.getItem('auth_token'));
  const email = ref<string | null>(localStorage.getItem('user_email'));
  const name = ref<string | null>(localStorage.getItem('user_name'));
  const authMethod = ref<string | null>(localStorage.getItem('auth_method'));
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
        token.value = response.token;
        email.value = response.email;
        name.value = response.name || null;
        authMethod.value = response.authMethod || 'Local';
        localStorage.setItem('auth_token', response.token);
        localStorage.setItem('user_email', response.email);
        if (response.name) {
          localStorage.setItem('user_name', response.name);
        }
        localStorage.setItem('auth_method', response.authMethod || 'Local');
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
        token.value = response.token;
        email.value = response.email;
        name.value = response.name || null;
        authMethod.value = response.authMethod || 'Local';
        localStorage.setItem('auth_token', response.token);
        localStorage.setItem('user_email', response.email);
        if (response.name) {
          localStorage.setItem('user_name', response.name);
        }
        localStorage.setItem('auth_method', response.authMethod || 'Local');
        requires2FA.value = false;
        pending2FAEmail.value = null;
      }

      return response;
    } catch (error) {
      handleApiError(error as any);
      throw error;
    }
  }

  function logout() {
    token.value = null;
    email.value = null;
    name.value = null;
    authMethod.value = null;
    requires2FA.value = false;
    pending2FAEmail.value = null;
    localStorage.removeItem('auth_token');
    localStorage.removeItem('user_email');
    localStorage.removeItem('user_name');
    localStorage.removeItem('auth_method');
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
