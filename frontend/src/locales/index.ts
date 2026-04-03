import { createI18n } from "vue-i18n";
import { nextTick } from "vue";
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

    current[keys[keys.length - 1]!] = flat[key];
  }

  return result;
}

// Load bundled files (may not exist in development)
const enBundledModule = import.meta.glob("./bundled/en.json", { eager: true });
const localeModulesAsync = import.meta.glob("./bundled/*.json");
const useBundled = Object.keys(enBundledModule).length > 0;

// Load individual English files (always available as fallback)
const enModules = import.meta.glob("./en/*.json", { eager: true });

console.log('Locale initialization:');
console.log('- useBundled:', useBundled);

// Get English content synchronously (always available)
function getEnglishContent(): any {
  if (useBundled) {
    // Production: use bundled English file
    console.log('Loading English from bundled file');

    // Get the content from the bundled module
    const moduleEntry = Object.values(enBundledModule)[0];
    const content = moduleEntry?.default || moduleEntry;

    if (content) {
      console.log('English bundled content loaded successfully');
      return flatToNested(content);
    } else {
      console.warn('Bundled English content not found, falling back to individual files');
    }
  }

  // Fallback: merge individual English files
  console.log('Loading English from individual files');
  let merged = {};
  for (const path in enModules) {
    const content = enModules[path].default || enModules[path];
    merged = deepMerge(merged, content);
  }
  console.log('English individual content loaded, keys:', Object.keys(merged).length);
  return flatToNested(merged);
}

// Get other locale content (async for dynamic loading)
async function getLocaleContent(locale: string): any {
  console.log(`Loading locale content for ${locale}, useBundled:`, useBundled);

  if (useBundled) {
    // Try bundled files first
    const path = `./bundled/${locale}.json`;
    console.log(`Attempting to load bundled locale ${locale} from ${path}`);
    const loadFn = localeModulesAsync[path];

    if (loadFn) {
      try {
        console.log(`Found load function for bundled ${locale}, calling it...`);
        const mod = await loadFn();
        const content = mod.default || mod;
        if (content) {
          console.log(`Successfully loaded bundled content for ${locale}`);
          return flatToNested(content);
        }
      } catch (error) {
        console.warn(`Failed to load bundled locale ${locale}:`, error);
      }
    } else {
      console.warn(`No bundled load function found for ${locale}`);
    }
  }

  // Fallback: load individual files
  console.log(`Loading individual files for ${locale}`);
  const individualModules = import.meta.glob("./*/*.json");
  const localeModules = Object.keys(individualModules)
    .filter(path => path.startsWith(`./${locale}/`) && path.endsWith('.json'));

  console.log(`Found ${localeModules.length} individual files for ${locale}`);

  let merged = {};
  for (const path of localeModules) {
    const loadFn = individualModules[path];
    if (loadFn) {
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
  }

  console.log(`Loaded individual content for ${locale}, keys:`, Object.keys(merged).length);
  return flatToNested(merged);
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
