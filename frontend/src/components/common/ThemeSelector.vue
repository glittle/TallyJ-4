<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { ElButton, ElIcon } from "element-plus";
import { Sunny, Moon } from "@element-plus/icons-vue";
import { useThemeStore } from "../../stores/themeStore";

useI18n();
const themeStore = useThemeStore();

const toggleTheme = () => {
  themeStore.toggleTheme();
};
</script>

<template>
  <div class="theme-selector">
    <ElButton
      @click="toggleTheme"
      :aria-label="$t('common.toggleTheme', 'Toggle Theme')"
      size="small"
      class="theme-toggle-btn"
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
  border-radius: 4px;
  transition: background-color 0.2s ease;

  &:hover {
    background-color: var(--el-color-info-light-9);
  }

  &:focus {
    outline: 2px solid var(--el-color-primary);
    outline-offset: 2px;
  }

  .el-icon {
    color: var(--el-text-color-primary);
    font-size: 18px;
  }
}
</style>
