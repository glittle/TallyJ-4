<script setup lang="ts">
import { computed, onMounted, reactive, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import type { FormInstance, FormRules } from "element-plus";
import { authService } from "../services/authService";
import { useSignInCapabilities } from "@/composables/useSignInCapabilities";
import {
  extractApiErrorMessage,
  translateIfPhraseKey,
} from "@/utils/errorHandler";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const { officers, loaded, load } = useSignInCapabilities();

function queryValue(value: unknown): string {
  if (Array.isArray(value)) {
    return typeof value[0] === "string" ? value[0] : "";
  }
  return typeof value === "string" ? value : "";
}

const email = queryValue(route.query.email).trim();
const token = queryValue(route.query.token);

const formRef = ref<FormInstance>();
const loading = ref(false);
const phase = ref<"form" | "success" | "invalid">(
  email && token ? "form" : "invalid",
);
const invalidMessage = ref(
  email && token ? "" : t("auth.resetPasswordPage.missing"),
);
const formError = ref("");

const form = reactive({
  password: "",
  confirmPassword: "",
});

function passwordProblems(value: string): string[] {
  const errors: string[] = [];
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
  return errors;
}

const rules = computed<FormRules>(() => ({
  password: [
    {
      required: true,
      validator: (_rule, value: string, callback) => {
        if (!value) {
          callback(new Error(t("auth.passwordRequired")));
          return;
        }
        const errors = passwordProblems(value);
        if (errors.length > 0) {
          callback(new Error(errors.join(" ")));
        } else {
          callback();
        }
      },
      trigger: "blur",
    },
  ],
  confirmPassword: [
    {
      required: true,
      validator: (_rule, value: string, callback) => {
        if (!value) {
          callback(new Error(t("auth.confirmPasswordRequired")));
        } else if (value !== form.password) {
          callback(new Error(t("auth.passwordMismatch")));
        } else {
          callback();
        }
      },
      trigger: "blur",
    },
  ],
}));

onMounted(() => {
  void load();
});

watch([loaded, officers], () => {
  if (
    phase.value === "success" ||
    !loaded.value ||
    officers.value?.local === true
  ) {
    return;
  }
  phase.value = "invalid";
  invalidMessage.value = t("auth.errors.signInMethodDisabled");
});

function describeError(error: unknown): string[] {
  return extractApiErrorMessage(error)
    .split(/\r?\n/)
    .map((part) => part.trim())
    .filter(Boolean)
    .map((part) => translateIfPhraseKey(part));
}

async function submit() {
  if (phase.value !== "form") {
    return;
  }

  formError.value = "";
  if (!form.password) {
    formError.value = t("auth.passwordRequired");
    await formRef.value?.validate().catch(() => undefined);
    return;
  }
  const problems = passwordProblems(form.password);
  if (problems.length > 0) {
    formError.value = problems.join(" ");
    await formRef.value?.validate().catch(() => undefined);
    return;
  }
  if (!form.confirmPassword) {
    formError.value = t("auth.confirmPasswordRequired");
    await formRef.value?.validate().catch(() => undefined);
    return;
  }
  if (form.confirmPassword !== form.password) {
    formError.value = t("auth.passwordMismatch");
    await formRef.value?.validate().catch(() => undefined);
    return;
  }

  loading.value = true;
  try {
    await authService.resetPassword(
      email,
      token,
      form.password,
      form.confirmPassword,
    );
    form.password = "";
    form.confirmPassword = "";
    phase.value = "success";
    const base = import.meta.env.BASE_URL ?? "/";
    const normalized = base.endsWith("/") ? base : `${base}/`;
    window.history.replaceState(
      window.history.state,
      "",
      `${normalized}reset-password`,
    );
  } catch (error) {
    const raw = extractApiErrorMessage(error);
    const parts = raw
      .split(/\r?\n/)
      .map((part) => part.trim())
      .filter(Boolean);
    if (
      parts.includes("auth.errors.invalidResetToken") ||
      parts.includes("auth.errors.signInMethodDisabled")
    ) {
      phase.value = "invalid";
      invalidMessage.value = describeError(error).join(" ");
      return;
    }
    formError.value = describeError(error).join(" ");
  } finally {
    loading.value = false;
  }
}

function goToLogin() {
  void router.push("/login");
}
</script>

<template>
  <div class="reset-password-page">
    <div class="reset-password-wrapper">
      <el-card class="reset-password-card">
        <h1>{{ t("auth.resetPasswordPage.title") }}</h1>

        <div v-if="phase === 'success'" class="reset-password-status">
          <p role="status" data-testid="reset-password-success">
            {{ t("auth.resetPasswordPage.success") }}
          </p>
          <el-button
            type="primary"
            data-testid="reset-password-login"
            @click="goToLogin"
          >
            {{ t("auth.forgotPasswordPage.backToLogin") }}
          </el-button>
        </div>

        <div v-else-if="phase === 'invalid'" class="reset-password-status">
          <p role="alert" data-testid="reset-password-invalid">
            {{ invalidMessage }}
          </p>
          <p class="reset-password-actions">
            <router-link to="/forgot-password">
              {{ t("auth.resetPasswordPage.requestAnother") }}
            </router-link>
            <el-button
              type="primary"
              data-testid="reset-password-login"
              @click="goToLogin"
            >
              {{ t("auth.forgotPasswordPage.backToLogin") }}
            </el-button>
          </p>
        </div>

        <template v-else>
          <p class="reset-password-note">
            {{ t("auth.resetPasswordPage.intro", { email }) }}
          </p>

          <el-form
            ref="formRef"
            :model="form"
            :rules="rules"
            label-position="top"
            @submit.prevent="submit"
          >
            <el-form-item :label="t('auth.password')" prop="password">
              <el-input
                v-model="form.password"
                type="password"
                autocomplete="new-password"
                show-password
                :placeholder="t('auth.passwordPlaceholder')"
                data-testid="reset-password-new"
              />
            </el-form-item>

            <el-form-item
              :label="t('auth.confirmPassword')"
              prop="confirmPassword"
            >
              <el-input
                v-model="form.confirmPassword"
                type="password"
                autocomplete="new-password"
                show-password
                :placeholder="t('auth.confirmPasswordPlaceholder')"
                data-testid="reset-password-confirm"
              />
            </el-form-item>

            <p
              v-if="formError"
              class="reset-password-error"
              role="alert"
              data-testid="reset-password-error"
            >
              {{ formError }}
            </p>

            <el-button
              native-type="submit"
              type="primary"
              class="submit-btn"
              :loading="loading"
              data-testid="reset-password-submit"
            >
              {{ t("auth.resetPasswordPage.submit") }}
            </el-button>
          </el-form>
        </template>
      </el-card>
    </div>
  </div>
</template>

<style lang="less">
.reset-password-page {
  display: flex;
  justify-content: center;
  padding: 40px 16px;

  .reset-password-wrapper {
    width: 100%;
    max-width: 440px;
  }

  .reset-password-card {
    border-radius: 12px;

    h1 {
      margin: 0 0 12px;
      font-size: 1.5rem;
      text-align: center;
    }
  }

  .reset-password-note,
  .reset-password-status p,
  .reset-password-error {
    margin: 0 0 16px;
    line-height: 1.45;
  }

  .reset-password-actions {
    display: flex;
    flex-direction: column;
    gap: 12px;
    align-items: center;

    a {
      color: var(--color-text-primary);
      text-decoration: underline;
    }
  }

  .submit-btn {
    width: 100%;
  }
}
</style>
