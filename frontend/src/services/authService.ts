import {
  getApiAuth2FaStatus,
  postApiAuthDisable2Fa,
  postApiAuthEnable2Fa,
  postApiAuthFacebook,
  postApiAuthForgotPassword,
  postApiAuthGoogleOneTap,
  postApiAuthKakao,
  postApiAuthLogin,
  postApiAuthLogout,
  postApiAuthRegisterAccount,
  postApiAuthResetPassword,
  postApiAuthSetup2Fa,
  postApiAuthTelegram,
  postApiAuthTellerLogin,
} from "../api/gen/configService/sdk.gen";
import type {
  GoogleOneTapRequest,
  LoginRequest,
  RegisterRequest,
} from "../api/gen/configService/types.gen";
import type { TelegramLoginRequest } from "../types";

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

  async get2FAStatus(): Promise<{ isEnabled: boolean; method: string | null }> {
    const response = await getApiAuth2FaStatus({
      throwOnError: true,
    });
    return response.data as unknown as {
      isEnabled: boolean;
      method: string | null;
    };
  },

  async googleOneTap(credential: string): Promise<AuthResponse> {
    const response = await postApiAuthGoogleOneTap({
      body: { credential } as GoogleOneTapRequest,
      throwOnError: true,
    });

    return response.data as AuthResponse;
  },

  async telegramLogin(data: TelegramLoginRequest): Promise<AuthResponse> {
    const response = await postApiAuthTelegram({
      body: data as unknown as any,
      throwOnError: true,
    });
    return response.data as AuthResponse;
  },

  async facebookLogin(accessToken: string): Promise<AuthResponse> {
    const response = await postApiAuthFacebook({
      body: { accessToken },
      throwOnError: true,
    });
    return response.data as AuthResponse;
  },

  async kakaoLogin(accessToken: string): Promise<AuthResponse> {
    const response = await postApiAuthKakao({
      body: { accessToken },
      throwOnError: true,
    });
    return response.data as AuthResponse;
  },

  async logout(): Promise<void> {
    await postApiAuthLogout({
      throwOnError: true,
    });
  },

  async tellerLogin(
    electionGuid: string,
    accessCode: string,
  ): Promise<{ electionGuid: string; electionName: string }> {
    const response = await postApiAuthTellerLogin({
      body: {
        electionGuid,
        accessCode,
      },
      throwOnError: true,
    });
    return response.data as unknown as {
      electionGuid: string;
      electionName: string;
    };
  },
};

export {
  type LoginRequest,
  type RegisterRequest,
} from "../api/gen/configService/types.gen";
