<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRouter, useRoute } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import { ElMessage } from "element-plus";
import { useI18n } from "vue-i18n";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const error = ref<string | null>(null);

onMounted(async () => {
  try {
    const token = route.query.token as string;
    const errorParam = route.query.error as string;
    
    if (errorParam) {
      error.value = errorParam;
      ElMessage.error(t("auth.googleLoginFailed"));
      setTimeout(() => {
        router.push("/login?mode=officer");
      }, 2000);
      return;
    }
    
    if (!token) {
      error.value = "No token received";
      ElMessage.error(t("auth.googleLoginFailed"));
      setTimeout(() => {
        router.push("/login?mode=officer");
      }, 2000);
      return;
    }
    
    authStore.token = token;
    authStore.email = route.query.email as string || null;
    localStorage.setItem('auth_token', token);
    if (authStore.email) {
      localStorage.setItem('user_email', authStore.email);
    }
    
    ElMessage.success(t("auth.loginSuccess"));
    
    const redirectPath = (route.query.redirect as string) || "/dashboard";
    router.push(redirectPath);
  } catch (err) {
    console.error("Google callback error:", err);
    error.value = err instanceof Error ? err.message : "Unknown error";
    ElMessage.error(t("auth.googleLoginFailed"));
    setTimeout(() => {
      router.push("/login?mode=officer");
    }, 2000);
  }
});
</script>

<template>
  <div class="callback-page">
    <el-card class="callback-card">
      <div v-if="!error" class="loading-container">
        <el-icon class="is-loading" :size="40">
          <svg viewBox="0 0 1024 1024" xmlns="http://www.w3.org/2000/svg">
            <path fill="currentColor" d="M512 64a32 32 0 0 1 32 32v192a32 32 0 0 1-64 0V96a32 32 0 0 1 32-32zm0 640a32 32 0 0 1 32 32v192a32 32 0 1 1-64 0V736a32 32 0 0 1 32-32zm448-192a32 32 0 0 1-32 32H736a32 32 0 1 1 0-64h192a32 32 0 0 1 32 32zm-640 0a32 32 0 0 1-32 32H96a32 32 0 0 1 0-64h192a32 32 0 0 1 32 32zM195.2 195.2a32 32 0 0 1 45.248 0L376.32 331.008a32 32 0 0 1-45.248 45.248L195.2 240.448a32 32 0 0 1 0-45.248zm452.544 452.544a32 32 0 0 1 45.248 0L828.8 783.552a32 32 0 0 1-45.248 45.248L647.744 692.992a32 32 0 0 1 0-45.248zM828.8 195.264a32 32 0 0 1 0 45.184L692.992 376.32a32 32 0 0 1-45.248-45.248l135.808-135.808a32 32 0 0 1 45.248 0zm-452.544 452.48a32 32 0 0 1 0 45.248L240.448 828.8a32 32 0 0 1-45.248-45.248l135.808-135.808a32 32 0 0 1 45.248 0z"></path>
          </svg>
        </el-icon>
        <p>{{ t("auth.processingLogin") }}</p>
      </div>
      <div v-else class="error-container">
        <el-icon :size="40" color="var(--el-color-danger)">
          <svg viewBox="0 0 1024 1024" xmlns="http://www.w3.org/2000/svg">
            <path fill="currentColor" d="M512 64a448 448 0 1 1 0 896 448 448 0 0 1 0-896zm0 393.664L407.936 353.6a38.4 38.4 0 1 0-54.336 54.336L457.664 512 353.6 616.064a38.4 38.4 0 1 0 54.336 54.336L512 566.336 616.064 670.4a38.4 38.4 0 1 0 54.336-54.336L566.336 512 670.4 407.936a38.4 38.4 0 1 0-54.336-54.336L512 457.664z"></path>
          </svg>
        </el-icon>
        <p>{{ t("auth.googleLoginFailed") }}</p>
        <p class="error-detail">{{ error }}</p>
      </div>
    </el-card>
  </div>
</template>

<style scoped lang="less">
.callback-page {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  padding: 20px;
}

.callback-card {
  width: 100%;
  max-width: 400px;
  text-align: center;
}

.loading-container,
.error-container {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 20px;
  padding: 40px 20px;
}

.loading-container p,
.error-container p {
  margin: 0;
  color: var(--color-text-primary);
  font-size: 1.1rem;
}

.error-detail {
  font-size: 0.9rem;
  color: var(--color-text-secondary);
}

.is-loading {
  animation: rotating 2s linear infinite;
}

@keyframes rotating {
  0% {
    transform: rotate(0deg);
  }
  100% {
    transform: rotate(360deg);
  }
}
</style>
