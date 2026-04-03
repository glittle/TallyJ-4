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

// Check if we're in production build (bundled files should exist)
const isProduction = import.meta.env.PROD || import.meta.env.MODE === 'production';

// Load English synchronously, others lazily
const enBundledModule = import.meta.glob("./bundled/en.json", { eager: true });
const enModules = isProduction ? {} : import.meta.glob("./en/*.json", { eager: true });
const localeModulesAsync = import.meta.glob("./bundled/*.json");

// Get English content synchronously (always available)
function getEnglishContent(): any {
  if (isProduction) {
    // Production: use bundled English file
    const content = enBundledModule["./bundled/en.json"]?.default || enBundledModule["./bundled/en.json"];
    return content ? flatToNested(content) : {};
  } else {
    // Development: merge individual English files
    let merged = {};
    for (const path in enModules) {
      const content = enModules[path].default || enModules[path];
      merged = deepMerge(merged, content);
    }
    return flatToNested(merged);
  }
}

// Get other locale content (async for dynamic loading)
async function getLocaleContent(locale: string): any {
  if (isProduction) {
    // Production: load bundled file dynamically
    const path = `./bundled/${locale}.json`;
    const loadFn = localeModulesAsync[path];
    if (loadFn) {
      const mod = await loadFn();
      const content = mod.default || mod;
      return content ? flatToNested(content) : {};
    }
    return {};
  } else {
    // Development: dynamically load and merge individual files
    const individualModules = import.meta.glob("./*/*.json");
    const localeModules = Object.keys(individualModules)
      .filter(path => path.startsWith(`./${locale}/`) && path.endsWith('.json'));

    let merged = {};
    for (const path of localeModules) {
      const loadFn = individualModules[path];
      if (loadFn) {
        const mod = await loadFn();
        const content = mod.default || mod;
        if (content) {
          merged = deepMerge(merged, content);
        }
      }
    }
    return flatToNested(merged);
  }
}

export const supportedLocales = [
  { value: "en", flag: "us", name: "english" },
  { value: "fr", flag: "fr", name: "french" },
  { value: "fi", flag: "fi", name: "finnish" },
  { value: "ko", flag: "kr", name: "korean" },
  { value: "es", flag: "es", name: "spanish" },
  { value: "pt", flag: "br", name: "portuguese" },
  { value: "hi", flag: "in", name: "hindi" },
  { value: "vi", flag: "vn", name: "vietnamese" },
  { value: "fa", flag: "ir", name: "persian" },
  { value: "sw", flag: "tz", name: "swahili" },
  { value: "ar", flag: "sa", name: "arabic" },
  { value: "zh", flag: "cn", name: "chinese" },
  { value: "ru", flag: "ru", name: "russian" },
] as const;

export type SupportedLocale = (typeof supportedLocales)[number]["value"];

const supportedLocaleValues = supportedLocales.map(
  (l) => l.value,
) as readonly string[];

function getBestLocale(): SupportedLocale {
  const saved = localStorage.getItem("preferred-language");

  if (saved && supportedLocaleValues.includes(saved)) {
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

      if (supportedLocaleValues.includes(lowerLang)) {
        return lowerLang as SupportedLocale;
      }

      const baseLang = lowerLang.split("-")[0];

      if (baseLang && supportedLocaleValues.includes(baseLang)) {
        return baseLang as SupportedLocale;
      }
    }
  }

  return "en";
}

const savedLocale = getBestLocale();
console.log("Saved locale:", savedLocale);

export const i18n = createI18n({
  legacy: false,
  globalInjection: true,
  locale: "en", // Start with English, we will switch it after loading
  fallbackLocale: "en",
  messages: {
    en: deepMerge(common, getEnglishContent()),
  },
});

export async function loadLocaleMessages(locale: SupportedLocale) {
  // If the language is already loaded
  if ((i18n.global.availableLocales as string[]).includes(locale)) {
    return nextTick();
  }

  // Load locale content using the unified async function
  const content = await getLocaleContent(locale);
  const finalMessages = deepMerge(common, content);
  i18n.global.setLocaleMessage(locale, finalMessages);

  return nextTick();
}

export async function setLocale(locale: SupportedLocale) {
  await loadLocaleMessages(locale);

  (i18n.global.locale as any).value = locale;

  localStorage.setItem("preferred-language", locale);
  document.querySelector("html")?.setAttribute("lang", locale);
}

// Load the saved locale
if (savedLocale !== "en") {
  setLocale(savedLocale);
}
