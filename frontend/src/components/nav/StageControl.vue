<script setup lang="ts">
import ReconciliationReportPanel from "@/components/results/ReconciliationReportPanel.vue";
import { useConfirmDialog } from "@/composables/useConfirmDialog";
import { useNotifications } from "@/composables/useNotifications";
import {
  STAGES,
  STAGE_META,
  type ElectionStage,
} from "@/domain/electionStages";
import { resultService } from "@/services/resultService";
import { useElectionStore } from "@/stores/electionStore";
import type { CountReconciliationReportDto } from "@/types";
import { extractApiErrorMessage } from "@/utils/errorHandler";
import { translateElectionStageChangeError } from "@/utils/electionStageErrorMessages";
import { isOnlineVotingCurrentlyOpen } from "@/utils/onlineVotingWindowOpen";
import { ElIcon } from "element-plus";
import { computed, ref } from "vue";
import { useI18n } from "vue-i18n";

const props = defineProps<{
  electionGuid: string;
  stage: ElectionStage;
}>();

const { t } = useI18n();
const electionStore = useElectionStore();
const { confirm } = useConfirmDialog();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const finalizeReportVisible = ref(false);
const finalizeReport = ref<CountReconciliationReportDto | null>(null);

const onlineWindowBlocksFinalize = computed(() => {
  const election = electionStore.currentElection;
  if (!election || election.electionGuid !== props.electionGuid) {
    return false;
  }
  return isOnlineVotingCurrentlyOpen(election);
});

async function selectStage(newStage: ElectionStage) {
  if (newStage === props.stage) {
    return;
  }

  let confirmLeavingFinalized = false;
  if (props.stage === "Finalized") {
    const confirmed = await confirm({
      title: t("elections.leaveFinalized.title"),
      message: t("elections.leaveFinalized.message", {
        stage: t(STAGE_META[newStage].i18nKey),
      }),
      confirmButtonText: t("elections.leaveFinalized.confirm"),
      type: "warning",
    });
    if (!confirmed) {
      return;
    }
    confirmLeavingFinalized = true;
  }

  if (newStage === "Finalized") {
    if (onlineWindowBlocksFinalize.value) {
      showErrorMessage(t("elections.stageChangeError.onlineVotingStillOpen"));
      return;
    }

    try {
      const report = await resultService.getCountReconciliation(
        props.electionGuid,
      );
      if (!report.isReconciled) {
        finalizeReport.value = report;
        finalizeReportVisible.value = true;
        showErrorMessage(t("tally.reconciliation.finalizeBlocked"));
        return;
      }
    } catch (error) {
      showErrorMessage(extractApiErrorMessage(error));
      return;
    }
  }

  try {
    await electionStore.setStage(
      props.electionGuid,
      newStage,
      confirmLeavingFinalized,
    );
    showSuccessMessage(
      t("elections.stageAdvanced", {
        stage: t(STAGE_META[newStage].i18nKey),
      }),
    );
  } catch (error) {
    const serverMessage = extractApiErrorMessage(error);
    showErrorMessage(translateElectionStageChangeError(serverMessage, t));
  }
}
</script>

<template>
  <div
    class="stage-control"
    role="radiogroup"
    :aria-label="t('elections.stage.modeLabel')"
  >
    <button
      v-for="s in STAGES"
      :key="s"
      role="radio"
      :aria-checked="s === stage"
      :disabled="s === 'Finalized' && onlineWindowBlocksFinalize"
      :title="
        s === 'Finalized' && onlineWindowBlocksFinalize
          ? t('elections.stageChangeError.onlineVotingStillOpen')
          : undefined
      "
      class="stage-control__seg"
      :class="{ 'is-selected': s === stage }"
      :style="
        s === stage
          ? {
              background: `var(${STAGE_META[s].colorVar})`,
              color: '#fff',
              borderColor: `var(${STAGE_META[s].colorVar})`,
            }
          : {}
      "
      @click="selectStage(s)"
    >
      <el-icon class="stage-control__seg-icon">
        <component :is="STAGE_META[s].icon" />
      </el-icon>
      <span>{{ t(STAGE_META[s].i18nKey) }}</span>
    </button>

    <el-dialog
      v-model="finalizeReportVisible"
      :title="t('tally.reconciliation.title')"
      width="720px"
    >
      <ReconciliationReportPanel :report="finalizeReport" />
    </el-dialog>
  </div>
</template>

<style lang="less">
.stage-control {
  display: inline-flex;
  width: 100%;
  align-items: stretch;
  border-radius: 6px;
  overflow: hidden;
  border: 1px solid var(--el-border-color);
  flex-direction: column;
  gap: 10px;

  &__seg {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 6px 14px;
    background: var(--el-fill-color-blank);
    border: none;
    border-bottom: 1px solid var(--el-border-color);
    cursor: pointer;
    font-size: 13px;
    font-weight: 500;
    color: var(--el-text-color-regular);
    transition:
      background 0.15s,
      color 0.15s;

    &:last-child {
      border-bottom: none;
    }

    &:hover:not(.is-selected):not(:disabled) {
      background: var(--el-fill-color-light);
    }

    &:disabled {
      cursor: not-allowed;
      opacity: 0.55;
    }

    &.is-selected {
      color: #fff;
      font-weight: 600;
    }

    &-icon {
      flex-shrink: 0;
    }
  }
}

html.dark .stage-control__seg:not(.is-selected) {
  color: var(--color-sidebar-text);
}
</style>
