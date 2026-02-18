// import { postApiAuthRegister, postApiAuthLogin, postApiAuthPasswordForgot, postApiAuthPasswordReset, postApiAuth2FaSetup, postApiAuth2FaEnable, postApiAuth2FaDisable } from '../api/gen/configService/sdk.gen';
import {
  postApiAuthRegisterAccount,
  postApiAuthLogin,
  postApiAuthForgotPassword,
  postApiAuthResetPassword,
  postApiAuthSetup2Fa,
  postApiAuthEnable2Fa,
  postApiAuthDisable2Fa,
} from "../api/gen/configService/sdk.gen";
import type {
  RegisterRequest,
  LoginRequest,
} from "../api/gen/configService/types.gen";

export interface AuthResponse {
  email: string;
  name?: string;
  authMethod?: string;
  requires2FA: boolean;
}

export interface TwoFactorSetupResponse {
  secret: string;
  qrCodeDataUrl: string;
}

export const authService = {
  async register(data: RegisterRequest): Promise<AuthResponse> {
    const response = await postApiAuthRegisterAccount({
      body: data,
      throwOnError: true,
    });
    return response.data as AuthResponse;
  },

  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await postApiAuthLogin({
      body: data,
      throwOnError: true,
    });
    return response.data as AuthResponse;
  },

  async forgotPassword(email: string): Promise<void> {
    await postApiAuthForgotPassword({
      body: { email },
      throwOnError: true,
    });
  },

  async resetPassword(
    email: string,
    token: string,
    newPassword: string,
    confirmPassword: string,
  ): Promise<void> {
    await postApiAuthResetPassword({
      body: {
        email,
        token,
        newPassword,
        confirmPassword,
      },
      throwOnError: true,
    });
  },

  async setup2FA(): Promise<TwoFactorSetupResponse> {
    const response = await postApiAuthSetup2Fa({
      throwOnError: true,
    });
    return response.data as TwoFactorSetupResponse;
  },

  async enable2FA(code: string): Promise<void> {
    await postApiAuthEnable2Fa({
      body: { code },
      throwOnError: true,
    });
  },

  async disable2FA(password: string, code: string): Promise<void> {
    await postApiAuthDisable2Fa({
      body: { password, code },
      throwOnError: true,
    });
  },

  async googleOneTap(credential: string): Promise<AuthResponse> {
    const apiUrl = import.meta.env.VITE_API_URL || 'http://localhost:5016';
    const response = await fetch(`${apiUrl}/api/auth/google/one-tap`, {
      method: 'POST',
      credentials: 'include',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ credential }),
    });

    if (!response.ok) {
      const errorData = await response.json().catch(() => ({ error: 'Google sign-in failed' }));
      throw new Error(errorData.error || 'Google sign-in failed');
    }

    return response.json() as Promise<AuthResponse>;
  },

  async logout(): Promise<void> {
    const response = await fetch('/api/auth/logout', {
      method: 'GET',
      credentials: 'include', // Include cookies for authentication
    });

    if (!response.ok) {
      throw new Error('Logout failed');
    }
  },
};

export {
  type RegisterRequest,
  type LoginRequest,
} from "../api/gen/configService/types.gen";
