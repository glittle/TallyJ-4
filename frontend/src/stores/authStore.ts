import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { authService, type LoginRequest, type RegisterRequest } from '../services/authService';

export const useAuthStore = defineStore('auth', () => {
  const token = ref<string | null>(localStorage.getItem('auth_token'));
  const email = ref<string | null>(localStorage.getItem('user_email'));
  const requires2FA = ref(false);
  const pending2FAEmail = ref<string | null>(null);

  const isAuthenticated = computed(() => !!token.value);

  async function register(data: RegisterRequest) {
    const response = await authService.register(data);
    
    if (response.requires2FA) {
      requires2FA.value = true;
      pending2FAEmail.value = response.email;
    } else {
      token.value = response.token;
      email.value = response.email;
      localStorage.setItem('auth_token', response.token);
      localStorage.setItem('user_email', response.email);
    }
    
    return response;
  }

  async function login(data: LoginRequest) {
    const response = await authService.login(data);
    
    if (response.requires2FA) {
      requires2FA.value = true;
      pending2FAEmail.value = data.email;
    } else {
      token.value = response.token;
      email.value = response.email;
      localStorage.setItem('auth_token', response.token);
      localStorage.setItem('user_email', response.email);
      requires2FA.value = false;
      pending2FAEmail.value = null;
    }
    
    return response;
  }

  function logout() {
    token.value = null;
    email.value = null;
    requires2FA.value = false;
    pending2FAEmail.value = null;
    localStorage.removeItem('auth_token');
    localStorage.removeItem('user_email');
  }

  return {
    token,
    email,
    requires2FA,
    pending2FAEmail,
    isAuthenticated,
    register,
    login,
    logout
  };
});
