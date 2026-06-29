import { createPinia } from "pinia";
import { createRouter, createWebHistory } from "vue-router";
import { createI18n } from "vue-i18n";
import { createAppConfig, setAppConfig } from "../config/appConfig";

setAppConfig(
  createAppConfig({
    env: "test",
    sentryDsn: "",
  }),
);

function flatToNested(flat: Record<string, string>): Record<string, unknown> {
  const result: Record<string, unknown> = {};

  for (const [key, value] of Object.entries(flat)) {
    const parts = key.split(".");
    let current: Record<string, unknown> = result;

    for (let i = 0; i < parts.length - 1; i++) {
      const part = parts[i]!;
      if (!current[part] || typeof current[part] !== "object") {
        current[part] = {};
      }
      current = current[part] as Record<string, unknown>;
    }

    current[parts.at(-1)!] = value;
  }

  return result;
}

function deepMerge(
  target: Record<string, unknown>,
  source: Record<string, unknown>,
): Record<string, unknown> {
  const result = { ...target };

  for (const key of Object.keys(source)) {
    const sourceVal = source[key];
    const targetVal = result[key];

    if (
      sourceVal &&
      typeof sourceVal === "object" &&
      !Array.isArray(sourceVal) &&
      targetVal &&
      typeof targetVal === "object" &&
      !Array.isArray(targetVal)
    ) {
      result[key] = deepMerge(
        targetVal as Record<string, unknown>,
        sourceVal as Record<string, unknown>,
      );
    } else {
      result[key] = sourceVal;
    }
  }

  return result;
}

function buildEnglishMessages(): Record<string, unknown> {
  const modules = import.meta.glob("../locales/en/*.json", {
    eager: true,
    import: "default",
  }) as Record<string, Record<string, string>>;

  let messages: Record<string, unknown> = {};
  for (const mod of Object.values(modules)) {
    messages = deepMerge(messages, flatToNested(mod));
  }

  return messages;
}

const testPinia = createPinia();
const testRouter = createRouter({
  history: createWebHistory(),
  routes: [],
});
const testI18n = createI18n({
  legacy: false,
  globalInjection: true,
  locale: "en",
  fallbackLocale: "en",
  messages: {
    en: buildEnglishMessages(),
  },
});

export { testPinia as pinia, testRouter as router, testI18n as i18n };