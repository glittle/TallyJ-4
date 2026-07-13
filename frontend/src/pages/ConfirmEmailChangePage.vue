<script setup lang="ts">
import { onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useI18n } from "vue-i18n";
import { useAuthStore } from "../stores/authStore";
import { extractApiErrorMessage } from "@/utils/errorHandler";

const { t } = useI18n();
const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const status = ref<"loading" | "success" | "error">("loading");
const errorMessage = ref("");

onMounted(async () => {
  const token = String(route.query.token || "");
  if (!token) {
    status.value = "error";
    errorMessage.value = t("auth.confirmEmailChange.missingToken");
    return;
  }

  try {
    await authStore.confirmEmailChange({ token });
    status.value = "success";
  } catch (err) {
    status.value = "error";
    errorMessage.value = extractApiErrorMessage(err);
  }
});
</script>

<template>
  <div class="confirm-email-change-page">
    <el-card class="confirm-card">
      <h2>{{ $t("auth.confirmEmailChange.title") }}</h2>

      <div v-if="status === 'loading'" class="status-block">
        <p>{{ $t("auth.confirmEmailChange.confirming") }}</p>
      </div>

      <el-result
        v-else-if="status === 'success'"
        icon="success"
        :title="$t('auth.confirmEmailChange.successTitle')"
        :sub-title="$t('auth.confirmEmailChange.successBody')"
      >
        <template #extra>
          <el-button type="primary" @click="router.push('/login')">
            {{ $t("auth.login") }}
          </el-button>
        </template>
      </el-result>

      <el-result
        v-else
        icon="error"
        :title="$t('auth.confirmEmailChange.errorTitle')"
        :sub-title="errorMessage"
      >
        <template #extra>
          <el-button type="primary" @click="router.push('/profile')">
            {{ $t("nav.profile") }}
          </el-button>
        </template>
      </el-result>
    </el-card>
  </div>
</template>

<style lang="less">
.confirm-email-change-page {
  display: flex;
  justify-content: center;
  padding: 40px 16px;

  .confirm-card {
    width: 100%;
    max-width: 480px;
    border-radius: 12px;

    h2 {
      margin: 0 0 16px;
      text-align: center;
    }

    .status-block {
      text-align: center;
      padding: 24px 0;
    }
  }
}
</style>
