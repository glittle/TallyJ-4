<template>
  <div class="ds-loading-state" :class="sizeClass">
    <div v-if="type === 'spinner'" class="ds-loading-state__spinner">
      <el-icon class="is-loading" :size="iconSize">
        <Loading />
      </el-icon>
      <p v-if="text" class="ds-loading-state__text">{{ text }}</p>
    </div>

    <div v-else-if="type === 'skeleton'" class="ds-loading-state__skeleton">
      <el-skeleton :rows="rows" animated />
    </div>

    <div v-else-if="type === 'card'" class="ds-loading-state__card">
      <el-skeleton :rows="rows" animated>
        <template #template>
          <el-skeleton-item
            variant="image"
            style="width: 100%; height: 240px"
          />
          <div style="padding: 14px">
            <el-skeleton-item variant="h3" style="width: 50%" />
            <div style="padding: 14px 0">
              <el-skeleton-item variant="text" style="margin-right: 16px" />
              <el-skeleton-item variant="text" style="width: 30%" />
            </div>
          </div>
        </template>
      </el-skeleton>
    </div>

    <div v-else-if="type === 'progress'" class="ds-loading-state__progress">
      <el-progress
        :percentage="percentage"
        :status="progressStatus"
        :stroke-width="strokeWidth"
      />
      <p v-if="text" class="ds-loading-state__text">{{ text }}</p>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { Loading } from "@element-plus/icons-vue";

interface Props {
  type?: "spinner" | "skeleton" | "card" | "progress";
  size?: "small" | "default" | "large";
  text?: string;
  rows?: number;
  percentage?: number;
  strokeWidth?: number;
}

const props = withDefaults(defineProps<Props>(), {
  type: "spinner",
  size: "default",
  text: undefined,
  rows: 5,
  percentage: 0,
  strokeWidth: 6,
});

const sizeClass = computed(() => `ds-loading-state--${props.size}`);

const iconSize = computed(() => {
  const sizes = {
    small: 24,
    default: 32,
    large: 48,
  };
  return sizes[props.size];
});

const progressStatus = computed(() => {
  if (props.percentage === 100) return "success";
  if (props.percentage < 0) return "exception";
  return undefined;
});
</script>

<style lang="less">
.ds-loading-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: var(--spacing-12) var(--spacing-6);
  min-height: 200px;

  &--small {
    padding: var(--spacing-6) var(--spacing-4);
    min-height: 100px;
  }

  &--large {
    padding: var(--spacing-16) var(--spacing-8);
    min-height: 300px;
  }

  &__spinner {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: var(--spacing-4);

    .el-icon {
      color: var(--color-primary-500);
    }
  }

  &__skeleton {
    width: 100%;
    padding: var(--spacing-6);
  }

  &__card {
    width: 100%;
    max-width: 600px;
  }

  &__progress {
    width: 100%;
    max-width: 600px;
    text-align: center;
  }

  &__text {
    margin: 0;
    font-size: var(--font-size-base);
    color: var(--color-text-secondary);
    font-weight: var(--font-weight-medium);
  }
}
</style>
