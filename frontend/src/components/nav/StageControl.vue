<script setup lang="ts">
import { useNotifications } from "@/composables/useNotifications";
import {
  STAGES,
  STAGE_META,
  type ElectionStage,
} from "@/domain/electionStages";
import { useElectionStore } from "@/stores/electionStore";
import { extractApiErrorMessage } from "@/utils/errorHandler";
import { translateElectionStageChangeError } from "@/utils/electionStageErrorMessages";
import { ElIcon } from "element-plus";
import { useI18n } from "vue-i18n";

const props = defineProps<{
  electionGuid: string;
  stage: ElectionStage;
}>();

const { t } = useI18n();
const electionStore = useElectionStore();
const { showSuccessMessage, showErrorMessage } = useNotifications();

async function selectStage(newStage: ElectionStage) {
  if (newStage === props.stage) {
    return;
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
  </div>
</template>

<style lang="less">
.stage-control {
  display: inline-flex;
  width: 100%;
  align-items: stretch;
  border-radius: 6px;
  overflow: hidden;
  border: 1px solid #dcdfe6;
  flex-direction: column;
  gap: 10px;

  &__seg {
    display: inline-flex;
    align-items: center;
    gap: 6px;
    padding: 6px 14px;
    background: #fff;
    border: none;
    border-bottom: 1px solid #dcdfe6;
    cursor: pointer;
    font-size: 13px;
    font-weight: 500;
    color: #606266;
    transition:
      background 0.15s,
      color 0.15s;

    &:last-child {
      border-bottom: none;
    }

    &:hover:not(.is-selected) {
      background: #f5f7fa;
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
</style>
