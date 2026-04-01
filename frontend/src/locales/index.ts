import { createI18n } from "vue-i18n";
import { nextTick } from "vue";
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

// Load English synchronously as it is the fallback language
const enModules = import.meta.glob("./en/*.json", { eager: true });

// Load other languages dynamically
const localeModulesAsync = import.meta.glob("./*/*.json");

// Merge all JSON files for a locale
function mergeLocaleFiles(modules: Record<string, any>): any {
  let merged = {};

  for (const path in modules) {
    const content = modules[path].default || modules[path];
    merged = deepMerge(merged, content);
  }

  return flatToNested(merged);
}

const supportedLocales = [
  "en",
  "fr",
  "fi",
  "ko",
  "es",
  "pt",
  "hi",
  "vi",
  "fa",
  "sw",
  "ar",
  "zh",
  "ru",
] as const;

export type SupportedLocale = (typeof supportedLocales)[number];

function getBestLocale(): SupportedLocale {
  const saved = localStorage.getItem("preferred-language");

  if (saved && (supportedLocales as readonly string[]).includes(saved)) {
    return saved as SupportedLocale;
  }

  if (typeof navigator !== "undefined") {
    const browserLanguages =
      navigator.languages || (navigator.language ? [navigator.language] : []);

    for (const lang of browserLanguages) {
      if (!lang) {
        continue;
      }

      const lowerLang = lang.toLowerCase();

      if ((supportedLocales as readonly string[]).includes(lowerLang)) {
        return lowerLang as SupportedLocale;
      }

      const baseLang = lowerLang.split("-")[0];

      if (
        baseLang &&
        (supportedLocales as readonly string[]).includes(baseLang)
      ) {
        return baseLang as SupportedLocale;
      }
    }
  }

  return "en";
}

const savedLocale = getBestLocale();
console.log("Saved locale:", savedLocale);

export const i18n = createI18n({
  locale: "en", // Start with English, we will switch it after loading
  fallbackLocale: "en",
  messages: {
    en: deepMerge(common, mergeLocaleFiles(enModules)),
  },
});

export async function loadLocaleMessages(locale: SupportedLocale) {
  // If the language is already loaded
  if (i18n.global.availableLocales.includes(locale)) {
    return nextTick();
  }

  const regex = new RegExp(`^\\./${locale}/.*\\.json$`);
  let merged = {};
  const loadPromises = [];

  for (const path in localeModulesAsync) {
    if (regex.test(path)) {
      loadPromises.push(
        localeModulesAsync[path]().then((mod: any) => {
          const content = mod.default || mod;
          merged = deepMerge(merged, content);
        }),
      );
    }
  }

  await Promise.all(loadPromises);

  const finalMessages = flatToNested(deepMerge(common, merged));
  i18n.global.setLocaleMessage(locale, finalMessages);
  return nextTick();
}

export async function setLocale(locale: SupportedLocale) {
  await loadLocaleMessages(locale);
  i18n.global.locale = locale;
  localStorage.setItem("preferred-language", locale);
  document.querySelector("html")?.setAttribute("lang", locale);
}

// Load the saved locale
if (savedLocale !== "en") {
  setLocale(savedLocale);
}
