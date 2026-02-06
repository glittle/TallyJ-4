import { postApiAuthRegister, postApiAuthLogin, postApiAuthPasswordForgot, postApiAuthPasswordReset, postApiAuth2FaSetup, postApiAuth2FaEnable, postApiAuth2FaDisable } from '../api/gen/configService/sdk.gen';
import type { RegisterRequest, LoginRequest } from '../api/gen/configService/types.gen';

export type { RegisterRequest, LoginRequest };

export interface AuthResponse {
  token: string;
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
    const response = await postApiAuthRegister({
      body: data,
      throwOnError: true
    });
    return response.data as AuthResponse;
  },

  async login(data: LoginRequest): Promise<AuthResponse> {
    const response = await postApiAuthLogin({
      body: data,
      throwOnError: true
    });
    return response.data as AuthResponse;
  },

  async forgotPassword(email: string): Promise<void> {
    await postApiAuthPasswordForgot({
      body: { email },
      throwOnError: true
    });
  },

  async resetPassword(email: string, token: string, newPassword: string, confirmPassword: string): Promise<void> {
    await postApiAuthPasswordReset({
      body: {
        email,
        token,
        newPassword,
        confirmPassword
      },
      throwOnError: true
    });
  },

  async setup2FA(): Promise<TwoFactorSetupResponse> {
    const response = await postApiAuth2FaSetup({
      throwOnError: true
    });
    return response.data as TwoFactorSetupResponse;
  },

  async enable2FA(code: string): Promise<void> {
    await postApiAuth2FaEnable({
      body: { code },
      throwOnError: true
    });
  },

  async disable2FA(password: string, code: string): Promise<void> {
    await postApiAuth2FaDisable({
      body: { password, code },
      throwOnError: true
    });
  }
};
