<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch, onBeforeUnmount } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import { ElMessage } from "element-plus";
import type { FormInstance, FormRules } from "element-plus";

const { t, locale } = useI18n();
const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();

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
  }
);

const isStandardLogin = computed(
  () => mode.value === "officer" || mode.value === "full-teller"
);
const isVoterLogin = computed(() => mode.value === "voter");
const isTellerLogin = computed(() => mode.value === "teller");

const loginForm = reactive({
  email: "",
  password: "",
  code: "", // Voter OTC
  passcode: "", // Teller Passcode
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
      { required: true, message: t("auth.voterLogin.codeRequired"), trigger: "blur" },
    ];
  }

  if (isTellerLogin.value) {
    baseRules.passcode = [
      {
        required: true,
        message: t("auth.tellerLogin.passcodeRequired"),
        trigger: "blur",
      },
    ];
  }

  return baseRules;
});

const requestCode = async () => {
  if (!loginForm.email) {
    ElMessage.warning(t("auth.emailRequired"));
    return;
  }
  loading.value = true;
  try {
    // API Call for System 3 OTC request would go here
    // await authService.requestVoterCode(loginForm.email);
    ElMessage.success(t("auth.voterLogin.emailSent"));
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
        ElMessage.warning("Voter OTC login not fully implemented in backend yet.");
        return;
      } else if (isTellerLogin.value) {
        // Handle System 2 login (Passcode)
        // await authStore.loginTeller(loginForm.passcode);
        ElMessage.warning(
          "Guest Teller passcode login not fully implemented in backend yet."
        );
        return;
      }

      ElMessage.success(t("auth.loginSuccess"));
      const redirectPath = (route.query.redirect as string) || "/dashboard";
      router.push(redirectPath);
    } catch (error) {
      console.error("Login failed:", error);
      ElMessage.error(t("auth.loginFailed"));
    } finally {
      loading.value = false;
    }
  });
};

const handleGoogleLogin = () => {
  const apiUrl = import.meta.env.VITE_API_URL || "http://localhost:5016";
  const redirectParam = route.query.redirect
    ? `?redirect=${encodeURIComponent(route.query.redirect as string)}`
    : "";
  const returnUrl = encodeURIComponent(
    globalThis.location.origin + "/auth/google/callback" + redirectParam
  );

  // Try the Google OAuth redirect
  globalThis.location.href = `${apiUrl}/api/auth/google/login?returnUrl=${returnUrl}`;
};

// add handler so if ESC is pressed, we go back to landing page
const handleKeydown = (event: KeyboardEvent) => {
  if (event.key === "Escape") {
    router.push("/");
  }
};

onMounted(() => {
  if (authStore.isAuthenticated && isStandardLogin.value) {
    router.push("/dashboard");
  }
  globalThis.addEventListener("keydown", handleKeydown);
});

onBeforeUnmount(() => {
  globalThis.removeEventListener("keydown", handleKeydown);
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
                  .replace(" ", "")}`
              )
            }}
          </h2>
          <h2 v-else-if="isVoterLogin">{{ t("auth.voterLogin.title") }}</h2>
          <h2 v-else-if="isTellerLogin">{{ t("auth.tellerLogin.title") }}</h2>

          <p class="mode-hint">
            {{
              isStandardLogin
                ? t("auth.landing.optionOfficerDesc")
                : isVoterLogin
                  ? t("auth.landing.optionVoterDesc")
                  : t("auth.landing.optionTellerDesc")
            }}
          </p>
        </div>
      </template>

      <!-- Social login only for Officers -->
      <div class="social-login" v-if="mode === 'officer'">
        <el-button class="google-btn" @click="handleGoogleLogin">
          <img src="https://www.gstatic.com/firebasejs/ui/2.0.0/images/auth/google.svg" alt="Google" />
          <span>{{ t("auth.googleLogin") }}</span>
        </el-button>
        <el-divider>{{ t("common.or") }}</el-divider>
      </div>

      <el-form ref="loginFormRef" :model="loginForm" :rules="rules" label-position="top" @keyup.enter="handleLogin">
        <!-- System 1 & 3: Email Field -->
        <el-form-item v-if="!isTellerLogin" :label="t('auth.email')" prop="email">
          <el-input v-model="loginForm.email" :placeholder="t('auth.emailPlaceholder')"
            :disabled="isVoterLogin && codeSent" autofocus />
        </el-form-item>

        <!-- System 1: Password Field -->
        <el-form-item v-if="isStandardLogin" :label="t('auth.password')" prop="password">
          <el-input v-model="loginForm.password" type="password" :placeholder="t('auth.passwordPlaceholder')"
            show-password />
        </el-form-item>

        <!-- System 3: One-Time Code Field -->
        <el-form-item v-if="isVoterLogin && codeSent" :label="t('auth.voterLogin.codeLabel')" prop="code">
          <el-input v-model="loginForm.code" :placeholder="t('auth.voterLogin.codePlaceholder')" maxlength="6" />
        </el-form-item>

        <!-- System 2: Election Passcode Field -->
        <el-form-item v-if="isTellerLogin" :label="t('auth.tellerLogin.passcodeLabel')" prop="passcode">
          <el-input v-model="loginForm.passcode" :placeholder="t('auth.tellerLogin.passcodePlaceholder')" />
        </el-form-item>

        <div class="login-actions">
          <!-- System 3: Request Code Button -->
          <el-button v-if="isVoterLogin && !codeSent" type="primary" :loading="loading" class="submit-btn"
            @click="requestCode">
            {{ t("auth.voterLogin.requestButton") }}
          </el-button>

          <!-- General Login Button -->
          <el-button v-else type="primary" :loading="loading" class="submit-btn" @click="handleLogin">
            {{
              isVoterLogin
                ? t("auth.voterLogin.loginButton")
                : isTellerLogin
                  ? t("auth.tellerLogin.loginButton")
                  : t("auth.loginButton")
            }}
          </el-button>

          <el-button v-if="isVoterLogin && codeSent" link class="retry-link" @click="codeSent = false">
            {{ t("common.tryAgain") }}
          </el-button>
        </div>

        <div class="auth-links">
          <router-link to="/register" v-if="mode === 'officer'" class="register">
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
