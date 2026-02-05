import { createI18n } from "vue-i18n";
import en from "./en.json";
import fr from "./fr.json";
import shared from "./shared.json";

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

const savedLocale = localStorage.getItem("preferred-language") || "en";

console.log("Saved locale:", savedLocale);

export const i18n = createI18n({
  locale: savedLocale,
  fallbackLocale: "en",
  messages: {
    en: deepMerge(shared, flatToNested(en)),
    fr: deepMerge(shared, flatToNested(fr)),
  },
});

export function setLocale(locale: "en" | "fr") {
  i18n.global.locale = locale;
  localStorage.setItem("preferred-language", locale);
}
