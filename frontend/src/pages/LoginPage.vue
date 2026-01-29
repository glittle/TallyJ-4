<template>
  <main class="login-page">
    <div class="login-container">
      <!-- Loading skeleton for initial load -->
      <el-skeleton
        v-if="initialLoading"
        :loading="initialLoading"
        animated
        :count="3"
        :rows="4"
        class="login-skeleton"
      />

      <div v-else>
        <h1 id="login-heading" class="login-title">{{ $t('auth.login') }}</h1>

        <!-- Error alert for general errors -->
        <el-alert
          v-if="generalError"
          :title="$t('common.error')"
          :description="generalError"
          type="error"
          :closable="false"
          show-icon
          class="mb-4"
          role="alert"
        />

        <form
          v-if="!requires2FA"
          :model="loginForm"
          :rules="rules"
          ref="formRef"
          role="form"
          aria-labelledby="login-heading"
          @submit.prevent="handleLogin"
          class="login-form"
        >
        <el-form-item :label="$t('auth.email')" prop="email" class="form-item">
          <el-input
            v-model="loginForm.email"
            type="email"
            :placeholder="$t('auth.emailPlaceholder')"
            aria-describedby="email-help email-error"
            autocomplete="email"
            :prefix-icon="Message"
            size="large"
            clearable
            @input="clearFieldError('email')"
          />
          <template #error="{ error }">
            <span id="email-error" class="error-message" role="alert">{{ error }}</span>
          </template>
        </el-form-item>

        <el-form-item :label="$t('auth.password')" prop="password" class="form-item">
          <el-input
            v-model="loginForm.password"
            type="password"
            :placeholder="$t('auth.passwordPlaceholder')"
            show-password
            aria-describedby="password-help password-error"
            autocomplete="current-password"
            :prefix-icon="Lock"
            size="large"
            @input="clearFieldError('password')"
          />
          <template #error="{ error }">
            <span id="password-error" class="error-message" role="alert">{{ error }}</span>
          </template>
        </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            native-type="submit"
            :loading="loading"
            style="width: 100%"
            :aria-label="loading ? 'Logging in...' : 'Login to your account'"
          >
            {{ $t('auth.loginButton') }}
          </el-button>
        </el-form-item>

        <nav class="links" aria-label="Account links">
          <router-link to="/register">{{ $t('auth.noAccount') }}</router-link>
          <router-link to="/forgot-password">{{ $t('auth.forgotPassword') }}</router-link>
        </nav>
      </form>

        <form
          v-else
          :model="twoFactorForm"
          ref="twoFactorFormRef"
          role="form"
          aria-labelledby="twofa-heading"
          @submit.prevent="handleVerify2FA"
          class="login-form"
        >
          <el-alert
            :title="$t('auth.twoFactorRequired')"
            :description="$t('auth.twoFactorDescription')"
            type="info"
            :closable="false"
            show-icon
            class="mb-4"
            role="alert"
          />

          <el-form-item :label="$t('auth.twoFactorCode')" prop="code" class="form-item">
            <el-input
              v-model="twoFactorForm.code"
              maxlength="6"
              :placeholder="$t('auth.twoFactorCodePlaceholder')"
              aria-describedby="twofa-help twofa-error"
              autocomplete="one-time-code"
              inputmode="numeric"
              :prefix-icon="Key"
              size="large"
              @input="clearFieldError('code')"
            />
            <template #error="{ error }">
              <span id="twofa-error" class="error-message" role="alert">{{ error }}</span>
            </template>
          </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            native-type="submit"
            :loading="loading"
            style="width: 100%"
            :aria-label="loading ? 'Verifying code...' : 'Verify two-factor authentication code'"
          >
            {{ $t('auth.verify') }}
          </el-button>
        </el-form-item>

        <el-button @click="cancel2FA" text aria-label="Cancel two-factor authentication">{{ $t('common.cancel') }}</el-button>
      </form>
    </div>
  </main>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { type FormInstance, type FormRules } from 'element-plus';
import { Message, Lock, Key } from '@element-plus/icons-vue';
import { useAuthStore } from '../stores/authStore';
import { useI18n } from 'vue-i18n';
import { useNotifications } from '../composables/useNotifications';

const { t } = useI18n();
const router = useRouter();
const authStore = useAuthStore();
const { successMessage } = useNotifications();

const formRef = ref<FormInstance>();
const twoFactorFormRef = ref<FormInstance>();
const loading = ref(false);
const requires2FA = ref(false);
const initialLoading = ref(false);
const generalError = ref<string | null>(null);

const loginForm = reactive({
  email: '',
  password: ''
});

const twoFactorForm = reactive({
  code: ''
});

const rules: FormRules = {
  email: [
    { required: true, message: t('auth.emailRequired'), trigger: 'blur' },
    { type: 'email', message: t('auth.emailInvalid'), trigger: 'blur' }
  ],
  password: [
    { required: true, message: t('auth.passwordRequired'), trigger: 'blur' }
  ]
};

async function handleLogin() {
  if (!formRef.value) return;

  generalError.value = null;

  await formRef.value.validate(async (valid) => {
    if (!valid) return;

    loading.value = true;
    try {
      const response = await authStore.login(loginForm);

      if (response.requires2FA) {
        requires2FA.value = true;
      } else {
        successMessage(t('auth.loginSuccess'));
        router.push('/');
      }
    } catch (error: any) {
      generalError.value = error.response?.data?.error || t('auth.loginFailed');
    } finally {
      loading.value = false;
    }
  });
}

async function handleVerify2FA() {
  if (!twoFactorFormRef.value) return;

  generalError.value = null;

  await twoFactorFormRef.value.validate(async (valid) => {
    if (!valid) return;

    loading.value = true;
    try {
      await authStore.login({
        email: loginForm.email,
        password: loginForm.password,
        twoFactorCode: twoFactorForm.code
      });

      successMessage(t('auth.loginSuccess'));
      router.push('/');
    } catch (error: any) {
      generalError.value = error.response?.data?.error || t('auth.twoFactorFailed');
    } finally {
      loading.value = false;
    }
  });
}

function cancel2FA() {
  requires2FA.value = false;
  twoFactorForm.code = '';
  generalError.value = null;
}

function clearFieldError(field: string) {
  if (generalError.value) {
    generalError.value = null;
  }
  // Clear form validation errors
  if (formRef.value) {
    formRef.value.clearValidate(field);
  }
  if (twoFactorFormRef.value) {
    twoFactorFormRef.value.clearValidate(field);
  }
}
</script>

<style scoped>
.login-page {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: var(--color-bg-secondary);
  padding: var(--spacing-4);
}

.login-container {
  width: 100%;
  max-width: 28rem; /* 448px */
  padding: var(--spacing-8);
  background: var(--color-bg-primary);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-xl);
  border: 1px solid var(--color-gray-200);
}

.login-skeleton {
  padding: var(--spacing-8);
}

.login-title {
  margin-bottom: var(--spacing-8);
  text-align: center;
  color: var(--color-text-primary);
  font-size: var(--font-size-3xl);
  font-weight: var(--font-weight-bold);
  line-height: var(--line-height-tight);
}

.login-form {
  .form-item {
    margin-bottom: var(--spacing-6);
  }

  .el-form-item__label {
    font-weight: var(--font-weight-medium);
    color: var(--color-text-primary);
    margin-bottom: var(--spacing-2);
    font-size: var(--font-size-base);
  }

  .error-message {
    color: var(--color-error-600);
    font-size: var(--font-size-sm);
    margin-top: var(--spacing-1);
    display: block;
  }
}

.links {
  display: flex;
  justify-content: space-between;
  margin-top: var(--spacing-6);
  padding-top: var(--spacing-4);
  border-top: 1px solid var(--color-gray-200);
}

.links a {
  color: var(--color-primary-600);
  text-decoration: none;
  font-weight: var(--font-weight-medium);
  font-size: var(--font-size-sm);
  transition: var(--transition-fast);
}

.links a:hover {
  color: var(--color-primary-700);
  text-decoration: underline;
}

/* Mobile responsiveness */
@media (max-width: 768px) {
  .login-page {
    padding: var(--spacing-5);
  }

  .login-container {
    padding: var(--spacing-6);
    max-width: 100%;
  }

  .login-title {
    font-size: var(--font-size-2xl);
    margin-bottom: var(--spacing-6);
  }

  .login-form .form-item {
    margin-bottom: var(--spacing-5);
  }
}

@media (max-width: 480px) {
  .login-page {
    padding: var(--spacing-3);
  }

  .login-container {
    padding: var(--spacing-5) var(--spacing-4);
  }

  .login-title {
    font-size: var(--font-size-xl);
    margin-bottom: var(--spacing-5);
  }

  .links {
    flex-direction: column;
    gap: var(--spacing-3);
    align-items: center;
    margin-top: var(--spacing-5);
  }
}

/* Focus and accessibility improvements */
.login-container :deep(.el-input__inner:focus) {
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.1);
}

.login-container :deep(.el-button) {
  height: 3rem;
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-medium);
  border-radius: var(--radius-md);
  transition: var(--transition-normal);
}

.login-container :deep(.el-button:hover) {
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

/* Loading state improvements */
.login-container :deep(.el-button.is-loading) {
  transform: none;
}

/* Alert styling */
.login-container :deep(.el-alert) {
  border-radius: var(--radius-md);
  border: none;
}

.login-container :deep(.el-alert--error) {
  background-color: var(--color-error-50);
  color: var(--color-error-700);
}

.login-container :deep(.el-alert--info) {
  background-color: var(--color-primary-50);
  color: var(--color-primary-700);
}
</style>
