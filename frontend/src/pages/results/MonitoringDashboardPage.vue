<template>
  <div class="monitoring-dashboard">
    <el-card>
      <template #header>
        <div class="card-header">
          <div class="header-actions">
            <el-button @click="handleImportCdn">
              <el-icon><Upload /></el-icon>
              {{ $t("ballots.cdnImport.button") }}
            </el-button>
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

        <!-- Online Voting Info -->
        <el-card>
          <template #header>
            <div class="online-voting-header">
              <span>{{ $t("monitoring.onlineVoting") }}</span>
              <el-button
                v-if="canAcceptOnlineBallots"
                type="primary"
                data-testid="accept-all-online-ballots"
                :loading="accepting"
                :disabled="pendingOnlineCount === 0"
                @click="confirmAcceptAll"
              >
                {{ $t("monitoring.acceptAll.button") }}
              </el-button>
            </div>
          </template>
          <el-descriptions :column="4" border>
            <el-descriptions-item :label="$t('monitoring.totalOnlineBallots')">
              {{ monitorInfo.onlineVotingInfo.totalOnlineBallots }}
            </el-descriptions-item>
            <el-descriptions-item
              :label="$t('monitoring.pendingOnlineBallots')"
            >
              {{ monitorInfo.onlineVotingInfo.pendingOnlineBallots }}
            </el-descriptions-item>
            <el-descriptions-item
              :label="$t('monitoring.processedOnlineBallots')"
            >
              {{ monitorInfo.onlineVotingInfo.processedOnlineBallots }}
            </el-descriptions-item>
            <el-descriptions-item :label="$t('monitoring.status')">
              <el-tag
                :type="
                  monitorInfo.onlineVotingInfo.onlineVotingEnabled
                    ? 'success'
                    : 'info'
                "
              >
                {{
                  monitorInfo.onlineVotingInfo.onlineVotingEnabled
                    ? $t("elections.onlineVotingEnabled")
                    : $t("elections.onlineVotingDisabled")
                }}
              </el-tag>
            </el-descriptions-item>
          </el-descriptions>
          <div
            class="online-close-countdown"
            data-testid="online-close-countdown"
            :class="closeCountdownClass"
          >
            <h3>{{ $t("monitoring.onlineWindow.title") }}</h3>
            <p class="online-close-status" data-testid="online-close-status">
              {{
                closeCountdown.isWindowOpen
                  ? $t("elections.onlineWindow.statusOpen")
                  : $t("elections.onlineWindow.statusClosed")
              }}
            </p>
            <p
              v-if="closeSummary.closeLine"
              class="online-close-line"
              data-testid="online-close-line"
            >
              {{ closeSummary.closeLine }}
            </p>
            <p v-else class="online-close-line" data-testid="online-close-line">
              {{ $t("monitoring.onlineWindow.noCloseTime") }}
            </p>
            <p
              v-if="closeCountdown.isClosingSoon"
              class="online-close-clock"
              data-testid="online-close-clock"
            >
              {{
                $t("monitoring.onlineWindow.remainingClock", {
                  clock: closeRemainingClock,
                })
              }}
            </p>
            <div v-if="canAcceptOnlineBallots" class="online-close-actions">
              <el-button
                v-if="!closeCountdown.isWindowOpen"
                data-testid="open-online-voting-5-minutes"
                :loading="updatingWindow"
                @click="openOnlineForMinutes(5)"
              >
                {{ $t("monitoring.onlineWindow.openFor5Minutes") }}
              </el-button>
              <el-button
                v-if="closeCountdown.isWindowOpen"
                data-testid="schedule-close-online-5-minutes"
                :loading="updatingWindow"
                @click="scheduleCloseInMinutes(5)"
              >
                {{ $t("monitoring.onlineWindow.scheduleCloseIn5Minutes") }}
              </el-button>
              <el-button
                v-if="closeCountdown.isWindowOpen"
                data-testid="close-online-voting-now"
                :loading="updatingWindow"
                @click="closeOnlineNow"
              >
                {{ $t("monitoring.onlineWindow.closeNow") }}
              </el-button>
            </div>
          </div>
          <div
            class="online-ballot-breakdown"
            data-testid="online-ballot-status-breakdown"
          >
            <h3>{{ $t("monitoring.onlineBallots.breakdownTitle") }}</h3>
            <p class="online-ballot-breakdown-note">
              {{ $t("monitoring.onlineBallots.countsOnly") }}
            </p>
            <el-descriptions :column="3" border>
              <!-- Submitted only. Summary "Pending" is Submitted + Processing. -->
              <el-descriptions-item
                :label="$t('monitoring.onlineBallots.status.Submitted')"
              >
                <el-tag
                  :type="onlineBallotStatusView('Submitted').tagType"
                  data-testid="submitted-online-ballots-count"
                >
                  {{ submittedOnlineCount }}
                </el-tag>
              </el-descriptions-item>
              <el-descriptions-item
                :label="$t('monitoring.onlineBallots.status.Processing')"
              >
                <el-tag
                  :type="onlineBallotStatusView('Processing').tagType"
                  data-testid="processing-online-ballots-count"
                >
                  {{ processingOnlineCount }}
                </el-tag>
              </el-descriptions-item>
              <el-descriptions-item
                :label="$t('monitoring.onlineBallots.status.Processed')"
              >
                <el-tag
                  :type="onlineBallotStatusView('Processed').tagType"
                  data-testid="accepted-online-ballots-count"
                >
                  {{ acceptedOnlineCount }}
                </el-tag>
              </el-descriptions-item>
            </el-descriptions>
          </div>
          <div class="accept-all-history">
            <h3>{{ $t("monitoring.acceptAll.history") }}</h3>
            <el-table
              v-if="acceptAllRuns.length > 0"
              :data="acceptAllRuns"
              stripe
              data-testid="accept-all-history"
              style="width: 100%"
            >
              <el-table-column
                :label="$t('monitoring.acceptAll.when')"
                min-width="180"
              >
                <template #default="scope">
                  {{ formatDateTime(scope.row.when) }}
                </template>
              </el-table-column>
              <el-table-column
                :label="$t('monitoring.acceptAll.who')"
                min-width="160"
              >
                <template #default="scope">
                  {{ acceptAllWho(scope.row) }}
                </template>
              </el-table-column>
              <el-table-column
                :label="$t('monitoring.acceptAll.pendingBefore')"
                width="140"
                align="center"
              >
                <template #default="scope">
                  {{ scope.row.pendingBefore }}
                </template>
              </el-table-column>
              <el-table-column
                :label="$t('monitoring.acceptAll.pendingAfter')"
                width="140"
                align="center"
              >
                <template #default="scope">
                  {{ scope.row.pendingAfter }}
                </template>
              </el-table-column>
              <el-table-column
                :label="$t('monitoring.acceptAll.acceptedBefore')"
                width="150"
                align="center"
              >
                <template #default="scope">
                  {{ scope.row.acceptedBefore }}
                </template>
              </el-table-column>
              <el-table-column
                :label="$t('monitoring.acceptAll.acceptedAfter')"
                width="150"
                align="center"
              >
                <template #default="scope">
                  {{ scope.row.acceptedAfter }}
                </template>
              </el-table-column>
            </el-table>
            <p
              v-else
              class="accept-all-history-empty"
              data-testid="accept-all-history-empty"
            >
              {{ $t("monitoring.acceptAll.noHistory") }}
            </p>
          </div>
        </el-card>
      </div>

      <el-empty v-else :description="$t('monitoring.noData')" />
    </el-card>
  </div>
</template>

<script setup lang="ts">
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
  Upload,
} from "@element-plus/icons-vue";
import { ElMessageBox } from "element-plus";
import { DateTime } from "luxon";
import { computed, onMounted, onUnmounted, ref } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute, useRouter } from "vue-router";
import { signalrService } from "../../services/signalrService";
import { useResultStore } from "../../stores/resultStore";
import { onlineBallotMonitorStatus } from "../../utils/onlineBallotMonitorStatus";
import type { AcceptAllOnlineBallotsRunDto, MonitorInfoDto } from "../../types";

const route = useRoute();
const router = useRouter();
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
const pendingOnlineCount = computed(
  () => monitorInfo.value?.onlineVotingInfo.pendingOnlineBallots ?? 0,
);
const acceptAllRuns = computed(
  () => monitorInfo.value?.onlineVotingInfo.acceptAllRuns ?? [],
);
const submittedOnlineCount = computed(
  () => monitorInfo.value?.onlineVotingInfo.submittedOnlineBallots ?? 0,
);
const processingOnlineCount = computed(
  () => monitorInfo.value?.onlineVotingInfo.processingOnlineBallots ?? 0,
);
const acceptedOnlineCount = computed(
  () => monitorInfo.value?.onlineVotingInfo.processedOnlineBallots ?? 0,
);
const onlineBallotStatusView = onlineBallotMonitorStatus;
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

const closeCountdownClass = computed(() => {
  if (closeCountdown.value.isClosingSoon) {
    return "is-closing-soon";
  }
  return closeCountdown.value.isWindowOpen ? "is-open" : "is-closed";
});

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

function handleImportCdn() {
  router.push(`/elections/${electionGuid}/ballots/cdn-import`);
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

function acceptAllWho(run: AcceptAllOnlineBallotsRunDto) {
  return run.acceptedBy || run.acceptedByUserId || "-";
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

.online-voting-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.online-ballot-breakdown,
.accept-all-history,
.online-close-countdown {
  margin-top: 20px;

  h3 {
    margin: 0 0 12px;
    font-size: 16px;
    font-weight: 600;
  }
}

.online-close-countdown {
  padding: 12px 14px;
  border-radius: var(--el-border-radius-base);
  border: 1px solid var(--el-border-color);
  background: var(--el-fill-color-light);

  &.is-open {
    border-color: var(--el-color-success);
    background: var(--el-color-success-light-9);
  }

  &.is-closing-soon {
    border-color: var(--el-color-warning);
    background: var(--el-color-warning-light-9);
  }

  &.is-closed {
    border-color: var(--el-color-danger);
    background: var(--el-color-danger-light-9);
  }

  .online-close-status {
    margin: 0 0 4px;
    font-weight: 600;
  }

  .online-close-line,
  .online-close-clock {
    margin: 0;
  }

  .online-close-clock {
    margin-top: 4px;
    font-size: 20px;
    font-weight: 700;
    font-variant-numeric: tabular-nums;
  }
}

.online-close-actions {
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
  margin-top: 12px;
}

.online-ballot-breakdown-note,
.accept-all-history-empty {
  margin: 0 0 12px;
  color: #909399;
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
