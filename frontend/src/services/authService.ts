// import { postApiAuthRegister, postApiAuthLogin, postApiAuthPasswordForgot, postApiAuthPasswordReset, postApiAuth2FaSetup, postApiAuth2FaEnable, postApiAuth2FaDisable } from '../api/gen/configService/sdk.gen';
import {
  postApiAuthRegisterAccount,
  postApiAuthLogin,
  postApiAuthForgotPassword,
  postApiAuthResetPassword,
  postApiAuthSetup2Fa,
  postApiAuthEnable2Fa,
  postApiAuthDisable2Fa,
  postApiAuthGoogleOneTap,
} from "../api/gen/configService/sdk.gen";
import type {
  RegisterRequest,
  LoginRequest,
  GoogleOneTapRequest,
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
    const response = await postApiAuthGoogleOneTap({
      body: { credential } as GoogleOneTapRequest,
      throwOnError: true,
    });

    return response.data as AuthResponse;
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

  async tellerLogin(electionGuid: string, accessCode: string): Promise<{ electionGuid: string; electionName: string }> {
    const response = await api.post('/api/auth/teller-login', {
      electionGuid,
      accessCode,
    });
    return response.data;
  },
};

export {
  type RegisterRequest,
  type LoginRequest,
} from "../api/gen/configService/types.gen";
