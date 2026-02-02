<template>
  <el-button
    :class="['ds-button', variantClass, sizeClass]"
    :type="type"
    :size="size"
    :loading="loading"
    :disabled="disabled"
    :icon="icon"
    v-bind="$attrs"
    @click="handleClick"
  >
    <slot />
  </el-button>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { Component } from 'vue';

interface Props {
  variant?: 'solid' | 'gradient' | 'outlined' | 'text' | 'ghost';
  type?: 'primary' | 'success' | 'warning' | 'danger' | 'info' | 'default';
  size?: 'large' | 'default' | 'small';
  loading?: boolean;
  disabled?: boolean;
  icon?: Component;
}

const props = withDefaults(defineProps<Props>(), {
  variant: 'solid',
  type: 'default',
  size: 'default',
  loading: false,
  disabled: false,
  icon: undefined
});

const emit = defineEmits<{
  click: [event: MouseEvent];
}>();

const variantClass = computed(() => `ds-button--${props.variant}`);
const sizeClass = computed(() => `ds-button--${props.size}`);

function handleClick(event: MouseEvent) {
  if (!props.loading && !props.disabled) {
    emit('click', event);
  }
}
</script>

<style lang="less">
.ds-button {
  transition: all var(--transition-normal);
  font-weight: var(--font-weight-medium);
  border-radius: var(--radius-md);
  
  &--gradient {
    background: linear-gradient(135deg, var(--color-primary-500) 0%, var(--color-primary-700) 100%);
    border: none;
    color: white;
    
    &:hover:not(:disabled) {
      background: linear-gradient(135deg, var(--color-primary-400) 0%, var(--color-primary-600) 100%);
      transform: translateY(-2px);
      box-shadow: 0 4px 12px rgba(37, 99, 235, 0.3);
    }
  }
  
  &--outlined {
    background: transparent;
    
    &:hover:not(:disabled) {
      transform: translateY(-2px);
    }
  }
  
  &--text {
    background: transparent;
    border: none;
    
    &:hover:not(:disabled) {
      background: var(--color-gray-100);
      
      .dark & {
        background: var(--color-gray-800);
      }
    }
  }
  
  &--ghost {
    background: rgba(255, 255, 255, 0.1);
    border: 1px solid rgba(255, 255, 255, 0.2);
    color: white;
    
    &:hover:not(:disabled) {
      background: rgba(255, 255, 255, 0.2);
      border-color: rgba(255, 255, 255, 0.3);
    }
  }
  
  &--large {
    padding: var(--spacing-4) var(--spacing-6);
    font-size: var(--font-size-lg);
  }
  
  &--small {
    padding: var(--spacing-2) var(--spacing-3);
    font-size: var(--font-size-sm);
  }
  
  &:not(:disabled):active {
    transform: scale(0.98);
  }
}
</style>
