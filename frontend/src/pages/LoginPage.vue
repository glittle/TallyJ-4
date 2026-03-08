<script setup lang="ts">
import type { GoogleCredentialResponse } from "../types/google-one-tap";

declare global {
  interface Window {
    google: any;
  }
}

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

const fetchGoogleClientId = async (): Promise<string | null> => {
  try {
    const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5016";
    const resp = await fetch(`${apiUrl}/api/public/auth-config`);
    if (!resp.ok) return null;
    const json = await resp.json();
    return json?.data?.googleClientId || null;
  } catch {
    return null;
  }
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
}
</style>
