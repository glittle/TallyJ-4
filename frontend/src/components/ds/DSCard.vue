<script setup lang="ts">
import { computed, type CSSProperties } from "vue";

interface Props {
  variant?: "default" | "gradient" | "elevated" | "outlined" | "flat";
  hoverable?: boolean;
  loading?: boolean;
  loadingRows?: number;
  shadow?: "always" | "hover" | "never";
  padding?: string | number;
}

const props = withDefaults(defineProps<Props>(), {
  variant: "default",
  hoverable: false,
  loading: false,
  loadingRows: 3,
  shadow: "hover",
  padding: undefined,
});

const variantClass = computed(() => `ds-card--${props.variant}`);
const shadowType = computed(() => props.shadow);

const bodyStyle = computed<CSSProperties | undefined>(() => {
  if (props.padding !== undefined) {
    const paddingValue =
      typeof props.padding === "number" ? `${props.padding}px` : props.padding;
    return { padding: paddingValue };
  }
  return undefined;
});
</script>

<template>
  <el-card
    :class="[
      'ds-card',
      variantClass,
      { 'ds-card--hoverable': hoverable, 'ds-card--loading': loading },
    ]"
    :shadow="shadowType"
    :body-style="bodyStyle"
    v-bind="$attrs"
  >
    <template v-if="$slots.header" #header>
      <div class="ds-card__header">
        <slot name="header" />
      </div>
    </template>

    <div v-if="loading" class="ds-card__loading">
      <el-skeleton :rows="loadingRows" animated />
    </div>

    <slot v-else />

    <template v-if="$slots.footer" #footer>
      <div class="ds-card__footer">
        <slot name="footer" />
      </div>
    </template>
  </el-card>
</template>

<style lang="less">
.ds-card {
  transition: var(--transition-normal);
  border-radius: var(--radius-lg);

  &--default {
    background: var(--color-bg-primary);
  }

  &--gradient {
    background: linear-gradient(
      135deg,
      var(--color-primary-50) 0%,
      var(--color-primary-100) 100%
    );
    border: none;

    .dark & {
      background: linear-gradient(
        135deg,
        var(--color-gray-800) 0%,
        var(--color-gray-700) 100%
      );
    }
  }

  &--elevated {
    box-shadow: var(--shadow-lg);
  }

  &--outlined {
    border: 2px solid var(--color-gray-300);
    box-shadow: none;

    .dark & {
      border-color: var(--color-gray-700);
    }
  }

  &--flat {
    box-shadow: none;
    border: none;
  }

  &--hoverable:hover {
    transform: translateY(-4px);
    box-shadow: var(--shadow-xl);
    cursor: pointer;
  }

  &--loading {
    min-height: 200px;
  }

  &__header {
    display: flex;
    justify-content: space-between;
    align-items: center;
  }

  &__footer {
    display: flex;
    justify-content: flex-end;
    gap: var(--spacing-3);
  }

  &__loading {
    padding: var(--spacing-6);
  }
}
</style>
