import { nextTick } from "vue";
import { createI18n } from "vue-i18n";
import commonRaw from "./common.json";
const common = flatToNested(commonRaw);

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

    current[keys.at(-1)!] = flat[key];
  }

  return result;
}

// Load bundled files (may not exist in development)
const enBundledModule = import.meta.glob("./bundled/en.json", { eager: true });

// Production uses pre-merged bundled/*.json files (created by merge-locales.js).
// Exclude en.json here because English is always eagerly loaded below.
const bundledLocaleModules = import.meta.glob([
  "./bundled/*.json",
  "!./bundled/en.json",
]);

// In development we load individual per-file JSONs for non-English locales on demand.
// In production we only use the bundled versions (the dev glob is dropped by the bundler).
const individualLocaleModules = import.meta.env.DEV
  ? import.meta.glob(["./*/*.json", "!./en/*.json"])
  : {};

const useBundled =
  Object.keys(enBundledModule).length > 0 && !import.meta.env.DEV;

// Load individual English files (always available as fallback + merged into initial i18n messages)
const enModules = import.meta.glob("./en/*.json", { eager: true });

if (import.meta.env.DEV) {
  console.log("Locale initialization: useBundled =", useBundled);
}

// Get English content synchronously (always available)
function getEnglishContent(): any {
  if (useBundled) {
    const moduleEntry = Object.values(enBundledModule)[0] as any;
    const content = moduleEntry?.default || moduleEntry;

    if (content) {
      return flatToNested(content);
    }
    // Fall through to individual files
  }

  // Merge individual English files (dev or bundled-missing fallback)
  let merged = {};
  for (const path in enModules) {
    const content =
      (enModules[path] as any).default || (enModules[path] as any);
    merged = deepMerge(merged, content);
  }
  return flatToNested(merged);
}

// Load bundled locale content (production path)
async function loadBundledLocale(locale: string): Promise<any> {
  const path = `./bundled/${locale}.json`;
  if (import.meta.env.DEV) {
    console.log(`Loading bundled locale ${locale} from ${path}`);
  }

  const loadFn = bundledLocaleModules[path];
  if (!loadFn) {
    if (import.meta.env.DEV) {
      console.warn(`No bundled module found for ${locale}`);
    }
    return null;
  }

  try {
    const mod = await loadFn();
    const content = mod.default || mod;
    if (content) {
      if (import.meta.env.DEV) {
        console.log(`Loaded bundled content for ${locale}`);
      }
      return flatToNested(content);
    }
  } catch (error) {
    console.warn(`Failed to load bundled locale ${locale}:`, error);
  }

  return null;
}

// Load individual locale files (development only fallback)
async function loadIndividualLocaleFiles(locale: string): Promise<any> {
  if (import.meta.env.DEV) {
    console.log(`Loading individual files for ${locale}`);
  }

  const localeModules = Object.keys(individualLocaleModules).filter(
    (p) => p.startsWith(`./${locale}/`) && p.endsWith(".json"),
  );

  if (import.meta.env.DEV) {
    console.log(`Found ${localeModules.length} individual files for ${locale}`);
  }

  let merged = {};
  for (const path of localeModules) {
    const loadFn = individualLocaleModules[path];
    if (!loadFn) {
      continue;
    }

    try {
      const mod = await loadFn();
      const content = mod.default || mod;
      if (content) {
        merged = deepMerge(merged, content);
      }
    } catch (error) {
      console.warn(`Failed to load individual file ${path}:`, error);
    }
  }

  if (import.meta.env.DEV) {
    console.log(
      `Loaded individual content for ${locale}, keys:`,
      Object.keys(merged).length,
    );
  }
  return flatToNested(merged);
}

// Get other locale content (async for dynamic loading)
async function getLocaleContent(locale: string): Promise<any> {
  if (useBundled) {
    const bundledContent = await loadBundledLocale(locale);
    if (bundledContent) {
      return bundledContent;
    }
  }
  return loadIndividualLocaleFiles(locale);
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
  { value: "hr", flag: "hr", name: "croatian" },
] as const;

export type SupportedLocale = (typeof supportedLocales)[number]["value"];

const supportedLocaleValues = supportedLocales.map(
  (l) => l.value,
) as readonly string[];

function findSupportedLocale(
  languages: readonly string[],
): SupportedLocale | null {
  for (const lang of languages) {
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

  return null;
}

function getBestLocale(): SupportedLocale {
  const saved = localStorage.getItem("preferred-language");

  if (saved && supportedLocaleValues.includes(saved)) {
    return saved as SupportedLocale;
  }

  if (typeof navigator !== "undefined") {
    const browserLanguages =
      navigator.languages || (navigator.language ? [navigator.language] : []);

    const matched = findSupportedLocale(browserLanguages);
    if (matched) {
      return matched;
    }
  }

  return "en";
}

const savedLocale = getBestLocale();
if (import.meta.env.DEV) {
  console.log("Initial locale:", savedLocale);
}

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
  await setLocale(savedLocale);
}
