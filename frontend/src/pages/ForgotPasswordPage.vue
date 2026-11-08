<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { useI18n } from "vue-i18n";
import type { FormInstance, FormRules } from "element-plus";
import { authService } from "../services/authService";
import { useSignInCapabilities } from "@/composables/useSignInCapabilities";
import { resolveUserFacingApiError } from "@/utils/errorHandler";

const { t } = useI18n();
const { officers, loaded, load } = useSignInCapabilities();

const formRef = ref<FormInstance>();
const loading = ref(false);
const sent = ref(false);
const formError = ref("");

const form = reactive({
  email: "",
});

const localOff = computed(() => loaded.value && officers.value?.local !== true);

const rules = computed<FormRules>(() => ({
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
}));

onMounted(() => {
  void load();
});

function emailProblem(value: string): string | null {
  const email = value.trim();
  if (!email) {
    return t("auth.emailRequired");
  }
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
    return t("auth.emailInvalid");
  }
  return null;
}

async function submit() {
  if (localOff.value) {
    return;
  }

  formError.value = "";
  const problem = emailProblem(form.email);
  if (problem) {
    formError.value = problem;
    await formRef.value?.validate().catch(() => undefined);
    return;
  }

  loading.value = true;
  try {
    await authService.forgotPassword(form.email.trim());
    sent.value = true;
  } catch (error) {
    sent.value = false;
    formError.value = resolveUserFacingApiError(
      error,
      t("auth.forgotPasswordPage.unavailable"),
    );
  } finally {
    loading.value = false;
  }
}
</script>

<template>
  <div class="forgot-password-page">
    <div class="forgot-password-wrapper">
      <el-card class="forgot-password-card">
        <h1>{{ t("auth.forgotPasswordPage.title") }}</h1>

        <p
          v-if="localOff"
          class="forgot-password-note"
          role="status"
          data-testid="forgot-password-disabled"
        >
          {{ t("auth.errors.signInMethodDisabled") }}
        </p>

        <template v-else>
          <p class="forgot-password-note">
            {{ t("auth.forgotPasswordPage.intro") }}
          </p>

          <p
            v-if="sent"
            class="forgot-password-note"
            role="status"
            aria-live="polite"
            data-testid="forgot-password-sent"
          >
            {{ t("auth.forgotPasswordPage.sent") }}
          </p>

          <el-form
            v-else
            ref="formRef"
            :model="form"
            :rules="rules"
            label-position="top"
            @submit.prevent="submit"
          >
            <el-form-item :label="t('auth.email')" prop="email">
              <el-input
                v-model="form.email"
                type="email"
                autocomplete="username"
                :placeholder="t('auth.emailPlaceholder')"
                data-testid="forgot-password-email"
              />
            </el-form-item>

            <p
              v-if="formError"
              class="forgot-password-error"
              role="alert"
              data-testid="forgot-password-error"
            >
              {{ formError }}
            </p>

            <el-button
              native-type="submit"
              type="primary"
              class="submit-btn"
              :loading="loading"
              data-testid="forgot-password-submit"
            >
              {{ t("auth.forgotPasswordPage.submit") }}
            </el-button>
          </el-form>
        </template>

        <p class="forgot-password-back">
          <router-link to="/login" data-testid="forgot-password-login">
            {{ t("auth.forgotPasswordPage.backToLogin") }}
          </router-link>
        </p>
      </el-card>
    </div>
  </div>
</template>

<style lang="less">
.forgot-password-page {
  display: flex;
  justify-content: center;
  padding: 40px 16px;

  .forgot-password-wrapper {
    width: 100%;
    max-width: 440px;
  }

  .forgot-password-card {
    border-radius: 12px;

    h1 {
      margin: 0 0 12px;
      font-size: 1.5rem;
      text-align: center;
    }
  }

  .forgot-password-note {
    margin: 0 0 16px;
    line-height: 1.45;
  }

  .forgot-password-error {
    margin: 0 0 12px;
    line-height: 1.45;
  }

  .submit-btn {
    width: 100%;
  }

  .forgot-password-back {
    margin: 16px 0 0;
    text-align: center;

    a {
      color: var(--color-text-primary);
      text-decoration: underline;
    }
  }
}
</style>
