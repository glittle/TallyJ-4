<template>
  <DSCard
    :variant="variant"
    :hoverable="hoverable"
    :loading="loading"
    class="chart-card"
  >
    <template #header>
      <div class="chart-card__header">
        <div>
          <h3 class="chart-card__title">{{ title }}</h3>
          <p v-if="subtitle" class="chart-card__subtitle">{{ subtitle }}</p>
        </div>
        <slot name="actions" />
      </div>
    </template>

    <div class="chart-card__content" :style="{ height: height }">
      <slot />
    </div>

    <template v-if="$slots.footer" #footer>
      <slot name="footer" />
    </template>
  </DSCard>
</template>

<script setup lang="ts">
import DSCard from "../ds/DSCard.vue";

interface Props {
  title: string;
  subtitle?: string;
  height?: string;
  variant?: "default" | "gradient" | "elevated" | "outlined" | "flat";
  hoverable?: boolean;
  loading?: boolean;
}

withDefaults(defineProps<Props>(), {
  subtitle: undefined,
  height: "300px",
  variant: "default",
  hoverable: false,
  loading: false,
});
</script>

<style lang="less">
.chart-card {
  &__header {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    gap: var(--spacing-4);
  }

  &__title {
    margin: 0;
    font-size: var(--font-size-lg);
    font-weight: var(--font-weight-semibold);
    color: var(--color-text-primary);
  }

  &__subtitle {
    margin: var(--spacing-1) 0 0 0;
    font-size: var(--font-size-sm);
    color: var(--color-text-secondary);
  }

  &__content {
    position: relative;
    width: 100%;
    min-height: 200px;
  }
}
</style>
