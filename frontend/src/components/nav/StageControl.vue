<script setup lang="ts">
import ReconciliationReportPanel from "@/components/results/ReconciliationReportPanel.vue";
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
import { ElIcon } from "element-plus";
import { ref } from "vue";
import { useI18n } from "vue-i18n";

const props = defineProps<{
  electionGuid: string;
  stage: ElectionStage;
}>();

const { t } = useI18n();
const electionStore = useElectionStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

const finalizeReportVisible = ref(false);
const finalizeReport = ref<CountReconciliationReportDto | null>(null);

async function selectStage(newStage: ElectionStage) {
  if (newStage === props.stage) {
    return;
  }

  if (newStage === "Finalized") {
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
    await electionStore.setStage(props.electionGuid, newStage);
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

    &:hover:not(.is-selected) {
      background: var(--el-fill-color-light);
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
