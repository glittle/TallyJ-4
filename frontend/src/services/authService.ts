import {
  postApiAuthRegisterAccount,
  postApiAuthLogin,
  postApiAuthForgotPassword,
  postApiAuthResetPassword,
  postApiAuthSetup2Fa,
  postApiAuthEnable2Fa,
  postApiAuthDisable2Fa,
  getApiAuth2FaStatus,
  postApiAuthGoogleOneTap,
  postApiAuthTelegram,
  postApiAuthFacebook,
  postApiAuthKakao,
  postApiAuthLogout,
  postApiAuthTellerLogin,
  postApiAuthVerifyEmail,
  putApiAccountUpdateProfile,
} from "@/api/gen/configService";
import { client } from "@/api/gen/configService/client.gen";
import type {
  RegisterRequest,
  LoginRequest,
  GoogleOneTapRequest,
  AccountUserProfileDto,
} from "@/api/gen/configService/types.gen";
import type { TelegramLoginRequest } from "../types";

export interface AuthResponse {
  email: string;
  name?: string;
  authMethod?: string;
  requires2FA: boolean;
  requiresEmailVerification?: boolean;
}

export interface TwoFactorSetupResponse {
  secret: string;
  qrCodeDataUrl: string;
}

export interface UserProfile {
  id?: string | null;
  userName?: string | null;
  displayName?: string | null;
  email?: string | null;
  emailConfirmed?: boolean;
  pendingEmail?: string | null;
  authMethod?: string | null;
  canChangeEmail?: boolean;
}

function unwrapProfile(response: {
  data?: { data?: AccountUserProfileDto } | AccountUserProfileDto | null;
}): UserProfile {
  const outer = response.data as { data?: UserProfile } | UserProfile | null;
  if (outer && typeof outer === "object" && "data" in outer && outer.data) {
    return outer.data as UserProfile;
  }
  return (outer ?? {}) as UserProfile;
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

  async verifyEmail(email: string, token: string): Promise<void> {
    await postApiAuthVerifyEmail({
      body: { email, token },
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

  async updateDisplayName(displayName: string): Promise<UserProfile> {
    // Prefer dedicated endpoint when backend is updated; fall back to updateProfile body.
    try {
      const response = await client.post({
        url: "/api/Account/changeDisplayName",
        body: { displayName },
        throwOnError: true,
      });
      return unwrapProfile(response as { data?: unknown });
    } catch {
      const response = await putApiAccountUpdateProfile({
        body: { displayName } as never,
        throwOnError: true,
      });
      return unwrapProfile(response);
    }
  },

  async getMyProfile(): Promise<UserProfile> {
    const response = await client.get({
      url: "/api/Account/getMyProfile",
      throwOnError: true,
    });
    return unwrapProfile(response as { data?: unknown });
  },

  async requestEmailChange(
    newEmail: string,
    currentPassword: string,
  ): Promise<void> {
    await client.post({
      url: "/api/Account/requestEmailChange",
      body: { newEmail, currentPassword },
      throwOnError: true,
    });
  },

  async confirmEmailChange(params: {
    token?: string;
    code?: string;
  }): Promise<void> {
    // Prefer anonymous Auth endpoint so link confirmation works without session.
    await client.post({
      url: "/api/Auth/confirmEmailChange",
      body: params,
      throwOnError: true,
    });
  },

  async get2FAStatus(): Promise<{
    isEnabled: boolean;
    method: string | null;
  }> {
    const response = await getApiAuth2FaStatus();
    return response.data as { isEnabled: boolean; method: string | null };
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
      body: data,
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
      body: { electionGuid, accessCode },
      throwOnError: true,
    });
    return response.data as { electionGuid: string; electionName: string };
  },
};

export {
  type RegisterRequest,
  type LoginRequest,
} from "@/api/gen/configService/types.gen";
