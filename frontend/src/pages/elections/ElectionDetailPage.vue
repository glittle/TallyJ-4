<script setup lang="ts">
// SetupTipsCard moved here from Dashboard: tips apply while configuring a
// specific election, next to the Election Details block.
import SetupTipsCard from "@/components/dashboard/SetupTipsCard.vue";
import { useNotifications } from "@/composables/useNotifications";
import { isGuestTeller } from "@/domain/guestTellerAccess";
import { formatNumber } from "@/utils/formatNumber";
import { Delete, Download, RefreshLeft } from "@element-plus/icons-vue";
import { ElMessageBox } from "element-plus";
import { computed, onMounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { electionService } from "../../services/electionService";
import { useElectionStatsStore } from "../../stores/electionStatsStore";
import { useElectionStore } from "../../stores/electionStore";
import { extractApiErrorMessage } from "../../utils/errorHandler";

const router = useRouter();
const route = useRoute();
const { t } = useI18n();
const electionStore = useElectionStore();
const electionStatsStore = useElectionStatsStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const electionGuid = route.params.id as string;
const loading = computed(
  () => electionStore.loading || electionStatsStore.loading,
);
const election = computed(() => electionStore.currentElection);
const electionStats = computed(() =>
  electionStatsStore.getCached(electionGuid),
);

const isGuest = computed(() => isGuestTeller());
const canResetTestElection = computed(
  () => !isGuest.value && election.value?.showAsTest === true,
);
const loadFailed = ref(false);

onMounted(async () => {
  loadFailed.value = false;
  try {
    await Promise.all([
      electionStore.fetchElectionById(electionGuid),
      electionStatsStore.fetchStats(electionGuid),
    ]);
  } catch (_error) {
    loadFailed.value = true;
    showErrorMessage(t("elections.loadError"));
  }
});

async function confirmReset() {
  try {
    await ElMessageBox.confirm(
      t("elections.reset.confirmMessage"),
      t("elections.reset.title"),
      {
        confirmButtonText: t("elections.reset.confirm"),
        cancelButtonText: t("common.cancel"),
        type: "warning",
        confirmButtonClass: "el-button--danger",
      },
    );

    await electionStore.resetElection(electionGuid);
    await electionStatsStore.fetchStats(electionGuid, { force: true });
    showSuccessMessage(t("elections.reset.success"));
  } catch (error: unknown) {
    if (error === "cancel" || error === "close") {
      return;
    }
    showErrorMessage(
      extractApiErrorMessage(error) || t("elections.reset.error"),
    );
  }
}

async function confirmDelete() {
  try {
    await ElMessageBox.confirm(
      t("elections.deleteConfirm"),
      t("common.warning"),
      {
        confirmButtonText: t("common.delete"),
        cancelButtonText: t("common.cancel"),
        type: "warning",
        confirmButtonClass: "el-button--danger",
      },
    );

    await electionStore.deleteElection(electionGuid);
    showSuccessMessage(t("elections.deleteSuccess"));
    router.push("/elections");
  } catch (error: any) {
    if (error !== "cancel") {
      showErrorMessage(error.message || t("elections.deleteError"));
    }
  }
}

function formatDate(date?: string) {
  if (!date) {
    return "-";
  }
  return new Date(date).toLocaleDateString();
}

function getStatusType(status?: string) {
  const typeMap: Record<string, any> = {
    Draft: "info",
    Voting: "success",
    Tallying: "warning",
    Finalized: "info",
  };
  return typeMap[status || ""] || "info";
}

async function exportElection() {
  if (!electionGuid) {
    return;
  }

  try {
    const blob = await electionService.exportElectionToJson(electionGuid);
    const url = globalThis.URL.createObjectURL(blob);
    const a = document.createElement("a");
    a.href = url;
    a.download = `Election_${electionGuid}.json`;
    document.body.appendChild(a);
    a.click();
    globalThis.URL.revokeObjectURL(url);
    a.remove();

    showSuccessMessage(t("elections.exportElectionSuccess"));
  } catch (error: any) {
    showErrorMessage(
      t("elections.exportElectionError") + " " + (error.message || ""),
    );
  }
}
</script>

<template>
  <div class="election-detail-page">
    <div v-if="loading" class="loading-container">
      <el-skeleton :rows="5" animated />
    </div>

    <div v-else-if="election">
      <div class="detail-top-row">
        <el-card class="info-card">
          <template #header>
            <span>{{ $t("elections.details") }}</span>
          </template>
          <el-descriptions :column="2" border>
            <el-descriptions-item :label="$t('elections.form.name')">
              {{ election.name }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('elections.form.type')">
              {{ election.electionType || "-" }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('elections.form.date')">
              {{ formatDate(election.dateOfElection) }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('elections.status')">
              <el-tag :type="getStatusType(election.electionStage)">
                {{ election.electionStage || "Draft" }}
              </el-tag>
            </el-descriptions-item>
            <el-descriptions-item :label="$t('elections.form.numberToElect')">
              {{ formatNumber(election.numberToElect) }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('elections.form.numberExtra')">
              {{ formatNumber(election.numberExtra) }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('elections.form.convenor')">
              {{ election.convenor || "-" }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('elections.form.electionMode')">
              {{ election.electionMode || "-" }}
            </el-descriptions-item>
          </el-descriptions>
        </el-card>
        <SetupTipsCard />
      </div>
      <el-card class="stats-card">
        <template #header>
          <span>{{ $t("elections.statistics") }}</span>
        </template>
        <div class="stat-item">
          <div class="stat-label">{{ $t("dashboard.totalVoters") }}</div>
          <div class="stat-value">
            {{ formatNumber(electionStats?.voterCount) }}
          </div>
        </div>
        <div class="stat-item">
          <div class="stat-label">{{ $t("dashboard.totalBallots") }}</div>
          <div class="stat-value">
            {{ formatNumber(electionStats?.ballotCount) }}
          </div>
        </div>
        <div class="stat-item">
          <div class="stat-label">{{ $t("elections.locations") }}</div>
          <div class="stat-value">
            {{ formatNumber(electionStats?.locationCount) }}
          </div>
        </div>
      </el-card>

      <el-row v-if="!isGuest">
        <el-card>
          <template #header>
            <span>{{ $t("common.actions") }}</span>
          </template>
          <el-button @click="exportElection">
            <el-icon>
              <Download />
            </el-icon>
            {{ $t("elections.exportElection") }}
          </el-button>
        </el-card>
      </el-row>

      <el-row v-if="!isGuest">
        <el-card class="danger-zone" style="margin-top: 20px">
          <template #header>
            <span style="color: #f56c6c">{{ $t("common.dangerZone") }}</span>
          </template>
          <div class="danger-zone-actions">
            <el-button
              v-if="canResetTestElection"
              type="warning"
              plain
              data-testid="reset-test-election"
              style="width: 100%"
              @click="confirmReset"
            >
              <el-icon>
                <RefreshLeft />
              </el-icon>
              {{ $t("elections.reset.action") }}
            </el-button>
            <el-button
              type="danger"
              plain
              style="width: 100%"
              @click="confirmDelete"
            >
              <el-icon>
                <Delete />
              </el-icon>
              {{ $t("common.delete") }}
            </el-button>
          </div>
        </el-card>
      </el-row>
    </div>

    <el-empty v-else :description="$t('elections.notFound')" />
  </div>
</template>

<style lang="less">
.election-detail-page {
  max-width: 1400px;
  margin: 0 auto;

  .loading-container {
    padding: 40px;
  }

  // Setup Tips sits beside Election Details (moved from Dashboard).
  .detail-top-row {
    display: flex;
    align-items: flex-start;
    gap: 16px;
    margin-top: 15px;

    .info-card {
      flex: 1;
      min-width: 0;
      margin-top: 0;
    }

    .setup-tips-card,
    .tips-panel {
      flex: 0 1 320px;
      max-width: 360px;
      margin-top: 0;
    }

    @media (max-width: 900px) {
      flex-direction: column;

      .setup-tips-card,
      .tips-panel {
        flex: 1 1 auto;
        max-width: none;
        width: 100%;
      }
    }
  }

  .info-card,
  .actions-card,
  .stats-card,
  .stage-card,
  .danger-zone {
    box-shadow: 0 2px 12px 0 rgba(0, 0, 0, 0.1);
  }

  .stats-card {
    .el-card__body {
      display: flex;
      gap: 3em;
    }
  }

  // Stack Danger Zone actions so both 100%-wide buttons share a left edge.
  // Element Plus `.el-button + .el-button { margin-left: 12px }` would otherwise
  // shift Delete while the buttons stay the same width.
  .danger-zone-actions {
    display: flex;
    flex-direction: column;
    align-items: stretch;
    gap: 12px;

    .el-button + .el-button {
      margin-left: 0;
    }
  }

  .stat-item {
    text-align: center;
  }

  .stat-label {
    font-size: 14px;
    color: #909399;
    margin-bottom: 8px;
  }

  .stat-value {
    font-size: 28px;
    font-weight: 600;
    color: #303132;
  }

  .stage-card {
    .stage-actions {
      display: flex;
      justify-content: center;
      padding: 10px 0;
    }
  }

  .el-card {
    margin-top: 15px;
  }
}
</style>
