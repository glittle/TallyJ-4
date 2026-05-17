<script setup lang="ts">
import { computed } from "vue";
import { useI18n } from "vue-i18n";
import { ElIcon } from "element-plus";
import { Close } from "@element-plus/icons-vue";
import { useNavUiStore } from "@/stores/navUiStore";

const props = defineProps<{
  tipId: string;
}>();

const { t } = useI18n();
const navUiStore = useNavUiStore();

const isDismissed = computed(() => navUiStore.isTipDismissed(props.tipId));

function dismiss() {
  navUiStore.dismissTip(props.tipId);
}
</script>

<template>
  <div v-if="!isDismissed" class="tips-panel">
    <div class="tips-panel__header">
      <slot name="header" />
      <button
        class="tips-panel__dismiss"
        :aria-label="t('dashboard.tips.dismiss')"
        @click="dismiss"
      >
        <el-icon><Close /></el-icon>
      </button>
    </div>
    <div class="tips-panel__body">
      <slot />
    </div>
  </div>
</template>

<style lang="less" scoped>
.tips-panel {
  background: var(--color-bg-secondary, #f5f7fa);
  border: 1px solid var(--color-border-light, #e4e7ed);
  border-radius: 6px;
  padding: 12px 14px;
  font-size: 12px;
  color: var(--color-text-secondary, #909399);

  &__header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 8px;
    gap: 8px;
  }

  &__dismiss {
    flex-shrink: 0;
    background: none;
    border: none;
    cursor: pointer;
    color: var(--color-text-secondary, #909399);
    padding: 2px;
    display: flex;
    align-items: center;
    line-height: 1;
    border-radius: 3px;
    transition: color 0.15s;

    &:hover {
      color: var(--color-text-primary, #303133);
    }
  }

  &__body {
    line-height: 1.6;
  }
}
</style>
