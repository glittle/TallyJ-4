import api from './api';

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
}

export interface LoginRequest {
  email: string;
  password: string;
  twoFactorCode?: string;
}

export interface AuthResponse {
  token: string;
  email: string;
  requires2FA: boolean;
}

export interface TwoFactorSetupResponse {
  secret: string;
  qrCodeDataUrl: string;
}

export const authService = {
  async register(data: RegisterRequest): Promise<AuthResponse> {
    const response = await api.post('/auth/register', data);
    return response.data;
  },

  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await api.post('/auth/login', data);
    return response.data;
  },

  async forgotPassword(email: string): Promise<void> {
    await api.post('/auth/password/forgot', { email });
  },

  async resetPassword(email: string, token: string, newPassword: string, confirmPassword: string): Promise<void> {
    await api.post('/auth/password/reset', {
      email,
      token,
      newPassword,
      confirmPassword
    });
  },

  async setup2FA(): Promise<TwoFactorSetupResponse> {
    const response = await api.post('/auth/2fa/setup');
    return response.data;
  },

  async enable2FA(code: string): Promise<void> {
    await api.post('/auth/2fa/enable', { code });
  },

  async disable2FA(password: string, code: string): Promise<void> {
    await api.post('/auth/2fa/disable', { password, code });
  }
};
