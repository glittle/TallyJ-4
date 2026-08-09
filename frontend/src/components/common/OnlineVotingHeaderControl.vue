<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import { isFullTeller } from "@/domain/guestTellerAccess";
import { useElectionStore } from "@/stores/electionStore";
import { extractApiErrorMessage } from "@/utils/errorHandler";
import { buildOnlineWindowSummary } from "@/utils/onlineVotingWindowSummary";
import { DateTime } from "luxon";
import { computed, onBeforeUnmount, reactive, ref, watch } from "vue";
import { useI18n } from "vue-i18n";
import { useRoute } from "vue-router";

const { t, locale } = useI18n();
const route = useRoute();
const electionStore = useElectionStore();
const { showErrorMessage } = useNotifications();
const saveResults = ref("");

const drawerOpen = ref(false);
const saving = ref(false);
/** Ticks while the drawer is open so relative phrases stay fresh. */
const nowTick = ref(DateTime.now());
let nowTimer: ReturnType<typeof setInterval> | null = null;

const electionGuid = computed(() => route.params.id as string | undefined);

const election = computed(() => {
  const current = electionStore.currentElection;
  if (!current || current.electionGuid !== electionGuid.value) {
    return null;
  }
  return current;
});

/** Header chip only when setup has enabled online voting for this election. */
const showControl = computed(
  () =>
    isFullTeller() &&
    Boolean(electionGuid.value) &&
    Boolean(election.value?.useOnlineVoting),
);

/** Whether the form window (or saved election when form empty) is open now. */
function isOpenAt(
  openValue: Date | string | null | undefined,
  closeValue: Date | string | null | undefined,
  nowMs: number = Date.now(),
): boolean {
  const openMs = openValue ? new Date(openValue).getTime() : null;
  const closeMs = closeValue ? new Date(closeValue).getTime() : null;
  if (openMs !== null && Number.isNaN(openMs)) {
    return false;
  }
  if (closeMs !== null && Number.isNaN(closeMs)) {
    return false;
  }
  if (openMs !== null && nowMs < openMs) {
    return false;
  }
  if (closeMs !== null && nowMs >= closeMs) {
    return false;
  }
  // No dates yet → closed.
  if (openMs === null && closeMs === null) {
    return false;
  }
  return true;
}

const form = reactive<{
  onlineWhenOpen: Date | null;
  onlineWhenClose: Date | null;
  onlineCloseIsEstimate: boolean;
}>({
  onlineWhenOpen: null,
  onlineWhenClose: null,
  onlineCloseIsEstimate: true,
});

/** Green when the saved election window is active; red otherwise. */
const isWindowOpen = computed(() => {
  const e = election.value;
  if (!e?.useOnlineVoting) {
    return false;
  }
  return isOpenAt(e.onlineWhenOpen, e.onlineWhenClose);
});

/** Status for the drawer summary — reflects form values as shown (even unsaved). */
const isFormWindowOpen = computed(() =>
  isOpenAt(form.onlineWhenOpen, form.onlineWhenClose, nowTick.value.toMillis()),
);

const windowSummary = computed(() =>
  buildOnlineWindowSummary(
    form.onlineWhenOpen,
    form.onlineWhenClose,
    nowTick.value,
    (key, params) => t(key, params ?? {}),
    String(locale.value),
  ),
);

const hasSummary = computed(() =>
  Boolean(
    windowSummary.value.openLine ||
    windowSummary.value.closeLine ||
    windowSummary.value.durationLine,
  ),
);

watch(
  electionGuid,
  async (guid) => {
    if (!guid || !isFullTeller()) {
      return;
    }
    if (electionStore.currentElection?.electionGuid !== guid) {
      try {
        await electionStore.fetchElectionById(guid);
      } catch {
        // Optional header control; page may surface errors.
      }
    }
  },
  { immediate: true },
);

watch(drawerOpen, (open) => {
  if (open) {
    nowTick.value = DateTime.now();
    if (!nowTimer) {
      nowTimer = setInterval(() => {
        nowTick.value = DateTime.now();
      }, 30_000);
    }
  } else if (nowTimer) {
    clearInterval(nowTimer);
    nowTimer = null;
  }
});

// Clear the in-drawer save banner when the operator edits either date
// (toast would cover the drawer summary at the top of the screen).
watch(
  () => [form.onlineWhenOpen, form.onlineWhenClose] as const,
  () => {
    saveResults.value = "";
  },
);

onBeforeUnmount(() => {
  if (nowTimer) {
    clearInterval(nowTimer);
    nowTimer = null;
  }
});

function toDate(value?: string | null): Date | null {
  if (!value) {
    return null;
  }
  const d = new Date(value);
  return Number.isNaN(d.getTime()) ? null : d;
}

function openDrawer() {
  saveResults.value = "";
  form.onlineWhenOpen = toDate(election.value?.onlineWhenOpen);
  form.onlineWhenClose = toDate(election.value?.onlineWhenClose);
  form.onlineCloseIsEstimate = election.value?.onlineCloseIsEstimate ?? true;
  drawerOpen.value = true;
}

async function saveDates() {
  saveResults.value = "";

  const guid = electionGuid.value;
  if (!guid || saving.value) {
    return;
  }

  // Client-side check mirrors server FluentValidation (clearer than a 400 round-trip).
  if (
    form.onlineWhenOpen &&
    form.onlineWhenClose &&
    form.onlineWhenOpen >= form.onlineWhenClose
  ) {
    showErrorMessage(t("elections.onlineWindow.openBeforeClose"));
    return;
  }

  saving.value = true;
  try {
    await electionStore.updateOnlineVotingWindow(guid, {
      onlineWhenOpen: form.onlineWhenOpen
        ? form.onlineWhenOpen.toISOString()
        : null,
      onlineWhenClose: form.onlineWhenClose
        ? form.onlineWhenClose.toISOString()
        : null,
      onlineCloseIsEstimate: form.onlineCloseIsEstimate,
    });
    saveResults.value = t("elections.onlineVotingWindowSaved");
    // Keep the drawer open so the operator can review the summary lines.
  } catch (error: unknown) {
    // hey-api throws the ProblemDetails JSON body (not an Error instance).
    showErrorMessage(extractApiErrorMessage(error));
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <div
    v-if="showControl"
    class="header-status-box online-voting-header-box"
    :class="isWindowOpen ? 'is-open' : 'is-closed'"
  >
    <span class="header-status-label">
      {{ t("elections.onlineVoting") }}
    </span>
    <el-button type="primary" size="small" @click="openDrawer">
      {{ t("elections.setOnlineVotingWindow") }}
    </el-button>

    <el-drawer
      v-model="drawerOpen"
      :title="t('elections.onlineVotingWindow')"
      direction="ttb"
      size="auto"
      class="online-voting-window-drawer"
      modal-class="online-voting-window-drawer-modal"
      :lock-scroll="false"
      append-to-body
    >
      <div class="online-window-body">
        <div class="online-window-field">
          <label>{{ t("elections.form.onlineWhenOpen") }}</label>
          <el-date-picker
            v-model="form.onlineWhenOpen"
            type="datetime"
            :placeholder="t('elections.form.onlineWhenOpenPlaceholder')"
            style="width: 100%"
          />
        </div>
        <div class="online-window-field">
          <label>{{ t("elections.form.onlineWhenClose") }}</label>
          <el-date-picker
            v-model="form.onlineWhenClose"
            type="datetime"
            :placeholder="t('elections.form.onlineWhenClosePlaceholder')"
            style="width: 100%"
          />
        </div>

        <div class="online-window-field online-window-estimate">
          <label>{{ t("elections.form.onlineCloseIsEstimate") }}</label>
          <el-switch v-model="form.onlineCloseIsEstimate" />
        </div>

        <div
          class="online-window-summary"
          :class="isFormWindowOpen ? 'is-open' : 'is-closed'"
          aria-live="polite"
        >
          <p class="online-window-status">
            {{
              isFormWindowOpen
                ? t("elections.onlineWindow.statusOpen")
                : t("elections.onlineWindow.statusClosed")
            }}
          </p>
          <template v-if="hasSummary">
            <p v-if="windowSummary.openLine">{{ windowSummary.openLine }}</p>
            <p v-if="windowSummary.closeLine">{{ windowSummary.closeLine }}</p>
            <p v-if="windowSummary.durationLine">
              {{ windowSummary.durationLine }}
            </p>
          </template>
        </div>

        <div class="online-window-actions">
          <el-button @click="drawerOpen = false">
            {{ t("common.close") }}
          </el-button>
          <el-button type="primary" :loading="saving" @click="saveDates">
            {{ t("common.save") }}
          </el-button>
        </div>

        <div v-if="saveResults" class="saveResults">
          <p>{{ saveResults }}</p>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<style lang="less">
// Base .header-status-box styles live in styles/utilities/box.less

.online-voting-window-drawer.el-drawer {
  width: min(480px, calc(100vw - 32px)) !important;
  height: auto !important;
  left: 50% !important;
  right: auto !important;
  transform: translateX(-50%);
  border-radius: 0 0 12px 12px;
  overflow: hidden;
  box-shadow: var(--el-box-shadow-light);

  .el-drawer__header {
    margin-bottom: 12px;
    padding: 20px 36px 0;
  }

  .el-drawer__body {
    overflow: visible;
    padding: 4px 36px 24px;
  }
}

.online-voting-window-drawer {
  .online-window-body {
    display: flex;
    flex-direction: column;
    gap: 16px;
  }

  .online-window-field {
    label {
      display: block;
      font-weight: 600;
      margin-bottom: 6px;
      color: var(--el-text-color-primary);
      font-size: var(--el-font-size-small);
    }

    &.online-window-estimate {
      display: flex;
      align-items: center;
      justify-content: space-between;
      gap: 12px;

      label {
        margin-bottom: 0;
      }
    }
  }

  .online-window-summary {
    margin: 0;
    padding: 12px 14px;
    border-radius: var(--el-border-radius-base);
    border: 1px solid var(--el-border-color);
    background: var(--el-fill-color-light);
    color: var(--el-text-color-regular);
    font-size: var(--el-font-size-small);
    line-height: 1.45;

    &.is-open {
      border-color: var(--el-color-success);
      background: var(--el-color-success-light-9);
      color: var(--el-color-success-dark-2);
    }

    &.is-closed {
      border-color: var(--el-color-danger);
      background: var(--el-color-danger-light-9);
      color: var(--el-color-danger-dark-2);
    }

    .online-window-status {
      font-weight: 600;
      font-size: var(--el-font-size-base);
    }

    p {
      margin: 0;

      & + p {
        margin-top: 4px;
      }
    }
  }

  .online-window-actions {
    display: flex;
    justify-content: flex-end;
    gap: 8px;
    margin-top: 4px;
  }

  .saveResults {
    margin: 0;
    padding: 10px 12px;
    border-radius: var(--el-border-radius-base);
    background: var(--el-color-success-light-9);
    border: 1px solid var(--el-color-success-light-5);
    color: var(--el-color-success-dark-2);
    font-size: var(--el-font-size-small);

    p {
      margin: 0;
    }
  }
}
</style>
