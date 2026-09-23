import { createPinia } from "pinia";
import { createRouter, createWebHistory } from "vue-router";
import { createI18n } from "vue-i18n";
import { createAppConfig, setAppConfig } from "../config/appConfig";
import { isRichEntry, unwrapMessages } from "../locales/richEntries.js";

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
      if (current[part] == null) {
        current[part] = {};
      } else if (typeof current[part] !== "object") {
        const parent = parts.slice(0, i + 1).join(".");
        throw new Error(
          `i18n key conflict: "${parent}" is a string and cannot have child "${key}"`,
        );
      }
      current = current[part] as Record<string, unknown>;
    }

    const last = parts.at(-1)!;
    if (current[last] && typeof current[last] === "object") {
      throw new Error(
        `i18n key conflict: "${key}" is a string but already has nested keys`,
      );
    }
    current[last] = value;
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

    if (isRichEntry(sourceVal)) {
      result[key] = sourceVal;
    } else if (
      sourceVal &&
      typeof sourceVal === "object" &&
      !Array.isArray(sourceVal) &&
      targetVal &&
      typeof targetVal === "object" &&
      !Array.isArray(targetVal) &&
      !isRichEntry(targetVal)
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
    messages = deepMerge(
      messages,
      flatToNested(unwrapMessages(mod) as Record<string, string>),
    );
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
