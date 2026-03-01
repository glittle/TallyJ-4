<script setup lang="ts">
import { ref, reactive, onMounted } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useAuthStore } from "../stores/authStore";
import { useNotifications } from '@/composables/useNotifications';
import { authService } from "../services/authService";
import type { FormInstance, FormRules } from "element-plus";

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const authStore = useAuthStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const joinFormRef = ref<FormInstance>();
const loading = ref(false);

const joinForm = reactive({
  electionGuid: "",
  accessCode: "",
});

const rules: FormRules = {
  electionGuid: [
    { required: true, message: t("auth.tellerJoin.electionGuidRequired"), trigger: "blur" },
  ],
  accessCode: [
    { required: true, message: t("auth.tellerJoin.accessCodeRequired"), trigger: "blur" },
  ],
};

const handleJoin = async () => {
  if (!joinFormRef.value) return;

  await joinFormRef.value.validate(async (valid) => {
    if (!valid) return;

    loading.value = true;
    try {
      const result = await authService.tellerLogin(joinForm.electionGuid, joinForm.accessCode);

      showSuccessMessage(t("auth.tellerJoin.joinSuccess"));

      // Redirect to the election dashboard
      router.push(`/elections/${result.electionGuid}`);
    } catch (error) {
      console.error("Teller join failed:", error);
      showErrorMessage(t("auth.tellerJoin.invalidElection"));
    } finally {
      loading.value = false;
    }
  });
};

// Pre-fill form from URL parameters
onMounted(() => {
  const electionGuid = route.params.electionGuid as string;
  const accessCode = route.query.code as string;

  if (electionGuid) {
    joinForm.electionGuid = electionGuid;
  }

  if (accessCode) {
    joinForm.accessCode = accessCode;
  }
});
</script>

<template>
  <div class="teller-join-page">
    <el-card class="join-card">
      <template #header>
        <div class="join-header">
          <h2>{{ t("auth.tellerJoin.title") }}</h2>
          <p class="description">{{ t("auth.tellerJoin.description") }}</p>
        </div>
      </template>

      <el-form
        ref="joinFormRef"
        :model="joinForm"
        :rules="rules"
        label-position="top"
        @keyup.enter="handleJoin"
      >
        <el-form-item
          :label="t('auth.tellerJoin.electionGuidLabel')"
          prop="electionGuid"
        >
          <el-input
            v-model="joinForm.electionGuid"
            :placeholder="t('auth.tellerJoin.electionGuidPlaceholder')"
            autofocus
          />
        </el-form-item>

        <el-form-item
          :label="t('auth.tellerJoin.accessCodeLabel')"
          prop="accessCode"
        >
          <el-input
            v-model="joinForm.accessCode"
            :placeholder="t('auth.tellerJoin.accessCodePlaceholder')"
            type="password"
          />
        </el-form-item>

        <div class="join-actions">
          <el-button
            type="primary"
            :loading="loading"
            class="join-btn"
            @click="handleJoin"
          >
            {{ t("auth.tellerJoin.joinButton") }}
          </el-button>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<style lang="less">
.teller-join-page {
  display: flex;
  justify-content: center;
  align-items: center;
  min-height: 100vh;
  padding: 20px;
  background: var(--el-bg-color-page);

  .join-card {
    width: 100%;
    max-width: 400px;

    .join-header {
      text-align: center;

      h2 {
        margin: 0 0 8px 0;
        color: var(--el-text-color-primary);
      }

      .description {
        margin: 0;
        color: var(--el-text-color-regular);
        font-size: 14px;
      }
    }

    .join-actions {
      text-align: center;
      margin-top: 24px;

      .join-btn {
        width: 100%;
      }
    }
  }
}
</style>