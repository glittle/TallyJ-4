<template>
  <div class="register-page">
    <div class="register-container">
      <!-- Loading skeleton for initial load -->
      <el-skeleton
        v-if="initialLoading"
        :loading="initialLoading"
        animated
        :count="4"
        :rows="4"
        class="register-skeleton"
      />

      <div v-else>
        <h1 class="register-title">{{ $t('auth.register') }}</h1>

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

        <el-form
          :model="registerForm"
          :rules="rules"
          ref="formRef"
          label-position="top"
          @submit.prevent="handleRegister"
          class="register-form"
        >
          <el-form-item :label="$t('auth.email')" prop="email" class="form-item">
            <el-input
              v-model="registerForm.email"
              type="email"
              :placeholder="$t('auth.emailPlaceholder')"
              aria-describedby="email-error"
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
              v-model="registerForm.password"
              type="password"
              :placeholder="$t('auth.passwordPlaceholder')"
              show-password
              aria-describedby="password-error"
              autocomplete="new-password"
              :prefix-icon="Lock"
              size="large"
              @input="clearFieldError('password')"
            />
            <template #error="{ error }">
              <span id="password-error" class="error-message" role="alert">{{ error }}</span>
            </template>
          </el-form-item>

          <el-form-item :label="$t('auth.confirmPassword')" prop="confirmPassword" class="form-item">
            <el-input
              v-model="registerForm.confirmPassword"
              type="password"
              :placeholder="$t('auth.confirmPasswordPlaceholder')"
              show-password
              aria-describedby="confirm-password-error"
              autocomplete="new-password"
              :prefix-icon="Lock"
              size="large"
              @input="clearFieldError('confirmPassword')"
            />
            <template #error="{ error }">
              <span id="confirm-password-error" class="error-message" role="alert">{{ error }}</span>
            </template>
          </el-form-item>

        <el-form-item>
          <el-button
            type="primary"
            native-type="submit"
            :loading="loading"
            style="width: 100%"
          >
            {{ $t('auth.registerButton') }}
          </el-button>
        </el-form-item>

        <div class="links">
          <router-link to="/login">{{ $t('auth.hasAccount') }}</router-link>
        </div>
      </el-form>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive } from 'vue';
import { useRouter } from 'vue-router';
import { ElMessage, type FormInstance, type FormRules } from 'element-plus';
import { Message, Lock } from '@element-plus/icons-vue';
import { useAuthStore } from '../stores/authStore';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();
const router = useRouter();
const authStore = useAuthStore();

const formRef = ref<FormInstance>();
const loading = ref(false);
const initialLoading = ref(false);
const generalError = ref<string | null>(null);

const registerForm = reactive({
  email: '',
  password: '',
  confirmPassword: ''
});

const validatePasswordMatch = (_rule: any, value: any, callback: any) => {
  if (value !== registerForm.password) {
    callback(new Error(t('auth.passwordMismatch')));
  } else {
    callback();
  }
};

const rules: FormRules = {
  email: [
    { required: true, message: t('auth.emailRequired'), trigger: 'blur' },
    { type: 'email', message: t('auth.emailInvalid'), trigger: 'blur' }
  ],
  password: [
    { required: true, message: t('auth.passwordRequired'), trigger: 'blur' },
    { min: 6, message: t('auth.passwordMinLength'), trigger: 'blur' }
  ],
  confirmPassword: [
    { required: true, message: t('auth.confirmPasswordRequired'), trigger: 'blur' },
    { validator: validatePasswordMatch, trigger: 'blur' }
  ]
};

function clearFieldError(field: string) {
  if (generalError.value) {
    generalError.value = null;
  }
  // Clear form validation errors
  if (formRef.value) {
    formRef.value.clearValidate(field);
  }
}

async function handleRegister() {
  if (!formRef.value) return;

  generalError.value = null;

  await formRef.value.validate(async (valid) => {
    if (!valid) return;

    loading.value = true;
    try {
      await authStore.register(registerForm);
      ElMessage.success(t('auth.registerSuccess'));
      router.push('/');
    } catch (error: any) {
      generalError.value = error.response?.data?.error || t('auth.registerFailed');
    } finally {
      loading.value = false;
    }
  });
}
</script>

<style scoped>
.register-page {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: var(--color-bg-secondary);
  padding: var(--spacing-4);
}

.register-container {
  width: 100%;
  max-width: 28rem; /* 448px */
  padding: var(--spacing-8);
  background: var(--color-bg-primary);
  border-radius: var(--radius-xl);
  box-shadow: var(--shadow-xl);
  border: 1px solid var(--color-gray-200);
}

.register-skeleton {
  padding: var(--spacing-8);
}

.register-title {
  margin-bottom: var(--spacing-8);
  text-align: center;
  color: var(--color-text-primary);
  font-size: var(--font-size-3xl);
  font-weight: var(--font-weight-bold);
  line-height: var(--line-height-tight);
}

.register-form {
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
  justify-content: center;
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
  .register-page {
    padding: var(--spacing-5);
  }

  .register-container {
    padding: var(--spacing-6);
    max-width: 100%;
  }

  .register-title {
    font-size: var(--font-size-2xl);
    margin-bottom: var(--spacing-6);
  }

  .register-form .form-item {
    margin-bottom: var(--spacing-5);
  }
}

@media (max-width: 480px) {
  .register-page {
    padding: var(--spacing-3);
  }

  .register-container {
    padding: var(--spacing-5) var(--spacing-4);
  }

  .register-title {
    font-size: var(--font-size-xl);
    margin-bottom: var(--spacing-5);
  }
}

/* Focus and accessibility improvements */
.register-container :deep(.el-input__inner:focus) {
  border-color: var(--color-primary-500);
  box-shadow: 0 0 0 2px rgba(59, 130, 246, 0.1);
}

.register-container :deep(.el-button) {
  height: 3rem;
  font-size: var(--font-size-base);
  font-weight: var(--font-weight-medium);
  border-radius: var(--radius-md);
  transition: var(--transition-normal);
}

.register-container :deep(.el-button:hover) {
  transform: translateY(-1px);
  box-shadow: var(--shadow-md);
}

/* Loading state improvements */
.register-container :deep(.el-button.is-loading) {
  transform: none;
}

/* Alert styling */
.register-container :deep(.el-alert) {
  border-radius: var(--radius-md);
  border: none;
}

.register-container :deep(.el-alert--error) {
  background-color: var(--color-error-50);
  color: var(--color-error-700);
}
</style>
