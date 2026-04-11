<script setup lang="ts">
import { computed, type Component } from "vue";
import {
  WarningFilled,
  CircleCloseFilled,
  InfoFilled,
  RefreshRight,
} from "@element-plus/icons-vue";

interface Props {
  type?: "error" | "warning" | "info";
  title?: string;
  message?: string;
  error?: Error | any;
  details?: string;
  showDetails?: boolean;
  retry?: boolean;
  size?: "small" | "default" | "large";
}

const props = withDefaults(defineProps<Props>(), {
  type: "error",
  title: undefined,
  message: undefined,
  error: undefined,
  details: undefined,
  showDetails: false,
  retry: false,
  size: "default",
});

const emit = defineEmits<{
  retry: [];
}>();

const typeClass = computed(() => `ds-error-display--${props.type}`);
const sizeClass = computed(() => `ds-error-display--${props.size}`);

const iconComponent = computed<Component>(() => {
  const icons = {
    error: CircleCloseFilled,
    warning: WarningFilled,
    info: InfoFilled,
  };
  return icons[props.type];
});

const iconSize = computed(() => {
  const sizes = {
    small: 32,
    default: 48,
    large: 64,
  };
  return sizes[props.size];
});

const defaultTitle = computed(() => {
  const titles = {
    error: "An Error Occurred",
    warning: "Warning",
    info: "Information",
  };
  return titles[props.type];
});

const defaultMessage = computed(() => {
  const messages = {
    error: "Something went wrong. Please try again later.",
    warning: "Please review the following information.",
    info: "Here is some information you should know.",
  };
  return messages[props.type];
});

function handleRetry() {
  emit("retry");
}
</script>

<template>
  <div class="ds-error-display" :class="[typeClass, sizeClass]">
    <div class="ds-error-display__icon">
      <el-icon :size="iconSize">
        <component :is="iconComponent" />
      </el-icon>
    </div>

    <div class="ds-error-display__content">
      <h3 class="ds-error-display__title">{{ title || defaultTitle }}</h3>
      <p class="ds-error-display__message">
        {{ message || error?.message || defaultMessage }}
      </p>

      <details
        v-if="showDetails && (error || details)"
        class="ds-error-display__details"
      >
        <summary>Technical Details</summary>
        <pre><code>{{ details || JSON.stringify(error, null, 2) }}</code></pre>
      </details>
    </div>

    <div v-if="$slots.actions || retry" class="ds-error-display__actions">
      <slot name="actions">
        <el-button v-if="retry" type="primary" @click="handleRetry">
          <el-icon><RefreshRight /></el-icon>
          Retry
        </el-button>
      </slot>
    </div>
  </div>
</template>

<style lang="less">
.ds-error-display {
  display: flex;
  flex-direction: column;
  align-items: center;
  text-align: center;
  padding: var(--spacing-12) var(--spacing-6);
  border-radius: var(--radius-lg);

  &--small {
    padding: var(--spacing-6) var(--spacing-4);
  }

  &--large {
    padding: var(--spacing-16) var(--spacing-8);
  }

  &--error {
    background-color: var(--color-error-50);

    .dark & {
      background-color: rgba(239, 68, 68, 0.1);
    }

    .ds-error-display__icon {
      color: var(--color-error-500);
    }
  }

  &--warning {
    background-color: var(--color-warning-50);

    .dark & {
      background-color: rgba(245, 158, 11, 0.1);
    }

    .ds-error-display__icon {
      color: var(--color-warning-500);
    }
  }

  &--info {
    background-color: var(--color-primary-50);

    .dark & {
      background-color: rgba(59, 130, 246, 0.1);
    }

    .ds-error-display__icon {
      color: var(--color-primary-500);
    }
  }

  &__icon {
    margin-bottom: var(--spacing-4);
  }

  &__content {
    max-width: 600px;
  }

  &__title {
    margin: 0 0 var(--spacing-3) 0;
    font-size: var(--font-size-2xl);
    font-weight: var(--font-weight-semibold);
    color: var(--color-text-primary);
  }

  &__message {
    margin: 0 0 var(--spacing-4) 0;
    font-size: var(--font-size-base);
    color: var(--color-text-secondary);
    line-height: var(--line-height-relaxed);
  }

  &__details {
    margin-top: var(--spacing-4);
    text-align: left;

    summary {
      cursor: pointer;
      font-weight: var(--font-weight-medium);
      color: var(--color-text-secondary);
      padding: var(--spacing-2);

      &:hover {
        color: var(--color-text-primary);
      }
    }

    pre {
      margin: var(--spacing-3) 0;
      padding: var(--spacing-4);
      background: var(--color-gray-100);
      border-radius: var(--radius-md);
      overflow-x: auto;
      font-size: var(--font-size-sm);

      .dark & {
        background: var(--color-gray-800);
      }

      code {
        font-family: var(--font-family-mono);
      }
    }
  }

  &__actions {
    margin-top: var(--spacing-6);
    display: flex;
    gap: var(--spacing-3);
    flex-wrap: wrap;
    justify-content: center;
  }
}
</style>
