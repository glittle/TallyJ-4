<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { authService } from "../services/authService";
import { extractApiErrorMessage } from "@/utils/errorHandler";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();

const status = ref<"loading" | "success" | "error">("loading");
const errorMessage = ref("");

onMounted(async () => {
  const email = String(route.query.email || "");
  const token = String(route.query.token || "");

  if (!email || !token) {
    status.value = "error";
    errorMessage.value = t("auth.verifyEmail.missingParams");
    return;
  }

  try {
    await authService.verifyEmail(email, token);
    status.value = "success";
  } catch (err) {
    status.value = "error";
    errorMessage.value = extractApiErrorMessage(err);
  }
});
</script>

<template>
  <div class="verify-email-page">
    <el-card class="verify-card">
      <h2>{{ $t("auth.verifyEmail.title") }}</h2>

      <div v-if="status === 'loading'" class="status-block">
        <el-icon class="is-loading" :size="32"
          ><i class="ep-loading"
        /></el-icon>
        <p>{{ $t("auth.verifyEmail.verifying") }}</p>
      </div>

      <div v-else-if="status === 'success'" class="status-block">
        <el-result
          icon="success"
          :title="$t('auth.verifyEmail.successTitle')"
          :sub-title="$t('auth.verifyEmail.successBody')"
        >
          <template #extra>
            <el-button type="primary" @click="router.push('/login')">
              {{ $t("auth.login") }}
            </el-button>
          </template>
        </el-result>
      </div>

      <div v-else class="status-block">
        <el-result
          icon="error"
          :title="$t('auth.verifyEmail.errorTitle')"
          :sub-title="errorMessage"
        >
          <template #extra>
            <el-button type="primary" @click="router.push('/login')">
              {{ $t("auth.login") }}
            </el-button>
          </template>
        </el-result>
      </div>
    </el-card>
  </div>
</template>

<style lang="less">
.verify-email-page {
  display: flex;
  justify-content: center;
  padding: 40px 16px;

  .verify-card {
    width: 100%;
    max-width: 480px;
    border-radius: 12px;

    h2 {
      margin: 0 0 16px;
      text-align: center;
    }

    .status-block {
      text-align: center;
    }
  }
}
</style>
