<script setup lang="ts">
import { ref, reactive, onMounted, onBeforeUnmount, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useNotifications } from '@/composables/useNotifications';
import { useAuthStore } from "../stores/authStore";
import api from "../services/api";
import type { FormInstance, FormRules } from "element-plus";

interface AvailableElection {
  electionGuid: string;
  name: string;
  dateOfElection?: string;
  electionType?: string;
}

const { t } = useI18n();
const router = useRouter();
const route = useRoute();
const { showSuccessMessage, showErrorMessage } = useNotifications();
const authStore = useAuthStore();

const joinFormRef = ref<FormInstance>();
const loading = ref(false);
const loadingElections = ref(false);
const elections = ref<AvailableElection[]>([]);
const prefilledFromUrl = ref(false);

const joinForm = reactive({
  electionGuid: "",
  accessCode: "",
});

const rules: FormRules = {
  electionGuid: [
    { required: true, message: t("auth.tellerJoin.electionRequired"), trigger: "change" },
  ],
  accessCode: [
    { required: true, message: t("auth.tellerJoin.accessCodeRequired"), trigger: "blur" },
  ],
};

const selectedElectionName = computed(() => {
  const found = elections.value.find(e => e.electionGuid === joinForm.electionGuid);
  return found?.name;
});

async function fetchAvailableElections() {
  loadingElections.value = true;
  try {
    const response = await api.get<{ data: AvailableElection[] }>('/api/public/elections');
    elections.value = response.data?.data ?? [];
  } catch (error) {
    console.error("Failed to fetch elections:", error);
  } finally {
    loadingElections.value = false;
  }
}

const handleJoin = async () => {
  console.log('Joining election...', joinFormRef.value);
  if (!joinFormRef.value) return;

  await joinFormRef.value.validate(async (valid) => {
    if (!valid) return;

    loading.value = true;
    try {
      const result = await authStore.tellerLogin(joinForm.electionGuid, joinForm.accessCode);

      showSuccessMessage(t("auth.tellerJoin.joinSuccess"));

      router.push(`/elections/${result.electionGuid}`);
    } catch (error) {
      console.error("Teller join failed:", error);
      showErrorMessage(t("auth.tellerJoin.invalidElection"));
    } finally {
      loading.value = false;
    }
  });
};

function formatDate(date?: string) {
  if (!date) return "";
  return new Date(date).toLocaleDateString();
}

// add handler so if ESC is pressed, we go back to landing page
const handleKeydown = (event: KeyboardEvent) => {
  if (event.key === "Escape") {
    router.push("/");
  }
};

onMounted(async () => {
  const electionGuid = route.params.electionGuid as string;

  if (electionGuid) {
    prefilledFromUrl.value = true;

    const accessCode = route.query.code as string;
    if (accessCode) {
      joinForm.accessCode = accessCode;
    }
  }


  await fetchAvailableElections();

  if (prefilledFromUrl.value) {
    // if we prefilled the form from the URL, immediately try to join
    joinForm.electionGuid = electionGuid;
    await handleJoin();
  }

  globalThis.addEventListener("keydown", handleKeydown);
});

onBeforeUnmount(() => {
  globalThis.removeEventListener("keydown", handleKeydown);
});
</script>

<template>
  <div class="login-page">
    <el-card class="login-card">
      <template #header>
        <div class="login-header">
          <h2>{{ t("auth.tellerJoin.title") }}</h2>
          <p class="mode-hint">{{ t("auth.tellerJoin.description") }}</p>
        </div>
      </template>

      <div v-if="loadingElections" class="loading-elections">
        <el-skeleton :rows="3" animated />
      </div>

      <div v-else-if="elections.length === 0 && !prefilledFromUrl" class="no-elections">
        <el-empty :description="t('auth.tellerJoin.noElections')" />
      </div>

      <el-form v-else ref="joinFormRef" :model="joinForm" :rules="rules" label-position="top" @keyup.enter="handleJoin">
        <el-form-item :label="t('auth.tellerJoin.selectElection')" prop="electionGuid">
          <select v-model="joinForm.electionGuid" size="9"
            style="width: 100%; padding: 8px; border: 1px solid #dcdfe6; border-radius: 4px;">
            <option v-for="election in elections" :key="election.electionGuid" :value="election.electionGuid">
              {{ election.name }}{{ election.dateOfElection ? ' - ' + formatDate(election.dateOfElection) : '' }}
            </option>
          </select>
        </el-form-item>

        <el-form-item :label="t('auth.tellerJoin.accessCodeLabel')" prop="accessCode">
          <el-input v-model="joinForm.accessCode" :placeholder="t('auth.tellerJoin.accessCodePlaceholder')"
            type="text" />
        </el-form-item>

        <div class="login-actions">
          <el-button type="primary" :loading="loading" class="submit-btn" @click="handleJoin">
            {{ t("auth.tellerJoin.joinButton") }}
          </el-button>
        </div>

        <div class="auth-links">
          <router-link to="/">
            {{ t("common.cancel") }}
          </router-link>
        </div>
      </el-form>
    </el-card>
  </div>
</template>

<style lang="less">
.login-page {
  display: flex;
  justify-content: center;
  align-items: center;
  padding-top: 40px;

  .login-card {
    width: 100%;
    max-width: 400px;
    border-radius: 12px;
  }

  .login-header {
    text-align: center;
  }

  .login-header h2 {
    margin: 0;
    color: var(--color-text-primary);
  }

  .mode-hint {
    margin-top: 10px;
    color: var(--color-text-secondary);
    font-size: 0.9rem;
  }

  .loading-elections {
    padding: 20px 0;
  }

  .no-elections {
    padding: 20px 0;
  }

  .login-actions {
    margin-top: 30px;
    display: flex;
    flex-direction: column;
    gap: 10px;
  }

  .submit-btn {
    width: 100%;
  }

  .auth-links {
    margin-top: 20px;
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 10px;
  }

  .auth-links a {
    color: var(--color-primary-500);
    text-decoration: none;
    font-size: 0.85rem;
  }

  .auth-links a:hover {
    text-decoration: underline;
  }
}
</style>
