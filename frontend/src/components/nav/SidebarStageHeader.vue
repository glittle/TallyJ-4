<script setup lang="ts">
import { STAGE_META, type ElectionStage } from "@/domain/electionStages";
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import StageControl from "./StageControl.vue";

const props = defineProps<{
  electionGuid: string;
  stage: ElectionStage;
}>();

const { t } = useI18n();
const meta = computed(() => STAGE_META[props.stage]);
</script>

<template>
  <section
    class="sidebar-stage-header"
    :style="{
      borderLeft: `4px solid var(${meta.colorVar})`,
      background: `var(${meta.bgVar})`,
    }"
  >
    <div class="ssh-label">{{ t("elections.stage.modeLabel") }}</div>
    <StageControl :election-guid="electionGuid" :stage="stage" />
  </section>
</template>

<style lang="less">
.sidebar-stage-header {
  padding: 10px 12px 10px 16px;

  .ssh-label {
    font-size: 11px;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.5px;
    color: var(--color-gray-500);
    margin-bottom: 8px;
  }
}
</style>
