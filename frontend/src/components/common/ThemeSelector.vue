<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { ElButton, ElIcon } from "element-plus";
import { Sunny, Moon } from "@element-plus/icons-vue";
import { useThemeStore } from "../../stores/themeStore";
import { useDark } from "@vueuse/core";
import { watch } from "vue";

const { t } = useI18n();
const themeStore = useThemeStore();

// Integrate useDark with the theme store
const isDark = useDark({
  storageKey: "theme",
  valueDark: "dark",
  valueLight: "light",
  selector: "html",
  attribute: "class",
  storage: localStorage,
});

// Sync theme store with useDark
watch(
  () => themeStore.theme,
  (newTheme) => {
    isDark.value = newTheme === "dark";
  },
  { immediate: true }
);

// Sync useDark with theme store
watch(
  isDark,
  (newIsDark) => {
    if (themeStore.theme !== (newIsDark ? "dark" : "light")) {
      themeStore.setTheme(newIsDark ? "dark" : "light");
    }
  }
);

const toggleTheme = () => {
  themeStore.setTheme(themeStore.theme === "dark" ? "light" : "dark");
};
</script>

<template>
  <div class="theme-selector">
    <ElButton
      @click="toggleTheme"
      :aria-label="$t('common.toggleTheme')"
      size="small"
      type="text"
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