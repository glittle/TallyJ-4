<script setup lang="ts">
import type {
  AcceptAllOnlineBallotsRunDto,
  OnlineVotingInfoDto,
} from "@/types";
import { onlineBallotMonitorStatus } from "@/utils/onlineBallotMonitorStatus";
import type { OnlineVotingCloseCountdown } from "@/utils/onlineVotingCloseCountdown";
import { computed } from "vue";

const props = defineProps<{
  info: OnlineVotingInfoDto;
  closeCountdown: OnlineVotingCloseCountdown;
  closeLine: string | null;
  closeRemainingClock: string;
  canManage: boolean;
  accepting: boolean;
  updatingWindow: boolean;
}>();

const emit = defineEmits<{
  acceptAll: [];
  scheduleClose: [];
  closeNow: [];
  openForMinutes: [];
}>();

const pendingCount = computed(() => props.info.pendingOnlineBallots ?? 0);
const submittedCount = computed(() => props.info.submittedOnlineBallots ?? 0);
const processingCount = computed(() => props.info.processingOnlineBallots ?? 0);
const acceptedCount = computed(() => props.info.processedOnlineBallots ?? 0);
const sessionCount = computed(
  () => props.info.connectedOnlineVoterSessions ?? 0,
);
const votedAnotherWayCount = computed(
  () => props.info.pendingOnlineVotedAnotherWay ?? 0,
);
const acceptAllRuns = computed(() => props.info.acceptAllRuns ?? []);
const statusView = onlineBallotMonitorStatus;

const statusKind = computed(() => {
  if (props.closeCountdown.isClosingSoon) {
    return "closingSoon";
  }
  return props.closeCountdown.isWindowOpen ? "open" : "closed";
});

const statusClass = computed(() => {
  if (statusKind.value === "closingSoon") {
    return "is-closing-soon";
  }
  return statusKind.value === "open" ? "is-open" : "is-closed";
});

const statusLabelKey = computed(() => {
  if (statusKind.value === "closingSoon") {
    return "monitoring.onlineWindow.closingSoon";
  }
  return statusKind.value === "open"
    ? "monitoring.onlineWindow.open"
    : "monitoring.onlineWindow.closed";
});

function formatDateTime(date?: string | Date | null) {
  if (!date) {
    return "-";
  }
  return new Date(date).toLocaleString();
}

function acceptAllWho(run: AcceptAllOnlineBallotsRunDto) {
  return run.acceptedBy || run.acceptedByUserId || "-";
}
</script>

<template>
  <el-card class="online-voting-monitor">
    <template #header>
      <div class="online-voting-monitor__header">
        <span>{{ $t("monitoring.onlineVoting") }}</span>
        <el-button
          v-if="canManage"
          type="primary"
          data-testid="accept-all-online-ballots"
          :loading="accepting"
          :disabled="pendingCount === 0"
          @click="emit('acceptAll')"
        >
          {{ $t("monitoring.acceptAll.buttonShort") }}
        </el-button>
      </div>
    </template>

    <div
      class="online-voting-monitor__status"
      data-testid="online-close-countdown"
      :class="statusClass"
    >
      <div class="online-voting-monitor__status-main">
        <el-tag
          class="online-voting-monitor__status-tag"
          :type="
            statusKind === 'closed'
              ? 'danger'
              : statusKind === 'closingSoon'
                ? 'warning'
                : 'success'
          "
          data-testid="online-close-status"
        >
          {{ $t(statusLabelKey) }}
        </el-tag>
        <el-tag
          v-if="!info.onlineVotingEnabled"
          type="info"
          data-testid="online-voting-enabled-tag"
        >
          {{ $t("elections.onlineVotingDisabled") }}
        </el-tag>
        <p
          v-if="closeLine"
          class="online-voting-monitor__close-line"
          data-testid="online-close-line"
        >
          {{ closeLine }}
        </p>
        <p
          v-else
          class="online-voting-monitor__close-line"
          data-testid="online-close-line"
        >
          {{ $t("monitoring.onlineWindow.noCloseTime") }}
        </p>
        <p
          v-if="closeCountdown.isClosingSoon"
          class="online-voting-monitor__clock"
          data-testid="online-close-clock"
        >
          {{
            $t("monitoring.onlineWindow.remainingClock", {
              clock: closeRemainingClock,
            })
          }}
        </p>
      </div>
      <div v-if="canManage" class="online-voting-monitor__window-actions">
        <el-button
          v-if="!closeCountdown.isWindowOpen"
          data-testid="open-online-voting-5-minutes"
          :loading="updatingWindow"
          @click="emit('openForMinutes')"
        >
          {{ $t("monitoring.onlineWindow.openFor5Minutes") }}
        </el-button>
        <el-button
          v-if="closeCountdown.isWindowOpen"
          data-testid="schedule-close-online-5-minutes"
          :loading="updatingWindow"
          @click="emit('scheduleClose')"
        >
          {{ $t("monitoring.onlineWindow.scheduleCloseIn5Minutes") }}
        </el-button>
        <el-button
          v-if="closeCountdown.isWindowOpen"
          data-testid="close-online-voting-now"
          :loading="updatingWindow"
          @click="emit('closeNow')"
        >
          {{ $t("monitoring.onlineWindow.closeNow") }}
        </el-button>
      </div>
    </div>

    <div
      class="online-voting-monitor__counts"
      data-testid="online-ballot-status-breakdown"
    >
      <div class="online-voting-monitor__metrics">
        <div
          class="online-voting-monitor__metric"
          :class="{ 'is-actionable': pendingCount > 0 }"
        >
          <div
            class="online-voting-monitor__metric-value"
            data-testid="pending-online-ballots-count"
          >
            {{ pendingCount }}
          </div>
          <div class="online-voting-monitor__metric-label">
            {{ $t("monitoring.pendingOnlineBallots") }}
          </div>
        </div>
        <div class="online-voting-monitor__metric">
          <div
            class="online-voting-monitor__metric-value"
            :class="`is-${statusView('Submitted').tagType}`"
            data-testid="submitted-online-ballots-count"
          >
            {{ submittedCount }}
          </div>
          <div class="online-voting-monitor__metric-label">
            {{ $t("monitoring.onlineBallots.status.Submitted") }}
          </div>
        </div>
        <div class="online-voting-monitor__metric">
          <div
            class="online-voting-monitor__metric-value"
            :class="`is-${statusView('Processing').tagType}`"
            data-testid="processing-online-ballots-count"
          >
            {{ processingCount }}
          </div>
          <div class="online-voting-monitor__metric-label">
            {{ $t("monitoring.onlineBallots.status.Processing") }}
          </div>
        </div>
        <div class="online-voting-monitor__metric">
          <div
            class="online-voting-monitor__metric-value"
            :class="`is-${statusView('Processed').tagType}`"
            data-testid="accepted-online-ballots-count"
          >
            {{ acceptedCount }}
          </div>
          <div class="online-voting-monitor__metric-label">
            {{ $t("monitoring.onlineBallots.status.Processed") }}
          </div>
        </div>
        <div
          class="online-voting-monitor__metric"
          data-testid="connected-online-voter-sessions"
        >
          <div
            class="online-voting-monitor__metric-value"
            data-testid="connected-online-voter-sessions-count"
          >
            {{ sessionCount }}
          </div>
          <div class="online-voting-monitor__metric-label">
            {{ $t("monitoring.connectedOnlineVoters.sessions") }}
          </div>
        </div>
        <div class="online-voting-monitor__metric">
          <div
            class="online-voting-monitor__metric-value"
            data-testid="pending-online-voted-another-way-count"
          >
            {{ votedAnotherWayCount }}
          </div>
          <div class="online-voting-monitor__metric-label">
            {{ $t("monitoring.onlineBallots.votedAnotherWay") }}
          </div>
        </div>
        <div class="online-voting-monitor__metric is-muted">
          <div class="online-voting-monitor__metric-value">
            {{ info.totalOnlineBallots }}
          </div>
          <div class="online-voting-monitor__metric-label">
            {{ $t("monitoring.totalOnlineBallots") }}
          </div>
        </div>
      </div>
      <p class="online-voting-monitor__note">
        {{ $t("monitoring.onlineBallots.countsOnly") }}
      </p>
    </div>

    <div class="online-voting-monitor__history">
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
        class="online-voting-monitor__history-empty"
        data-testid="accept-all-history-empty"
      >
        {{ $t("monitoring.acceptAll.noHistory") }}
      </p>
    </div>
  </el-card>
</template>

<style lang="less">
.online-voting-monitor {
  &__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: var(--spacing-3);
  }

  &__status {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    gap: var(--spacing-3);
    margin-bottom: var(--spacing-5);
    padding: var(--spacing-3) var(--spacing-4);
    border-radius: var(--el-border-radius-base);
    border: 1px solid transparent;
    border-inline-start-width: 4px;
    background: color-mix(in srgb, var(--el-fill-color-light) 80%, var(--el-bg-color));

    &.is-open {
      border-inline-start-color: var(--el-color-success);
      background: color-mix(
        in srgb,
        var(--el-color-success) 12%,
        var(--el-bg-color)
      );
    }

    &.is-closing-soon {
      border-inline-start-color: var(--el-color-warning);
      background: color-mix(
        in srgb,
        var(--el-color-warning) 14%,
        var(--el-bg-color)
      );
    }

    &.is-closed {
      border-inline-start-color: var(--el-color-danger);
      background: color-mix(
        in srgb,
        var(--el-color-danger) 12%,
        var(--el-bg-color)
      );
    }
  }

  &__status-main {
    display: flex;
    flex-wrap: wrap;
    align-items: baseline;
    gap: var(--spacing-2) var(--spacing-3);
    min-width: 0;
  }

  &__status-tag {
    font-weight: var(--font-weight-semibold);
  }

  &__close-line {
    margin: 0;
    color: var(--color-text-primary);
  }

  &__clock {
    margin: 0;
    font-size: var(--font-size-xl);
    font-weight: var(--font-weight-bold);
    font-variant-numeric: tabular-nums;
    color: var(--color-text-primary);
  }

  &__window-actions {
    display: flex;
    flex-wrap: wrap;
    gap: var(--spacing-2);
  }

  &__metrics {
    display: grid;
    grid-template-columns: repeat(auto-fit, minmax(7.5rem, 1fr));
    gap: var(--spacing-4) var(--spacing-3);
  }

  &__metric {
    min-width: 0;

    &.is-actionable .online-voting-monitor__metric-value {
      color: var(--el-color-primary);
    }

    &.is-muted .online-voting-monitor__metric-value,
    &.is-muted .online-voting-monitor__metric-label {
      color: var(--el-text-color-secondary);
    }
  }

  &__metric-value {
    font-size: var(--font-size-2xl);
    font-weight: var(--font-weight-bold);
    line-height: var(--line-height-tight);
    font-variant-numeric: tabular-nums;
    color: var(--color-text-primary);

    &.is-warning {
      color: var(--el-color-warning);
    }

    &.is-success {
      color: var(--el-color-success);
    }
  }

  &__metric-label {
    margin-top: var(--spacing-1);
    font-size: var(--font-size-sm);
    color: var(--el-text-color-secondary);
  }

  &__note,
  &__history-empty {
    margin: var(--spacing-3) 0 0;
    color: var(--el-text-color-secondary);
  }

  &__history {
    margin-top: var(--spacing-6);

    h3 {
      margin: 0 0 var(--spacing-3);
      font-size: var(--font-size-base);
      font-weight: var(--font-weight-semibold);
      color: var(--color-text-primary);
    }
  }
}
</style>
