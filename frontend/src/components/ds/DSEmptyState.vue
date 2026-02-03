<template>
  <div class="ds-empty-state" :class="sizeClass">
    <div class="ds-empty-state__icon" v-if="icon || $slots.icon">
      <slot name="icon">
        <el-icon :size="iconSize">
          <component :is="icon" />
        </el-icon>
      </slot>
    </div>
    
    <div class="ds-empty-state__content">
      <h3 v-if="title" class="ds-empty-state__title">{{ title }}</h3>
      <p v-if="description" class="ds-empty-state__description">{{ description }}</p>
      <slot />
    </div>
    
    <div v-if="$slots.actions" class="ds-empty-state__actions">
      <slot name="actions" />
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, type Component } from 'vue';

interface Props {
  title?: string;
  description?: string;
  icon?: Component;
  size?: 'small' | 'default' | 'large';
}

const props = withDefaults(defineProps<Props>(), {
  title: undefined,
  description: undefined,
  icon: undefined,
  size: 'default'
});

const sizeClass = computed(() => `ds-empty-state--${props.size}`);

const iconSize = computed(() => {
  const sizes = {
    small: 48,
    default: 64,
    large: 80
  };
  return sizes[props.size];
});
</script>

<style lang="less">
.ds-empty-state {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  text-align: center;
  padding: var(--spacing-12) var(--spacing-6);
  
  &--small {
    padding: var(--spacing-8) var(--spacing-4);
  }
  
  &--large {
    padding: var(--spacing-16) var(--spacing-8);
  }
  
  &__icon {
    margin-bottom: var(--spacing-6);
    color: var(--color-gray-400);
    opacity: 0.6;
    
    .dark & {
      color: var(--color-gray-600);
    }
  }
  
  &__content {
    max-width: 480px;
  }
  
  &__title {
    margin: 0 0 var(--spacing-3) 0;
    font-size: var(--font-size-2xl);
    font-weight: var(--font-weight-semibold);
    color: var(--color-text-primary);
  }
  
  &__description {
    margin: 0 0 var(--spacing-6) 0;
    font-size: var(--font-size-base);
    color: var(--color-text-secondary);
    line-height: var(--line-height-relaxed);
  }
  
  &__actions {
    display: flex;
    gap: var(--spacing-3);
    flex-wrap: wrap;
    justify-content: center;
  }
}
</style>
