<script setup lang="ts">
import { ref, reactive, onMounted, computed } from "vue";
import { useI18n } from "vue-i18n";
import { useRouter, useRoute } from "vue-router";
import { useNotifications } from '@/composables/useNotifications';
import { authService } from "../services/authService";
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
  if (!joinFormRef.value) return;

  await joinFormRef.value.validate(async (valid) => {
    if (!valid) return;

    loading.value = true;
    try {
      const result = await authService.tellerLogin(joinForm.electionGuid, joinForm.accessCode);

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

onMounted(async () => {
  const electionGuid = route.params.electionGuid as string;
  const accessCode = route.query.code as string;

  if (electionGuid) {
    joinForm.electionGuid = electionGuid;
    prefilledFromUrl.value = true;
  }

  if (accessCode) {
    joinForm.accessCode = accessCode;
  }

  await fetchAvailableElections();
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

      <div v-if="loadingElections" class="loading-elections">
        <el-skeleton :rows="3" animated />
      </div>

      <div v-else-if="elections.length === 0 && !prefilledFromUrl" class="no-elections">
        <el-empty :description="t('auth.tellerJoin.noElections')" />
      </div>

      <el-form
        v-else
        ref="joinFormRef"
        :model="joinForm"
        :rules="rules"
        label-position="top"
        @keyup.enter="handleJoin"
      >
        <el-form-item
          :label="t('auth.tellerJoin.selectElection')"
          prop="electionGuid"
        >
          <el-select
            v-model="joinForm.electionGuid"
            :placeholder="t('auth.tellerJoin.selectElectionPlaceholder')"
            filterable
            style="width: 100%"
          >
            <el-option
              v-for="election in elections"
              :key="election.electionGuid"
              :label="election.name"
              :value="election.electionGuid"
            >
              <div class="election-option">
                <span class="election-name">{{ election.name }}</span>
                <span v-if="election.dateOfElection" class="election-date">{{ formatDate(election.dateOfElection) }}</span>
              </div>
            </el-option>
          </el-select>
        </el-form-item>

        <el-form-item
          :label="t('auth.tellerJoin.accessCodeLabel')"
          prop="accessCode"
        >
          <el-input
            v-model="joinForm.accessCode"
            :placeholder="t('auth.tellerJoin.accessCodePlaceholder')"
            type="password"
            show-password
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
    max-width: 450px;

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

    .loading-elections {
      padding: 20px 0;
    }

    .no-elections {
      padding: 20px 0;
    }

    .election-option {
      display: flex;
      justify-content: space-between;
      align-items: center;
      width: 100%;

      .election-name {
        flex: 1;
      }

      .election-date {
        color: var(--el-text-color-secondary);
        font-size: 12px;
        margin-left: 12px;
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
