import { defineStore } from "pinia";
import { ref } from "vue";

export type Theme = "light" | "dark";

export const useThemeStore = defineStore("theme", () => {
  const theme = ref<Theme>((localStorage.getItem("theme") as Theme) || "light");

  const setTheme = (newTheme: Theme) => {
    theme.value = newTheme;
    localStorage.setItem("theme", newTheme);
    applyTheme(newTheme);
  };

  const toggleTheme = () => {
    const newTheme = theme.value === "light" ? "dark" : "light";
    setTheme(newTheme);
  };

  const applyTheme = (theme: Theme) => {
    const root = document.documentElement;
    const isDark = theme === "dark";
    if (isDark) {
      root.classList.add("dark");
    } else {
      root.classList.remove("dark");
    }

    // Exactly "light" or "dark". "light dark" would let the browser follow the OS.
    document
      .querySelector('meta[name="color-scheme"]')
      ?.setAttribute("content", isDark ? "dark" : "light");
  };

  // Apply theme on store initialization
  applyTheme(theme.value);

  return {
    theme,
    setTheme,
    toggleTheme,
  };
});
