import { computed } from "vue";
import { useThemeStore, type Theme } from "../stores/themeStore";

export function useTheme() {
  const themeStore = useThemeStore();

  const theme = computed(() => themeStore.theme);
  const isDark = computed(() => themeStore.theme === "dark");
  const isLight = computed(() => themeStore.theme === "light");

  const setTheme = (newTheme: Theme) => {
    themeStore.setTheme(newTheme);
  };

  const toggleTheme = () => {
    themeStore.toggleTheme();
  };

  const prefersDark = computed(() => {
    if (typeof window !== "undefined" && window.matchMedia) {
      return window.matchMedia("(prefers-color-scheme: dark)").matches;
    }
    return false;
  });

  const setSystemTheme = () => {
    setTheme(prefersDark.value ? "dark" : "light");
  };

  return {
    theme,
    isDark,
    isLight,
    prefersDark,
    setTheme,
    toggleTheme,
    setSystemTheme,
  };
}
