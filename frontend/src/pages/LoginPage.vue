<template>
  <main class="login-page">
    <div class="login-container">
      <h1 id="login-heading">{{ $t('auth.login') }}</h1>

      <form
        v-if="!requires2FA"
        :model="loginForm"
        :rules="rules"
        ref="formRef"
        role="form"
        aria-labelledby="login-heading"
        @submit.prevent="handleLogin"
      >
        <el-form-item :label="$t('auth.email')" prop="email">
          <el-input
            v-model="loginForm.email"
            type="email"
            :placeholder="$t('auth.emailPlaceholder')"
            aria-describedby="email-help"
            autocomplete="email"
          />
        </el-form-item>

        <el-form-item :label="$t('auth.password')" prop="password">
          <el-input
            v-model="loginForm.password"
            type="password"
            :placeholder="$t('auth.passwordPlaceholder')"
            show-password
            aria-describedby="password-help"
            autocomplete="current-password"
          />
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
      >
        <el-alert
          :title="$t('auth.twoFactorRequired')"
          type="info"
          :closable="false"
          style="margin-bottom: 20px"
          role="alert"
        />

        <el-form-item :label="$t('auth.twoFactorCode')" prop="code">
          <el-input
            v-model="twoFactorForm.code"
            maxlength="6"
            :placeholder="$t('auth.twoFactorCodePlaceholder')"
            aria-describedby="twofa-help"
            autocomplete="one-time-code"
            inputmode="numeric"
          />
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
      // Error is handled by the store
    } finally {
      loading.value = false;
    }
  });
}

async function handleVerify2FA() {
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
    // Error is handled by the store
  } finally {
    loading.value = false;
  }
}

function cancel2FA() {
  requires2FA.value = false;
  twoFactorForm.code = '';
}
</script>

<style scoped>
.login-page {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  background: #f5f7fa;
}

.login-container {
  width: 100%;
  max-width: 400px;
  padding: 40px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}

.login-container h1 {
  margin-bottom: 30px;
  text-align: center;
  color: #303133;
}

.links {
  display: flex;
  justify-content: space-between;
  margin-top: 20px;
}

.links a {
  color: #409eff;
  text-decoration: none;
}

.links a:hover {
  text-decoration: underline;
}

/* Mobile responsiveness */
@media (max-width: 768px) {
  .login-page {
    padding: 20px;
  }

  .login-container {
    width: 100%;
    max-width: 400px;
    padding: 30px 20px;
  }

  .login-container h1 {
    font-size: 1.8rem;
    margin-bottom: 25px;
  }
}

@media (max-width: 480px) {
  .login-page {
    padding: 10px;
  }

  .login-container {
    padding: 25px 15px;
  }

  .login-container h1 {
    font-size: 1.6rem;
    margin-bottom: 20px;
  }

  .links {
    flex-direction: column;
    gap: 10px;
    align-items: center;
  }
}
</style>
