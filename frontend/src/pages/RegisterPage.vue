<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { getAppConfig } from "@/config/appConfig";
import { authService } from "@/services/authService";
import { extractApiErrorMessage } from "@/utils/errorHandler";
import type { FormInstance, FormRules } from "element-plus";
import { computed, onMounted, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const appConfig = getAppConfig();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const inviteToken = computed(() => {
  const raw = route.query.invite;
  return typeof raw === "string" ? raw.trim() : "";
});

const inviteChecked = ref(!inviteToken.value);
const inviteValid = ref(false);
const inviteLoading = ref(false);
const submitted = ref(false);

const registerFormRef = ref<FormInstance>();
const loading = ref(false);

const registerForm = reactive({
  email: "",
  displayName: "",
  password: "",
  confirmPassword: "",
});

const validatePassword = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
  if (!value) {
    callback(new Error(t("auth.passwordRequired")));
    return;
  }

  const errors = [];
  if (value.length < 12) {
    errors.push(t("auth.passwordMinLength12"));
  }
  if (!/(?=.*[a-z])/.test(value)) {
    errors.push(t("auth.passwordRequireLowercase"));
  }
  if (!/(?=.*[A-Z])/.test(value)) {
    errors.push(t("auth.passwordRequireUppercase"));
  }
  if (!/(?=.*\d)/.test(value)) {
    errors.push(t("auth.passwordRequireDigit"));
  }
  if (!/(?=.*[^a-zA-Z\d])/.test(value)) {
    errors.push(t("auth.passwordRequireSpecial"));
  }

  if (errors.length > 0) {
    callback(new Error(errors.join(" ")));
  } else {
    callback();
  }
};

const validatePass2 = (_rule: unknown, value: string, callback: (error?: Error) => void) => {
  if (!value) {
    callback(new Error(t("auth.confirmPasswordRequired")));
  } else if (value !== registerForm.password) {
    callback(new Error(t("auth.passwordMismatch")));
  } else {
    callback();
  }
};

const rules = reactive<FormRules>({
  email: [
    {
      required: true,
      validator: (_rule, value: string, callback) => {
        if (!value) {
          callback(new Error(t("auth.emailRequired")));
        } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(value)) {
          callback(new Error(t("auth.emailInvalid")));
        } else {
          callback();
        }
      },
      trigger: "blur",
    },
  ],
  displayName: [
    {
      required: true,
      validator: (_rule, value: string, callback) => {
        if (!value || !String(value).trim()) {
          callback(new Error(t("auth.displayNameRequired")));
        } else if (String(value).trim().length > 200) {
          callback(new Error(t("auth.displayNameMaxLength")));
        } else {
          callback();
        }
      },
      trigger: "blur",
    },
  ],
  password: [{ validator: validatePassword, trigger: "blur" }],
  confirmPassword: [{ validator: validatePass2, trigger: "blur" }],
});

const goToLogin = () => {
  router.push({ path: "/login", query: route.query });
};

const handleGoogleLogin = () => {
  const apiUrl = appConfig.apiUrl;
  const redirectParam = route.query.redirect
    ? `?redirect=${encodeURIComponent(route.query.redirect as string)}`
    : "";
  const returnUrl = encodeURIComponent(
    globalThis.location.origin + "/auth/google/callback" + redirectParam,
  );

  globalThis.location.href = `${apiUrl}/api/auth/google/login?returnUrl=${returnUrl}`;
};

const handleRegister = async () => {
  if (!registerFormRef.value || !inviteToken.value) {
    return;
  }

  await registerFormRef.value.validate(async (valid) => {
    if (!valid) {
      return;
    }

    loading.value = true;
    try {
      const response = await authService.registerWithInvite({
        token: inviteToken.value,
        email: registerForm.email,
        displayName: registerForm.displayName.trim(),
        password: registerForm.password,
        confirmPassword: registerForm.confirmPassword,
      });
      submitted.value = true;
      if (response.requiresEmailVerification) {
        showSuccessMessage(t("auth.registerCheckEmail"));
      } else {
        showSuccessMessage(t("auth.registerSuccess"));
      }
    } catch (error) {
      showErrorMessage(extractApiErrorMessage(error));
    } finally {
      loading.value = false;
    }
  });
};

onMounted(async () => {
  if (!inviteToken.value) {
    inviteChecked.value = true;
    inviteValid.value = false;
    return;
  }

  inviteLoading.value = true;
  try {
    const status = await authService.peekAccountInvite(inviteToken.value);
    inviteValid.value = status.valid === true;
  } catch {
    inviteValid.value = false;
  } finally {
    inviteChecked.value = true;
    inviteLoading.value = false;
  }
});
</script>

<template>
  <div class="register-page">
    <el-card class="register-card">
      <template #header>
        <div class="register-header">
          <h2>
            {{
              inviteToken
                ? t("auth.inviteRegisterTitle")
                : t("auth.registerClosedTitle")
            }}
          </h2>
        </div>
      </template>

      <template v-if="!inviteToken">
        <p class="register-closed-body">
          {{ t("auth.registerClosedBody") }}
        </p>

        <div class="register-actions">
          <el-button type="primary" class="submit-btn" @click="handleGoogleLogin">
            {{ t("auth.googleLogin") }}
          </el-button>
          <el-button class="submit-btn" @click="goToLogin">
            {{ t("auth.hasAccount") }}
          </el-button>
        </div>
      </template>

      <p v-else-if="inviteLoading" class="register-closed-body">
        {{ t("auth.inviteChecking") }}
      </p>

      <div v-else-if="submitted" class="register-actions">
        <p class="register-closed-body">{{ t("auth.registerCheckEmail") }}</p>
        <el-button type="primary" class="submit-btn" @click="goToLogin">
          {{ t("auth.hasAccount") }}
        </el-button>
      </div>

      <template v-else-if="inviteChecked && !inviteValid">
        <p class="register-closed-body">
          {{ t("auth.errors.invalidInvite") }}
        </p>
        <div class="register-actions">
          <el-button type="primary" class="submit-btn" @click="handleGoogleLogin">
            {{ t("auth.googleLogin") }}
          </el-button>
          <el-button class="submit-btn" @click="goToLogin">
            {{ t("auth.hasAccount") }}
          </el-button>
        </div>
      </template>

      <el-form
        v-else
        ref="registerFormRef"
        :model="registerForm"
        :rules="rules"
        label-position="top"
        @keyup.enter="handleRegister"
      >
        <p class="register-closed-body">{{ t("auth.inviteRegisterBody") }}</p>

        <el-form-item :label="t('auth.email')" prop="email">
          <el-input
            v-model="registerForm.email"
            :placeholder="t('auth.emailPlaceholder')"
          />
        </el-form-item>

        <el-form-item :label="t('auth.displayName')" prop="displayName">
          <el-input
            v-model="registerForm.displayName"
            :placeholder="t('auth.displayNamePlaceholder')"
            maxlength="200"
          />
        </el-form-item>

        <el-form-item :label="t('auth.password')" prop="password">
          <el-input
            v-model="registerForm.password"
            type="password"
            :placeholder="t('auth.passwordPlaceholder')"
            show-password
          />
        </el-form-item>

        <el-form-item :label="t('auth.confirmPassword')" prop="confirmPassword">
          <el-input
            v-model="registerForm.confirmPassword"
            type="password"
            :placeholder="t('auth.confirmPasswordPlaceholder')"
            show-password
          />
        </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            class="submit-btn"
            :loading="loading"
            @click="handleRegister"
          >
            {{ t("auth.registerButton") }}
          </el-button>
        </el-form-item>
      </el-form>
    </el-card>
  </div>
</template>

<style lang="less">
.register-page {
  display: flex;
  justify-content: center;
  align-items: center;
  padding-top: 40px;

  .register-card {
    width: 100%;
    max-width: 400px;
    border-radius: 12px;
  }

  .register-header {
    text-align: center;
  }

  .register-header h2 {
    margin: 0;
    color: #b0caff;
  }

  .register-closed-body {
    margin: 0 0 24px;
    color: var(--color-text-secondary);
    text-align: center;
    line-height: 1.5;
  }

  .register-actions {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }

  .submit-btn {
    width: 100%;
  }
}
</style>
