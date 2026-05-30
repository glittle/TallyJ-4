<script setup lang="ts">
import type { ElectionStage } from "@/domain/electionStages";
import { STAGE_META, STAGES } from "@/domain/electionStages";
import { ElIcon } from "element-plus";
import { computed } from "vue";
import { useI18n } from "vue-i18n";

const props = withDefaults(
  defineProps<{
    stage: ElectionStage;
    size?: "sm" | "md" | "lg";
    showStepCount?: boolean;
  }>(),
  {
    size: "md",
    showStepCount: false,
  },
);

const { t } = useI18n();
const meta = computed(() => STAGE_META[props.stage]);
const stepNumber = computed(() => STAGES.indexOf(props.stage) + 1);
</script>

<template>
  <span
    class="stage-indicator"
    :class="`stage-indicator--${size}`"
    role="status"
    aria-live="polite"
    :style="{
      color: `var(${meta.colorVar})`,
      backgroundColor: `var(${meta.bgVar})`,
    }"
  >
    <el-icon class="stage-indicator__icon">
      <component :is="meta.icon" />
    </el-icon>
    <span class="stage-indicator__name">{{ t(meta.i18nKey) }}</span>
    <span v-if="showStepCount" class="stage-indicator__count">
      {{ stepNumber }}/{{ STAGES.length }}
    </span>
  </span>
</template>

<style lang="less">
.stage-indicator {
  display: inline-flex;
  align-items: center;
  gap: 4px;
  padding: 2px 8px;
  border-radius: 4px;
  font-weight: 600;
  line-height: 1.4;

  &--sm {
    font-size: 11px;
    padding: 1px 6px;
  }

  &--md {
    font-size: 13px;
  }

  &--lg {
    font-size: 15px;
    padding: 4px 12px;
  }

  &__icon {
    flex-shrink: 0;
  }

  &__name {
    white-space: nowrap;
  }

  &__count {
    font-weight: 400;
    opacity: 0.75;
    font-size: 0.85em;
  }
}
</style>
