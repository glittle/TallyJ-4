<script setup lang="ts">
import type { GoogleCredentialResponse } from "../types/google-one-tap";

declare global {
  interface Window {
    google: any;
  }
}

declare const FB: any;
declare const Kakao: any;

import {
  ref,
  reactive,
  computed,
  onMounted,
  watch,
  nextTick,
  onBeforeUnmount,
} from "vue";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import { useNotifications } from "@/composables/useNotifications";
import type { FormInstance, FormRules } from "element-plus";
import TelegramLoginButton from "../components/auth/TelegramLoginButton.vue";

const { t, locale } = useI18n();
const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const { showSuccessMessage, showErrorMessage, showWarningMessage } =
  useNotifications();

const loginFormRef = ref<FormInstance>();
const loading = ref(false);
const codeSent = ref(false);

// Mode determines which UI to show
const mode = ref((route.query.mode as string) || "officer");

// Watch for query changes to update mode
watch(
  () => route.query.mode,
  (newMode) => {
    if (newMode) {
      mode.value = newMode as string;
    }
  },
);

const isStandardLogin = computed(
  () => mode.value === "officer" || mode.value === "full-teller",
);
const isVoterLogin = computed(() => mode.value === "voter");

const loginForm = reactive({
  email: "",
  password: "",
  code: "", // Voter OTC
});

const rules = computed<FormRules>(() => {
  const baseRules: FormRules = {};

  if (isStandardLogin.value || isVoterLogin.value) {
    baseRules.email = [
      { required: true, message: t("auth.emailRequired"), trigger: "blur" },
      { type: "email", message: t("auth.emailInvalid"), trigger: "blur" },
    ];
  }

  if (isStandardLogin.value) {
    baseRules.password = [
      { required: true, message: t("auth.passwordRequired"), trigger: "blur" },
    ];
  }

  if (isVoterLogin.value && codeSent.value) {
    baseRules.code = [
      {
        required: true,
        message: t("auth.voterLogin.codeRequired"),
        trigger: "blur",
      },
    ];
  }

  return baseRules;
});

const requestCode = async () => {
  if (!loginForm.email) {
    showWarningMessage(t("auth.emailRequired"));
    return;
  }
  loading.value = true;
  try {
    // API Call for System 3 OTC request would go here
    // await authService.requestVoterCode(loginForm.email);
    showSuccessMessage(t("auth.voterLogin.emailSent"));
    codeSent.value = true;
  } finally {
    loading.value = false;
  }
};

const handleLogin = async () => {
  if (!loginFormRef.value) return;

  await loginFormRef.value.validate(async (valid) => {
    if (!valid) return;

    loading.value = true;
    try {
      if (isStandardLogin.value) {
        await authStore.login({
          email: loginForm.email,
          password: loginForm.password,
        });
      } else if (isVoterLogin.value) {
        // Handle System 3 login (OTC)
        // await authStore.loginVoter(loginForm.email, loginForm.code);
        showWarningMessage(
          "Voter OTC login not fully implemented in backend yet.",
        );
        return;
      }

      showSuccessMessage(t("auth.loginSuccess"));
      const redirectPath = (route.query.redirect as string) || "/dashboard";
      router.push(redirectPath);
    } catch (error) {
      console.error("Login failed:", error);
      showErrorMessage(t("auth.loginFailed"));
    } finally {
      loading.value = false;
    }
  });
};

const googleButtonRef = ref<HTMLElement>();
const googleClientId = ref<string | null>(null);
const googleReady = ref(false);
const telegramBotUsername = ref<string | null>(null);
const telegramReady = ref(false);

const fbReady = ref(false);
const fbError = ref(false);
const fbScriptLoaded = ref(false);

const kakaoReady = ref(false);
const kakaoError = ref(false);
const kakaoScriptLoaded = ref(false);

const authConfig = ref<{
  googleClientId?: string;
  facebookAppId?: string;
  kakaoJsKey?: string;
  telegramBotUsername?: string;
} | null>(null);

const gisScriptLoaded = ref(false);
let gisCleanup: (() => void) | null = null;

const handleGoogleOneTapCallback = async (
  response: GoogleCredentialResponse,
) => {
  loading.value = true;
  try {
    await authStore.googleOneTapLogin(response.credential);
    showSuccessMessage(t("auth.loginSuccess"));
    const redirectPath = (route.query.redirect as string) || "/dashboard";
    router.push(redirectPath);
  } catch {
    showErrorMessage(t("auth.googleLoginFailed"));
  } finally {
    loading.value = false;
  }
};

const handleTelegramSuccess = async (user: any) => {
  loading.value = true;
  try {
    await authStore.telegramLogin({
      id: user.id,
      firstName: user.first_name,
      lastName: user.last_name,
      username: user.username,
      photoUrl: user.photo_url,
      authDate: user.auth_date,
      hash: user.hash,
    });
    showSuccessMessage(t("auth.loginSuccess"));
    const redirectPath = (route.query.redirect as string) || "/dashboard";
    router.push(redirectPath);
  } catch {
    showErrorMessage(t("auth.googleLoginFailed"));
  } finally {
    loading.value = false;
  }
};

const loadGisScript = (): Promise<void> => {
  return new Promise((resolve, reject) => {
    if (gisScriptLoaded.value || typeof google !== "undefined") {
      gisScriptLoaded.value = true;
      resolve();
      return;
    }

    const script = document.createElement("script");
    script.src = "https://accounts.google.com/gsi/client";
    script.async = true;
    script.defer = true;

    script.onload = () => {
      gisScriptLoaded.value = true;
      resolve();
    };

    script.onerror = () => {
      reject(new Error("Failed to load Google Identity Services script"));
    };

    document.head.appendChild(script);
  });
};

const fetchAuthConfig = async () => {
  if (authConfig.value) return authConfig.value;
  try {
    const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5016";
    const resp = await fetch(`${apiUrl}/api/public/auth-config`);
    if (!resp.ok) return null;
    const json = await resp.json();
    authConfig.value = json?.data ?? null;
    telegramBotUsername.value = authConfig.value?.telegramBotUsername || null;
    telegramReady.value = Boolean(telegramBotUsername.value);
    return authConfig.value;
  } catch {
    return null;
  }
};
const fetchGoogleClientId = async (): Promise<string | null> => {
  const config = await fetchAuthConfig();
  return config?.googleClientId || null;
};
const renderGoogleButton = () => {
  nextTick(() => {
    if (googleButtonRef.value && googleClientId.value && googleReady.value) {
      console.log("Rendering Google One Tap button with locale:", locale.value);
      window.google.accounts.id.renderButton(googleButtonRef.value, {
        type: "standard",
        theme: "outline",
        size: "large",
        text: "signin_with",
        shape: "rectangular",
        width: "300",
        locale: locale.value,
      });
    }
  });
};

watch(googleButtonRef, (el) => {
  if (el && googleReady.value) {
    renderGoogleButton();
  }
});

const initGoogleOneTap = async () => {
  try {
    // Lazy load the GIS script only when needed
    await loadGisScript();
    const clientId = await fetchGoogleClientId();
    googleClientId.value = clientId;

    if (!clientId || typeof window.google === "undefined") {
      googleReady.value = false;
      return;
    }

    window.google.accounts.id.initialize({
      client_id: clientId,
      callback: handleGoogleOneTapCallback,
      auto_select: false,
      cancel_on_tap_outside: true,
      use_fedcm_for_prompt: true,
    });

    googleReady.value = true;
    renderGoogleButton();
    window.google.accounts.id.prompt();

    // Store cleanup function
    gisCleanup = () => {
      if (typeof window.google !== "undefined" && googleReady.value) {
        try {
          window.google.accounts.id.cancel();
        } catch {
          // Ignore errors from cancel
        }
      }
      googleReady.value = false;
    };
  } catch (error) {
    console.error("Failed to initialize Google One Tap:", error);
    googleReady.value = false;
  }
};

const teardownGoogleOneTap = () => {
  if (gisCleanup) {
    gisCleanup();
    gisCleanup = null;
  }
};

const handleFacebookLogin = async () => {
  try {
    loading.value = true;
    if (typeof FB === "undefined") {
      console.error("Facebook SDK not loaded");
      showErrorMessage(t("voting.auth.facebook.error"));
      loading.value = false;
      return;
    }
    const timeoutId = setTimeout(() => {
      console.warn("Facebook login timeout - callback never called");
      loading.value = false;
      showErrorMessage(t("voting.auth.facebook.popupBlocked"));
    }, 10000);
    try {
      FB.login(
        async (res: any) => {
          clearTimeout(timeoutId);
          try {
            if (res.authResponse?.accessToken) {
              await authStore.facebookLogin(res.authResponse.accessToken);
              showSuccessMessage(t("auth.loginSuccess"));
              const redirectPath = (route.query.redirect as string) || "/dashboard";
              await router.push(redirectPath);
            } else {
              showErrorMessage(t("voting.auth.facebook.cancelled"));
            }
          } catch (callbackError) {
            console.error("Error during Facebook login callback:", callbackError);
            showErrorMessage(t("auth.loginFailed"));
          } finally {
            loading.value = false;
          }
        },
        { scope: "email" },
      );
    } catch (syncError) {
      console.error("FB.login threw synchronously:", syncError);
      clearTimeout(timeoutId);
      showErrorMessage(t("voting.auth.facebook.error"));
      loading.value = false;
    }
  } catch (error) {
    console.error("Error with Facebook authentication:", error);
    showErrorMessage(t("auth.loginFailed"));
    loading.value = false;
  }
};

const loadFacebookSdk = (): Promise<void> => {
  return new Promise((resolve, reject) => {
    if (fbScriptLoaded.value || typeof FB !== "undefined") {
      fbScriptLoaded.value = true;
      resolve();
      return;
    }
    const script = document.createElement("script");
    script.src = "https://connect.facebook.net/en_US/sdk.js";
    script.async = true;
    script.defer = true;
    script.crossOrigin = "anonymous";
    script.onload = () => {
      fbScriptLoaded.value = true;
      resolve();
    };
    script.onerror = () => reject(new Error("Failed to load Facebook SDK"));
    document.head.appendChild(script);
  });
};

const initFacebookSdk = async () => {
  try {
    fbError.value = false;
    const config = await fetchAuthConfig();
    if (!config?.facebookAppId) {
      fbError.value = true;
      return;
    }
    await loadFacebookSdk();
    FB.init({
      appId: config.facebookAppId,
      cookie: true,
      xfbml: true,
      version: "v18.0",
    });
    fbReady.value = true;
  } catch (error) {
    console.error("Failed to initialize Facebook SDK:", error);
    fbError.value = true;
  }
};

const handleKakaoLogin = async () => {
  try {
    loading.value = true;
    const timeoutId = setTimeout(() => {
      loading.value = false;
      showErrorMessage(t("voting.auth.kakao.popupBlocked"));
    }, 10000);
    Kakao.Auth.login({
      success: async (authObj: any) => {
        clearTimeout(timeoutId);
        try {
          await authStore.kakaoLogin(authObj.access_token);
          showSuccessMessage(t("auth.loginSuccess"));
          const redirectPath = (route.query.redirect as string) || "/dashboard";
          router.push(redirectPath);
        } catch (error) {
          console.error("Kakao login success handler failed:", error);
          showErrorMessage(t("auth.loginFailed"));
        } finally {
          loading.value = false;
        }
      },
      fail: (err: any) => {
        clearTimeout(timeoutId);
        console.error("Kakao login failed:", err);
        showErrorMessage(t("auth.loginFailed"));
        loading.value = false;
      },
    });
  } catch (error) {
    console.error("Error with Kakao authentication:", error);
    showErrorMessage(t("auth.loginFailed"));
    loading.value = false;
  }
};

const loadKakaoSdk = (): Promise<void> => {
  return new Promise((resolve, reject) => {
    if (kakaoScriptLoaded.value || typeof Kakao !== "undefined") {
      kakaoScriptLoaded.value = true;
      resolve();
      return;
    }
    const script = document.createElement("script");
    script.src = "https://t1.kakaocdn.net/kakao_js_sdk/2.7.2/kakao.min.js";
    script.async = true;
    script.onload = () => {
      kakaoScriptLoaded.value = true;
      resolve();
    };
    script.onerror = () => reject(new Error("Failed to load Kakao SDK"));
    document.head.appendChild(script);
  });
};

const initKakaoSdk = async () => {
  try {
    kakaoError.value = false;
    const config = await fetchAuthConfig();
    if (!config?.kakaoJsKey) {
      kakaoError.value = true;
      return;
    }
    await loadKakaoSdk();
    if (!Kakao.isInitialized()) {
      Kakao.init(config.kakaoJsKey);
    }
    kakaoReady.value = true;
  } catch (error) {
    console.error("Failed to initialize Kakao SDK:", error);
    kakaoError.value = true;
  }
};

const handleGoogleLogin = () => {
  const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5016";
  const redirectParam = route.query.redirect
    ? `?redirect=${encodeURIComponent(route.query.redirect as string)}`
    : "";
  const returnUrl = encodeURIComponent(
    globalThis.location.origin + "/auth/google/callback" + redirectParam,
  );

  globalThis.location.href = `${apiUrl}/api/auth/google/login?returnUrl=${returnUrl}`;
};

// add handler so if ESC is pressed, we go back to landing page
const handleKeydown = (event: KeyboardEvent) => {
  if (event.key === "Escape") {
    router.push("/");
  }
};

// Watch mode changes to initialize/teardown Google One Tap
watch(
  () => mode.value,
  (newMode, oldMode) => {
    if (newMode === "officer") {
      // Initialize Google One Tap when switching into officer mode
      initGoogleOneTap();
      initFacebookSdk();
      initKakaoSdk();
    } else if (oldMode === "officer") {
      // Tear down Google One Tap when leaving officer mode
      teardownGoogleOneTap();
    }
  },
);

onMounted(() => {
  if (authStore.isAuthenticated && isStandardLogin.value) {
    router.push("/dashboard");
  }
  globalThis.addEventListener("keydown", handleKeydown);
  if (mode.value === "officer") {
    initGoogleOneTap();
    initFacebookSdk();
    initKakaoSdk();
  }
});

onBeforeUnmount(() => {
  globalThis.removeEventListener("keydown", handleKeydown);
  teardownGoogleOneTap();
});
</script>

<template>
  <div class="login-page">
    <el-card class="login-card">
      <template #header>
        <div class="login-header">
          <h2 v-if="isStandardLogin">
            {{
              t(
                `auth.landing.login${mode
                  .replace("-", " ")
                  .replace(/\b\w/g, (l) => l.toUpperCase())
                  .replace(" ", "")}`,
              )
            }}
          </h2>
          <h2 v-else-if="isVoterLogin">{{ t("auth.voterLogin.title") }}</h2>

          <p class="mode-hint">
            {{
              isStandardLogin
                ? t("auth.landing.optionOfficerDesc")
                : t("auth.landing.optionVoterDesc")
            }}
          </p>
        </div>
      </template>

      <!-- Social login only for Officers -->
      <div v-if="mode === 'officer'" class="social-login">
        <div
          v-if="googleReady"
          ref="googleButtonRef"
          class="google-btn-container"
        ></div>
        <el-button
          v-if="!googleReady"
          class="google-btn"
          @click="handleGoogleLogin"
        >
          <img
            src="https://www.gstatic.com/firebasejs/ui/2.0.0/images/auth/google.svg"
            alt="Google"
          />
          <span>{{ t("auth.googleLogin") }}</span>
        </el-button>

        <div v-if="fbReady" class="facebook-btn-container">
          <el-button class="facebook-btn" @click="handleFacebookLogin" :loading="loading">
            <svg viewBox="0 0 24 24" width="24" height="24" xmlns="http://www.w3.org/2000/svg">
              <path d="M24 12.073c0-6.627-5.373-12-12-12s-12 5.373-12 12c0 5.99 4.388 10.954 10.125 11.854v-8.385H7.078v-3.47h3.047V9.43c0-3.007 1.792-4.669 4.533-4.669 1.312 0 2.686.235 2.686.235v2.953H15.83c-1.491 0-1.956.925-1.956 1.874v2.25h3.328l-.532 3.47h-2.796v8.385C19.612 23.027 24 18.062 24 12.073z" fill="#1877F2"/>
            </svg>
            <span>{{ t("voting.auth.facebook.login") || 'Login with Facebook' }}</span>
          </el-button>
        </div>
        <div v-else-if="fbError">
          <el-alert :title="t('voting.auth.facebook.error') || 'Facebook login unavailable'" type="warning" :closable="false" />
        </div>

        <div v-if="kakaoReady" class="kakao-btn-container">
          <el-button class="kakao-btn" @click="handleKakaoLogin" :loading="loading">
            <svg viewBox="0 0 24 24" width="24" height="24" xmlns="http://www.w3.org/2000/svg">
              <path d="M12 3c-5.523 0-10 3.518-10 7.857 0 2.805 1.821 5.253 4.582 6.643-.243.91-1.025 3.861-1.053 4.004-.035.18.118.256.24.167.098-.071 3.253-2.203 4.536-3.111.551.082 1.118.125 1.695.125 5.523 0 10-3.518 10-7.857C22 6.518 17.523 3 12 3z" fill="#3E2723"/>
            </svg>
            <span>{{ t("voting.auth.kakao.button") || 'Login with Kakao' }}</span>
          </el-button>
        </div>
        <div v-else-if="kakaoError">
          <el-alert :title="t('voting.auth.kakao.error') || 'Kakao login unavailable'" type="warning" :closable="false" />
        </div>

        <div v-if="telegramReady && telegramBotUsername" class="telegram-btn">
          <TelegramLoginButton
            :bot-username="telegramBotUsername"
            @success="handleTelegramSuccess"
          />
        </div>
        <el-divider>{{ t("common.or") }}</el-divider>
      </div>

      <el-form
        ref="loginFormRef"
        :model="loginForm"
        :rules="rules"
        label-position="top"
        @keyup.enter="handleLogin"
      >
        <!-- System 1 & 3: Email Field -->
        <el-form-item :label="t('auth.email')" prop="email">
          <el-input
            v-model="loginForm.email"
            :placeholder="t('auth.emailPlaceholder')"
            :disabled="isVoterLogin && codeSent"
            autofocus
          />
        </el-form-item>

        <!-- System 1: Password Field -->
        <el-form-item
          v-if="isStandardLogin"
          :label="t('auth.password')"
          prop="password"
        >
          <el-input
            v-model="loginForm.password"
            type="password"
            :placeholder="t('auth.passwordPlaceholder')"
            show-password
          />
        </el-form-item>

        <!-- System 3: One-Time Code Field -->
        <el-form-item
          v-if="isVoterLogin && codeSent"
          :label="t('auth.voterLogin.codeLabel')"
          prop="code"
        >
          <el-input
            v-model="loginForm.code"
            :placeholder="t('auth.voterLogin.codePlaceholder')"
            maxlength="6"
          />
        </el-form-item>

        <div class="login-actions">
          <!-- System 3: Request Code Button -->
          <el-button
            v-if="isVoterLogin && !codeSent"
            type="primary"
            :loading="loading"
            class="submit-btn"
            @click="requestCode"
          >
            {{ t("auth.voterLogin.requestButton") }}
          </el-button>

          <!-- General Login Button -->
          <el-button
            v-else
            type="primary"
            :loading="loading"
            class="submit-btn"
            @click="handleLogin"
          >
            {{
              isVoterLogin
                ? t("auth.voterLogin.loginButton")
                : t("auth.loginButton")
            }}
          </el-button>

          <el-button
            v-if="isVoterLogin && codeSent"
            link
            class="retry-link"
            @click="codeSent = false"
          >
            {{ t("common.tryAgain") }}
          </el-button>
        </div>

        <div class="auth-links">
          <router-link
            v-if="mode === 'officer'"
            to="/register"
            class="register"
          >
            {{ t("auth.noAccount") }}
          </router-link>
          <router-link to="/">
            {{ t("common.cancel") }}
          </router-link>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<style lang="less">
.login-page {
  display: flex;
  justify-content: center;
  align-items: center;
  padding-top: 40px;

  .login-card {
    width: 100%;
    max-width: 400px;
    border-radius: 12px;
  }

  .login-header {
    text-align: center;
  }

  .login-header h2 {
    margin: 0;
    color: var(--color-text-primary);
  }

  .mode-hint {
    margin-top: 10px;
    color: var(--color-text-secondary);
    font-size: 0.9rem;
  }

  .login-actions {
    margin-top: 30px;
    display: flex;
    flex-direction: column;
    gap: 10px;
  }

  .submit-btn {
    width: 100%;
  }

  .retry-link {
    font-size: 0.85rem;
  }

  .auth-links {
    margin-top: 20px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
  }

  .auth-links a {
    color: var(--color-primary-500);
    text-decoration: none;
    font-size: 0.85rem;

    &.register {
      font-size: 1rem;
    }
  }

  .auth-links a:hover {
    text-decoration: underline;
  }

  .social-login {
    display: flex;
    flex-direction: column;
    margin-top: 20px;
    text-align: center;
    align-items: center;
    justify-content: center;
  }

  .el-divider--horizontal {
    margin: 2em 0;
  }

  .google-btn-container {
    display: flex;
    justify-content: center;
  }

  .google-btn {
    width: 80%;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
  }

  .google-btn img {
    width: 18px;
    height: 18px;
  }

  .facebook-btn-container, .kakao-btn-container {
    display: flex;
    justify-content: center;
    width: 100%;
    margin-top: 10px;
  }

  .facebook-btn, .kakao-btn {
    width: 80%;
    display: flex;
    align-items: center;
    justify-content: center;
    gap: 10px;
  }

  .telegram-btn {
    display: flex;
    justify-content: center;
    width: 100%;
    margin-top: 10px;
  }
}
</style>
