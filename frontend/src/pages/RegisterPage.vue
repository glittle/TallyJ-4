<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { getAppConfig } from "@/config/appConfig";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const appConfig = getAppConfig();

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
</script>

<template>
  <div class="register-page">
    <el-card class="register-card">
      <template #header>
        <div class="register-header">
          <h2>{{ t("auth.registerClosedTitle") }}</h2>
        </div>
      </template>

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
