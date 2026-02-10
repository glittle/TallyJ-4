import { createI18n } from "vue-i18n";
import common from "./common.json";

// Utility function to deep merge objects
function deepMerge(target: any, source: any): any {
  const result = { ...target };
  for (const key in source) {
    if (
      source[key] &&
      typeof source[key] === "object" &&
      !Array.isArray(source[key])
    ) {
      result[key] = deepMerge(result[key] || {}, source[key]);
    } else {
      result[key] = source[key];
    }
  }
  return result;
}

// Utility function to convert flat dotted keys to nested objects
function flatToNested(flat: any): any {
  const result: any = {};
  for (const key in flat) {
    const keys = key.split(".");
    let current = result;
    for (let i = 0; i < keys.length - 1; i++) {
      const k = keys[i]!;
      if (!current[k]) {
        current[k] = {};
      }
      current = current[k];
    }
    current[keys[keys.length - 1]!] = flat[key];
  }
  return result;
}

// Load all locale files using Vite's glob import
const enModules = import.meta.glob("./en/*.json", { eager: true });
const frModules = import.meta.glob("./fr/*.json", { eager: true });

// Merge all JSON files for a locale
function mergeLocaleFiles(modules: Record<string, any>): any {
  let merged = {};
  for (const path in modules) {
    const content = modules[path].default || modules[path];
    merged = deepMerge(merged, content);
  }
  return flatToNested(merged);
}

const savedLocale = localStorage.getItem("preferred-language") || "en";

console.log("Saved locale:", savedLocale);

export const i18n = createI18n({
  locale: savedLocale,
  fallbackLocale: "en",
  messages: {
    en: deepMerge(common, mergeLocaleFiles(enModules)),
    fr: deepMerge(common, mergeLocaleFiles(frModules)),
  },
});

export function setLocale(locale: "en" | "fr") {
  i18n.global.locale = locale;
  localStorage.setItem("preferred-language", locale);
}
