<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { ElButton, ElIcon } from "element-plus";
import type { ComponentSize } from "element-plus";
import { Sunny, Moon } from "@element-plus/icons-vue";
import { useThemeStore } from "../../stores/themeStore";

withDefaults(
  defineProps<{
    size?: ComponentSize;
  }>(),
  {
    size: "default",
  },
);

useI18n();
const themeStore = useThemeStore();

const toggleTheme = () => {
  themeStore.toggleTheme();
};
</script>

<template>
  <div class="theme-selector">
    <ElButton
      :aria-label="$t('common.toggleTheme')"
      :size="size"
      text
      class="theme-toggle-btn"
      @click="toggleTheme"
    >
      <ElIcon>
        <Sunny v-if="themeStore.theme === 'light'" />
        <Moon v-else />
      </ElIcon>
    </ElButton>
  </div>
</template>

<style lang="less">
.theme-selector {
  display: inline-block;
}

.theme-toggle-btn {
  padding: 8px;
  border-radius: var(--el-border-radius-base);
  transition:
    border-color 0.2s ease,
    background-color 0.2s ease;

  .el-icon {
    color: var(--el-text-color-primary);
    font-size: 18px;
  }
}
</style>
