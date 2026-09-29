/**
 * BCP-47 locale folder names shared by the validator and merge-locales.
 *
 * A 2–3 letter language, an optional 4-letter script, and an optional
 * 2-letter or 3-digit region. Examples: en, fil, zh-Hans, pt-BR, en-US, es-419.
 */
export const LOCALE_DIRECTORY_SOURCE =
  "[a-z]{2,3}(?:-[A-Z][a-z]{3})?(?:-(?:[A-Z]{2}|\\d{3}))?";

export const LOCALE_DIRECTORY_PATTERN = new RegExp(
  `^${LOCALE_DIRECTORY_SOURCE}$`,
);

export function isLocaleDirectoryName(name) {
  return LOCALE_DIRECTORY_PATTERN.test(name);
}

/** Present under src/locales, but never catalogs. */
export const IGNORED_LOCALE_DIRECTORIES = new Set([
  "bundled",
  "__tests__",
  "node_modules",
]);
