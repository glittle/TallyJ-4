<template>
  <div class="monitoring-dashboard">
    <el-card>
      <template #header>
        <div class="card-header">
          <div class="header-actions">
            <el-button
              type="primary"
              :loading="loading"
              icon="Refresh"
              @click="refreshData"
            >
              {{ $t("common.refresh") }}
            </el-button>
          </div>
        </div>
      </template>

      <div v-if="loading && !monitorInfo" class="loading-container">
        <el-skeleton :rows="8" animated />
      </div>

      <div v-else-if="monitorInfo">
        <!-- Summary Cards -->
        <el-row :gutter="20" class="summary-row">
          <el-col :span="6">
            <el-card class="summary-card">
              <div class="summary-content">
                <div class="summary-icon">
                  <el-icon size="32" color="#409EFF"
                    ><DocumentChecked
                  /></el-icon>
                </div>
                <div class="summary-text">
                  <div class="summary-value">
                    {{ monitorInfo.totalBallots }}
                  </div>
                  <div class="summary-label">
                    {{ $t("monitoring.totalBallots") }}
                  </div>
                </div>
              </div>
            </el-card>
          </el-col>
          <el-col :span="6">
            <el-card class="summary-card">
              <div class="summary-content">
                <div class="summary-icon">
                  <el-icon size="32" color="#67C23A"><Check /></el-icon>
                </div>
                <div class="summary-text">
                  <div class="summary-value">{{ monitorInfo.totalVotes }}</div>
                  <div class="summary-label">
                    {{ $t("monitoring.totalVotes") }}
                  </div>
                </div>
              </div>
            </el-card>
          </el-col>
          <el-col :span="6">
            <el-card class="summary-card">
              <div class="summary-content">
                <div class="summary-icon">
                  <el-icon size="32" color="#E6A23C"><Monitor /></el-icon>
                </div>
                <div class="summary-text">
                  <div class="summary-value">
                    {{ monitorInfo.computers.length }}
                  </div>
                  <div class="summary-label">
                    {{ $t("monitoring.activeComputers") }}
                  </div>
                </div>
              </div>
            </el-card>
          </el-col>
          <el-col :span="6">
            <el-card class="summary-card">
              <div class="summary-content">
                <div class="summary-icon">
                  <el-icon size="32" color="#F56C6C"><Location /></el-icon>
                </div>
                <div class="summary-text">
                  <div class="summary-value">
                    {{ monitorInfo.locations.length }}
                  </div>
                  <div class="summary-label">
                    {{ $t("monitoring.locations") }}
                  </div>
                </div>
              </div>
            </el-card>
          </el-col>
        </el-row>

        <el-card
          v-if="monitorInfo.ballotsByMethod"
          class="method-breakdown-card"
          data-testid="ballots-by-method"
          style="margin-bottom: 20px"
        >
          <template #header>
            <span>{{ $t("monitoring.ballotsByMethod.title") }}</span>
          </template>
          <p class="online-ballot-breakdown-note">
            {{ $t("monitoring.ballotsByMethod.note") }}
          </p>
          <el-descriptions :column="5" border>
            <el-descriptions-item
              :label="$t('monitoring.ballotsByMethod.inPerson')"
            >
              <span data-testid="method-count-in-person">{{
                monitorInfo.ballotsByMethod.inPerson
              }}</span>
            </el-descriptions-item>
            <el-descriptions-item
              :label="$t('monitoring.ballotsByMethod.mailed')"
            >
              <span data-testid="method-count-mailed">{{
                monitorInfo.ballotsByMethod.mailed
              }}</span>
            </el-descriptions-item>
            <el-descriptions-item
              :label="$t('monitoring.ballotsByMethod.droppedOff')"
            >
              <span data-testid="method-count-dropped-off">{{
                monitorInfo.ballotsByMethod.droppedOff
              }}</span>
            </el-descriptions-item>
            <el-descriptions-item
              :label="$t('monitoring.ballotsByMethod.kiosk')"
            >
              <span data-testid="method-count-kiosk">{{
                monitorInfo.ballotsByMethod.kiosk
              }}</span>
            </el-descriptions-item>
            <el-descriptions-item
              :label="$t('monitoring.ballotsByMethod.online')"
            >
              <span data-testid="method-count-online">{{
                monitorInfo.ballotsByMethod.online
              }}</span>
            </el-descriptions-item>
          </el-descriptions>
        </el-card>

        <!-- Last Updated -->
        <el-alert
          :title="
            $t('monitoring.lastUpdated', {
              time: formatDateTime(monitorInfo.lastUpdated),
            })
          "
          type="info"
          :closable="false"
          style="margin: 20px 0"
        />

        <!-- Computers Table -->
        <el-card style="margin-bottom: 20px">
          <template #header>
            <span>{{ $t("monitoring.computers") }}</span>
          </template>
          <el-table :data="monitorInfo.computers" stripe style="width: 100%">
            <el-table-column
              prop="computerCode"
              :label="$t('monitoring.computerCode')"
              width="150"
            />
            <el-table-column
              prop="locationName"
              :label="$t('monitoring.location')"
              width="200"
            />
            <el-table-column
              prop="ballotCount"
              :label="$t('monitoring.ballotsEntered')"
              width="150"
              align="center"
            />
            <el-table-column
              prop="lastContact"
              :label="$t('monitoring.lastContact')"
              width="180"
            >
              <template #default="scope">
                {{ formatDateTime(scope.row.lastContact) }}
              </template>
            </el-table-column>
            <el-table-column
              prop="status"
              :label="$t('monitoring.status')"
              width="120"
            >
              <template #default="scope">
                <el-tag :type="getStatusType(scope.row.status)">
                  {{ scope.row.status }}
                </el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-card>

        <!-- Locations Table -->
        <el-card style="margin-bottom: 20px">
          <template #header>
            <span>{{ $t("monitoring.locations") }}</span>
          </template>
          <el-table :data="monitorInfo.locations" stripe style="width: 100%">
            <el-table-column
              prop="locationName"
              :label="$t('monitoring.location')"
              width="200"
            />
            <el-table-column
              prop="voterCount"
              :label="$t('monitoring.registeredVoters')"
              width="150"
              align="center"
            />
            <el-table-column
              prop="ballotCount"
              :label="$t('monitoring.ballotsEntered')"
              width="150"
              align="center"
            />
            <el-table-column
              prop="voteCount"
              :label="$t('monitoring.totalVotes')"
              width="120"
              align="center"
            />
            <el-table-column
              :label="$t('monitoring.turnout')"
              width="120"
              align="center"
            >
              <template #default="scope">
                {{
                  calculateTurnout(scope.row.voterCount, scope.row.ballotCount)
                }}%
              </template>
            </el-table-column>
            <el-table-column
              prop="status"
              :label="$t('monitoring.status')"
              width="120"
            >
              <template #default="scope">
                <el-tag :type="getStatusType(scope.row.status)">
                  {{ scope.row.status }}
                </el-tag>
              </template>
            </el-table-column>
          </el-table>
        </el-card>

        <OnlineVotingMonitorPanel
          :info="monitorInfo.onlineVotingInfo"
          :close-countdown="closeCountdown"
          :close-line="closeSummary.closeLine"
          :close-remaining-clock="closeRemainingClock"
          :can-manage="canAcceptOnlineBallots"
          :accepting="accepting"
          :updating-window="updatingWindow"
          @accept-all="confirmAcceptAll"
          @schedule-close="scheduleCloseInMinutes(5)"
          @close-now="closeOnlineNow"
          @open-for-minutes="openOnlineForMinutes(5)"
        />
      </div>

      <el-empty v-else :description="$t('monitoring.noData')" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
import OnlineVotingMonitorPanel from "@/components/results/OnlineVotingMonitorPanel.vue";
import { useApiErrorHandler } from "@/composables/useApiErrorHandler";
import { useNotifications } from "@/composables/useNotifications";
import { isFullTeller } from "@/domain/guestTellerAccess";
import { electionService } from "@/services/electionService";
import { useElectionStore } from "@/stores/electionStore";
import { extractApiErrorMessage } from "@/utils/errorHandler";
import {
  closeOnlineVotingNowAt,
  formatCloseRemainingClock,
  getOnlineVotingCloseCountdown,
  scheduleOnlineCloseAt,
} from "@/utils/onlineVotingCloseCountdown";
import { buildOnlineWindowSummary } from "@/utils/onlineVotingWindowSummary";
import {
  Check,
  DocumentChecked,
  Location,
  Monitor,
} from "@element-plus/icons-vue";
import { ElMessageBox } from "element-plus";
import { DateTime } from "luxon";
import { computed, onMounted, onUnmounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";
import { signalrService } from "../../services/signalrService";
import { useResultStore } from "../../stores/resultStore";
import type { MonitorInfoDto } from "../../types";

const route = useRoute();
const resultStore = useResultStore();
const electionStore = useElectionStore();
const { handleApiError } = useApiErrorHandler();
const { showSuccessMessage, showErrorMessage } = useNotifications();
const { t, locale } = useI18n();

const electionGuid = route.params.id as string;
const monitorInfo = ref<MonitorInfoDto | null>(null);
const loading = ref(false);
const accepting = ref(false);
const canAcceptOnlineBallots = computed(() => isFullTeller());
const refreshInterval = ref<number | null>(null);
const updatingWindow = ref(false);
const nowTick = ref(DateTime.now());
let closeCountdownTimer: ReturnType<typeof setInterval> | null = null;
let frontDeskConnection: Awaited<
  ReturnType<typeof signalrService.connectToFrontDeskHub>
> | null = null;
let onlineElectionHandler: ((data: unknown) => void) | null = null;

const closeIsEstimate = computed(() => {
  const current = electionStore.currentElection;
  if (!current || current.electionGuid !== electionGuid) {
    return true;
  }
  return current.onlineCloseIsEstimate ?? true;
});

const closeCountdown = computed(() =>
  getOnlineVotingCloseCountdown(
    monitorInfo.value?.onlineVotingInfo.onlineVotingStart,
    monitorInfo.value?.onlineVotingInfo.onlineVotingEnd,
    nowTick.value.toJSDate(),
  ),
);

const closeSummary = computed(() =>
  buildOnlineWindowSummary(
    monitorInfo.value?.onlineVotingInfo.onlineVotingStart,
    monitorInfo.value?.onlineVotingInfo.onlineVotingEnd,
    nowTick.value,
    (key, params) => t(key, params ?? {}),
    String(locale.value),
    closeIsEstimate.value,
  ),
);

const closeRemainingClock = computed(() =>
  formatCloseRemainingClock(closeCountdown.value.remainingMs),
);

onMounted(async () => {
  await ensureElectionLoaded();
  await loadData();
  startAutoRefresh();
  startCloseCountdownTick();
  await initializeOnlineElectionListener();
});

onUnmounted(async () => {
  stopAutoRefresh();
  stopCloseCountdownTick();
  await teardownOnlineElectionListener();
});

async function ensureElectionLoaded() {
  if (electionStore.currentElection?.electionGuid === electionGuid) {
    return;
  }
  try {
    await electionStore.fetchElectionById(electionGuid);
  } catch {
    // Countdown still works from monitor times; estimate wording falls back.
  }
}

async function loadData() {
  try {
    loading.value = true;
    const data = await resultStore.fetchMonitorInfo(electionGuid);
    monitorInfo.value = data;
  } catch (error) {
    handleApiError(error);
  } finally {
    loading.value = false;
  }
}

async function refreshData() {
  await loadData();
}

/** Soft refresh when operator changes online open/close (FrontDesk updateOnlineElection). */
async function initializeOnlineElectionListener() {
  try {
    frontDeskConnection = await signalrService.connectToFrontDeskHub();
    onlineElectionHandler = (data: unknown) => {
      const payload = data as { electionGuid?: string } | null;
      if (!payload?.electionGuid) {
        return;
      }
      if (
        String(payload.electionGuid).toLowerCase() !==
        electionGuid.toLowerCase()
      ) {
        return;
      }
      void loadData();
    };
    frontDeskConnection.on("updateOnlineElection", onlineElectionHandler);
    await signalrService.joinFrontDeskElection(electionGuid);
  } catch (e) {
    console.error("Failed to listen for updateOnlineElection on monitor:", e);
  }
}

/**
 * Drop this page's handler only. This page joins the FrontDesk election group
 * so it can receive updateOnlineElection events; we intentionally do not leave
 * the group here because FrontDesk connection/group membership may be shared
 * with other parts of the app in the same tab.
 */
async function teardownOnlineElectionListener() {
  try {
    if (frontDeskConnection && onlineElectionHandler) {
      frontDeskConnection.off("updateOnlineElection", onlineElectionHandler);
    }
  } catch (e) {
    console.error("Failed to remove updateOnlineElection handler:", e);
  } finally {
    onlineElectionHandler = null;
    frontDeskConnection = null;
  }
}

async function confirmAcceptAll() {
  try {
    const summary =
      await electionService.getAcceptAllOnlineBallotsSummary(electionGuid);
    if (summary.pendingCount === 0) {
      showErrorMessage(t("monitoring.acceptAll.none"));
      await loadData();
      return;
    }

    await ElMessageBox.confirm(
      t("monitoring.acceptAll.confirmMessage", {
        pending: summary.pendingCount,
      }),
      t("monitoring.acceptAll.confirmTitle"),
      {
        confirmButtonText: t("monitoring.acceptAll.button"),
        cancelButtonText: t("common.cancel"),
        type: "warning",
      },
    );

    accepting.value = true;
    const result = await electionService.acceptAllOnlineBallots(electionGuid);
    const messageKey = result.messageKey || "monitoring.acceptAll.complete";
    showSuccessMessage(t(messageKey, { accepted: result.acceptedCount ?? 0 }));
    await loadData();
  } catch (error: unknown) {
    if (error === "cancel" || error === "close") {
      return;
    }
    const fromBody = error as {
      messageKey?: string;
      response?: { data?: { messageKey?: string } };
    };
    const key = fromBody.messageKey ?? fromBody.response?.data?.messageKey;
    showErrorMessage(
      key ? t(key) : extractApiErrorMessage(error) || t("common.error"),
    );
  } finally {
    accepting.value = false;
  }
}

function startAutoRefresh() {
  // Refresh every 30 seconds
  refreshInterval.value = setInterval(() => {
    loadData();
  }, 30000);
}

function stopAutoRefresh() {
  if (refreshInterval.value) {
    clearInterval(refreshInterval.value);
    refreshInterval.value = null;
  }
}

function startCloseCountdownTick() {
  stopCloseCountdownTick();
  nowTick.value = DateTime.now();
  closeCountdownTimer = setInterval(() => {
    nowTick.value = DateTime.now();
  }, 1000);
}

function stopCloseCountdownTick() {
  if (closeCountdownTimer) {
    clearInterval(closeCountdownTimer);
    closeCountdownTimer = null;
  }
}

function currentOpenIso(): string | null {
  const open = monitorInfo.value?.onlineVotingInfo.onlineVotingStart;
  if (!open) {
    return null;
  }
  const date = new Date(open);
  return Number.isNaN(date.getTime()) ? null : date.toISOString();
}

async function applyCloseTime(close: Date, nextEstimate: boolean) {
  if (updatingWindow.value) {
    return;
  }
  updatingWindow.value = true;
  try {
    await electionStore.updateOnlineVotingWindow(electionGuid, {
      onlineWhenOpen: currentOpenIso(),
      onlineWhenClose: close.toISOString(),
      onlineCloseIsEstimate: nextEstimate,
    });
    showSuccessMessage(t("monitoring.onlineWindow.saved"));
    await loadData();
  } catch (error) {
    handleApiError(error);
  } finally {
    updatingWindow.value = false;
  }
}

/** v3 Schedule close in 5 minutes — firm deadline, not an estimate. */
function scheduleCloseInMinutes(minutes: number) {
  void applyCloseTime(scheduleOnlineCloseAt(minutes), false);
}

function closeOnlineNow() {
  void applyCloseTime(closeOnlineVotingNowAt(), closeIsEstimate.value);
}

function openOnlineForMinutes(minutes: number) {
  void applyCloseTime(scheduleOnlineCloseAt(minutes), closeIsEstimate.value);
}

function formatDateTime(date?: string | Date | null) {
  if (!date) {
    return "-";
  }
  return new Date(date).toLocaleString();
}

function getStatusType(status: string) {
  const statusMap: Record<string, string> = {
    Online: "success",
    Offline: "danger",
    Active: "success",
    Inactive: "warning",
    Closed: "info",
  };
  return statusMap[status] || "info";
}

function calculateTurnout(registered: number, ballots: number) {
  if (registered === 0) {
    return 0;
  }
  return Math.round((ballots / registered) * 100);
}
</script>

<style lang="less">
.monitoring-dashboard {
  max-width: 1400px;
  margin: 0 auto;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-actions {
  display: flex;
  gap: 10px;
}

.online-ballot-breakdown-note {
  margin: 0 0 12px;
  color: var(--el-text-color-secondary);
}

.summary-row {
  margin-bottom: 20px;
}

.summary-card {
  height: 100px;
}

.summary-content {
  display: flex;
  align-items: center;
  gap: 15px;
  height: 100%;
}

.summary-icon {
  flex-shrink: 0;
}

.summary-text {
  flex: 1;
}

.summary-value {
  font-size: 24px;
  font-weight: bold;
  color: #303133;
  line-height: 1.2;
}

.summary-label {
  font-size: 14px;
  color: #909399;
  margin-top: 4px;
}

.loading-container {
  padding: 40px;
}
</style>
