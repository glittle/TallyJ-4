<script setup lang="ts">
import { ref, reactive } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import { useNotifications } from '@/composables/useNotifications';
import type { FormInstance, FormRules } from "element-plus";

const { t } = useI18n();
const router = useRouter();
const authStore = useAuthStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const registerFormRef = ref<FormInstance>();
const loading = ref(false);

const registerForm = reactive({
  email: "",
  password: "",
  confirmPassword: "",
});

const validatePassword = (rule: any, value: any, callback: any) => {
  if (value === "") {
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

const validatePass2 = (rule: any, value: any, callback: any) => {
  if (value === "") {
    callback(new Error(t("auth.confirmPasswordRequired")));
  } else if (value !== registerForm.password) {
    callback(new Error(t("auth.passwordMismatch")));
  } else {
    callback();
  }
};

const rules = reactive<FormRules>({
  email: [
    { required: true, message: t("auth.emailRequired"), trigger: "blur" },
    { type: "email", message: t("auth.emailInvalid"), trigger: "blur" },
  ],
  password: [
    { validator: validatePassword, trigger: "blur" },
  ],
  confirmPassword: [
    { validator: validatePass2, trigger: "blur" },
  ],
});

const handleRegister = async () => {
  if (!registerFormRef.value) return;

  await registerFormRef.value.validate(async (valid) => {
    if (valid) {
      loading.value = true;
      try {
        await authStore.register({
          email: registerForm.email,
          password: registerForm.password,
          confirmPassword: registerForm.confirmPassword,
        });
        showSuccessMessage(t("auth.registerSuccess"));
        router.push("/dashboard");
      } catch (error) {
        console.error("Registration failed:", error);
        showErrorMessage(t("auth.registerFailed"));
      } finally {
        loading.value = false;
      }
    }
  });
};
</script>

<template>
  <div class="register-page">
    <el-card class="register-card">
      <template #header>
        <div class="register-header">
          <h2>{{ t("auth.register") }}</h2>
        </div>
      </template>

      <el-form ref="registerFormRef" :model="registerForm" :rules="rules" label-position="top"
        @keyup.enter="handleRegister">
        <el-form-item :label="t('auth.email')" prop="email">
          <el-input v-model="registerForm.email" :placeholder="t('auth.emailPlaceholder')" />
        </el-form-item>

        <el-form-item :label="t('auth.password')" prop="password">
          <el-input v-model="registerForm.password" type="password" :placeholder="t('auth.passwordPlaceholder')"
            show-password />
        </el-form-item>

        <el-form-item :label="t('auth.confirmPassword')" prop="confirmPassword">
          <el-input v-model="registerForm.confirmPassword" type="password"
            :placeholder="t('auth.confirmPasswordPlaceholder')" show-password />
        </el-form-item>

        <div class="register-actions">
          <el-button type="primary" :loading="loading" class="submit-btn" @click="handleRegister">
            {{ t("auth.registerButton") }}
          </el-button>
        </div>

        <div class="auth-links">
          <router-link to="/login">
            {{ t("auth.hasAccount") }}
          </router-link>
        </div>
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

  .register-actions {
    margin-top: 30px;
  }

  .submit-btn {
    width: 100%;
  }

  .auth-links {
    margin-top: 20px;
    text-align: center;
  }

  .auth-links a {
    color: #b0caff;
    text-decoration: none;
    font-size: 0.9rem;
  }

  .auth-links a:hover {
    text-decoration: underline;
  }
}
</style>
