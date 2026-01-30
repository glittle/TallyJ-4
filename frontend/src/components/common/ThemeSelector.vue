<script setup lang="ts">
import { useI18n } from "vue-i18n";
import { ElSelect, ElOption } from "element-plus";
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

const themes = [
  { value: "light", label: t("common.light") },
  { value: "dark", label: t("common.dark") },
];

const changeTheme = (theme: string) => {
  themeStore.setTheme(theme as "light" | "dark");
};
</script>

<template>
  <div class="theme-selector">
    <label for="theme-select" class="sr-only">{{ $t("common.theme") }}</label>
    <ElSelect
      id="theme-select"
      :model-value="themeStore.theme"
      @update:model-value="changeTheme"
      size="small"
      style="width: 100px"
      aria-label="Select theme"
    >
      <ElOption
        v-for="theme in themes"
        :key="theme.value"
        :label="theme.label"
        :value="theme.value"
      />
    </ElSelect>
  </div>
</template>

<style lang="less">
.theme-selector {
  display: inline-block;
}
</style>