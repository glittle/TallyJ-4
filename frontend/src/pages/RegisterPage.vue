<template>
  <div class="register-page">
    <div class="register-container">
      <h1>{{ $t('auth.register') }}</h1>
      
      <el-form
        :model="registerForm"
        :rules="rules"
        ref="formRef"
        label-position="top"
        @submit.prevent="handleRegister"
      >
        <el-form-item :label="$t('auth.email')" prop="email">
          <el-input
            v-model="registerForm.email"
            type="email"
            :placeholder="$t('auth.emailPlaceholder')"
          />
        </el-form-item>

        <el-form-item :label="$t('auth.password')" prop="password">
          <el-input
            v-model="registerForm.password"
            type="password"
            :placeholder="$t('auth.passwordPlaceholder')"
            show-password
          />
        </el-form-item>

        <el-form-item :label="$t('auth.confirmPassword')" prop="confirmPassword">
          <el-input
            v-model="registerForm.confirmPassword"
            type="password"
            :placeholder="$t('auth.confirmPasswordPlaceholder')"
            show-password
          />
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
import { useAuthStore } from '../stores/authStore';
import { useI18n } from 'vue-i18n';

const { t } = useI18n();
const router = useRouter();
const authStore = useAuthStore();

const formRef = ref<FormInstance>();
const loading = ref(false);

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

async function handleRegister() {
  if (!formRef.value) return;
  
  await formRef.value.validate(async (valid) => {
    if (!valid) return;

    loading.value = true;
    try {
      await authStore.register(registerForm);
      ElMessage.success(t('auth.registerSuccess'));
      router.push('/');
    } catch (error: any) {
      ElMessage.error(error.response?.data?.error || t('auth.registerFailed'));
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
  background: #f5f7fa;
}

.register-container {
  width: 100%;
  max-width: 400px;
  padding: 40px;
  background: white;
  border-radius: 8px;
  box-shadow: 0 2px 12px rgba(0, 0, 0, 0.1);
}

.register-container h1 {
  margin-bottom: 30px;
  text-align: center;
  color: #303133;
}

.links {
  display: flex;
  justify-content: center;
  margin-top: 20px;
}

.links a {
  color: #409eff;
  text-decoration: none;
}

.links a:hover {
  text-decoration: underline;
}
</style>
