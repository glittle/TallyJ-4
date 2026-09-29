import { readFileSync, readdirSync, statSync } from "fs";
import { join } from "path";
import { isLocaleDirectoryName } from "./localeDirectory.js";
import { isRichEntry } from "./richEntries.js";

export { isLocaleDirectoryName };

const SOURCE_STATUS = "source";
const TRANSLATION_STATUSES = new Set(["ai", "human", "approved"]);
const ISO_UTC_TIMESTAMP = /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d+)?Z$/;

export class ValidationError {
  constructor(type, message, details = {}) {
    this.type = type;
    this.message = message;
    this.details = details;
  }
}

function getAllJsonFiles(dir, baseDir = dir, topLevel = true) {
  const files = [];
  const entries = readdirSync(dir);

  for (const entry of entries) {
    const fullPath = join(dir, entry);
    const stat = statSync(fullPath);

    if (stat.isDirectory()) {
      // Same folder rule as merge-locales. A typo directory with JSON is not
      // a locale here; merge-locales exits non-zero instead of skipping it.
      if (topLevel && !isLocaleDirectoryName(entry)) {
        continue;
      }
      files.push(...getAllJsonFiles(fullPath, baseDir, false));
    } else if (entry.endsWith(".json") && entry !== "package.json") {
      const relativePath = fullPath
        .substring(baseDir.length + 1)
        .replace(/\\/g, "/");
      files.push(relativePath);
    }
  }

  return files;
}

function loadJsonFile(localesDir, filePath) {
  try {
    const content = readFileSync(join(localesDir, filePath), "utf-8");
    return JSON.parse(content);
  } catch (error) {
    throw new ValidationError(
      "FILE_READ_ERROR",
      `Failed to read ${filePath}: ${error.message}`,
      { filePath },
    );
  }
}

function getAllKeys(obj, prefix = "") {
  const keys = [];

  for (const [key, value] of Object.entries(obj)) {
    const fullKey = prefix ? `${prefix}.${key}` : key;

    if (
      value &&
      typeof value === "object" &&
      !Array.isArray(value) &&
      !isRichEntry(value) &&
      Object.keys(value).length > 0
    ) {
      const nestedKeys = getAllKeys(value, fullKey);
      keys.push(...nestedKeys);
    } else {
      keys.push(fullKey);
    }
  }

  return keys;
}

function checkPrefixCollisions(keys, filePath) {
  const errors = [];
  const sorted = [...new Set(keys)].sort();

  for (let i = 0; i < sorted.length; i++) {
    const prefix = `${sorted[i]}.`;
    for (
      let j = i + 1;
      j < sorted.length && sorted[j].startsWith(prefix);
      j++
    ) {
      errors.push(
        new ValidationError(
          "PREFIX_COLLISION",
          `Key "${sorted[i]}" in ${filePath} is both a leaf and a parent of "${sorted[j]}". Rename one of them — flatToNested cannot nest a child under a leaf.`,
          { filePath, key: sorted[i], child: sorted[j] },
        ),
      );
      break;
    }
  }

  return errors;
}

function checkDuplicateKeys(keys, filePath) {
  const errors = [];
  const seen = new Map();

  for (const key of keys) {
    if (seen.has(key)) {
      errors.push(
        new ValidationError(
          "DUPLICATE_KEY",
          `Duplicate key "${key}" in ${filePath}`,
          { filePath, key },
        ),
      );
    }
    seen.set(key, true);
  }

  return errors;
}

function isIsoUtcTimestamp(value) {
  return (
    typeof value === "string" &&
    ISO_UTC_TIMESTAMP.test(value) &&
    !Number.isNaN(Date.parse(value))
  );
}

function allowedStatuses(locale) {
  if (locale === null || locale === "en") {
    return new Set([SOURCE_STATUS]);
  }
  return TRANSLATION_STATUSES;
}

function statusExpectation(locale) {
  if (locale === null || locale === "en") {
    return "source";
  }
  return "ai, human, or approved";
}

function checkMessageLeaf(value, key, filePath, locale) {
  const errors = [];

  if (typeof value === "string") {
    errors.push(
      new ValidationError(
        "BARE_STRING",
        `Key "${key}" in ${filePath} is a bare string. Locale leaves must be { t, s, w }.`,
        { filePath, key },
      ),
    );
    return errors;
  }

  if (!isRichEntry(value)) {
    errors.push(
      new ValidationError(
        "INVALID_VALUE_TYPE",
        `Key "${key}" in ${filePath} must be a { t, s, w } object`,
        { filePath, key, valueType: value === null ? "null" : typeof value },
      ),
    );
    return errors;
  }

  if (value.t.trim() === "") {
    errors.push(
      new ValidationError(
        "EMPTY_VALUE",
        `Key "${key}" in ${filePath} has empty text`,
        { filePath, key },
      ),
    );
  }

  if (typeof value.s !== "string" || !allowedStatuses(locale).has(value.s)) {
    errors.push(
      new ValidationError(
        "INVALID_STATUS",
        `Key "${key}" in ${filePath} has status "${value.s}"; expected ${statusExpectation(locale)}.`,
        { filePath, key, status: value.s },
      ),
    );
  }

  if (!isIsoUtcTimestamp(value.w)) {
    errors.push(
      new ValidationError(
        "INVALID_TIMESTAMP",
        `Key "${key}" in ${filePath} has w "${value.w}" which is not an ISO-8601 UTC timestamp.`,
        { filePath, key, w: value.w },
      ),
    );
  }

  return errors;
}

function isConfigValue(value) {
  return (
    Array.isArray(value) ||
    typeof value === "number" ||
    typeof value === "boolean"
  );
}

function checkMessageLeaves(data, filePath, locale, allowConfigValues) {
  const errors = [];

  function walk(obj, prefix) {
    for (const [key, value] of Object.entries(obj)) {
      const fullKey = prefix ? `${prefix}.${key}` : key;

      if (isRichEntry(value) || typeof value === "string") {
        errors.push(...checkMessageLeaf(value, fullKey, filePath, locale));
        continue;
      }

      if (
        value &&
        typeof value === "object" &&
        !Array.isArray(value) &&
        Object.keys(value).length > 0
      ) {
        walk(value, fullKey);
        continue;
      }

      if (allowConfigValues && isConfigValue(value)) {
        continue;
      }

      errors.push(
        new ValidationError(
          "INVALID_VALUE_TYPE",
          `Key "${fullKey}" in ${filePath} must be a { t, s, w } object`,
          {
            filePath,
            key: fullKey,
            valueType:
              value === null
                ? "null"
                : Array.isArray(value)
                  ? "array"
                  : typeof value,
          },
        ),
      );
    }
  }

  walk(data, "");
  return errors;
}

function categorizeFiles(files) {
  const rootFiles = [];
  const localeFiles = new Map();

  for (const file of files) {
    if (file.includes("/")) {
      const [locale, ...rest] = file.split("/");
      const fileName = rest.join("/");

      if (!localeFiles.has(locale)) {
        localeFiles.set(locale, []);
      }
      localeFiles.get(locale).push({ original: file, fileName });
    } else {
      rootFiles.push(file);
    }
  }

  return { rootFiles, localeFiles };
}

function getAllKeysInLocale(localesDir, localeFiles, locale) {
  const allKeys = new Map();
  const files = localeFiles.get(locale) || [];

  for (const { original, fileName } of files) {
    const data = loadJsonFile(localesDir, original);
    const keys = getAllKeys(data);

    for (const key of keys) {
      if (allKeys.has(key)) {
        allKeys.get(key).push(fileName);
      } else {
        allKeys.set(key, [fileName]);
      }
    }
  }

  return allKeys;
}

function checkDuplicateKeysInLocale(localesDir, localeFiles) {
  const errors = [];

  for (const [_locale, _fileList] of localeFiles.entries()) {
    const keyMap = getAllKeysInLocale(localesDir, localeFiles, _locale);

    for (const [key, files] of keyMap.entries()) {
      if (files.length > 1) {
        errors.push(
          new ValidationError(
            "DUPLICATE_KEY_ACROSS_FILES",
            `Key "${key}" in locale "${_locale}" is duplicated across files: ${files.join(", ")}`,
            { locale: _locale, key, files },
          ),
        );
      }
    }
  }

  return errors;
}

function checkPrefixCollisionsInLocale(localesDir, localeFiles) {
  const errors = [];

  for (const [locale, _fileList] of localeFiles.entries()) {
    if (locale === "bundled") {
      continue;
    }
    const keyMap = getAllKeysInLocale(localesDir, localeFiles, locale);
    const keys = [...keyMap.keys()];
    const sorted = [...new Set(keys)].sort();

    for (let i = 0; i < sorted.length; i++) {
      const prefix = `${sorted[i]}.`;
      for (
        let j = i + 1;
        j < sorted.length && sorted[j].startsWith(prefix);
        j++
      ) {
        errors.push(
          new ValidationError(
            "PREFIX_COLLISION",
            `Key "${sorted[i]}" in locale "${locale}" is both a leaf and a parent of "${sorted[j]}". Rename one of them — flatToNested cannot nest a child under a leaf.`,
            { locale, key: sorted[i], child: sorted[j] },
          ),
        );
        break;
      }
    }
  }

  return errors;
}

/**
 * English is the reference catalog. `fallbackLocale` is `en`, and new strings
 * are added to `en/` only, so a key that other locales have not translated yet
 * is expected.
 *
 * - awaiting: keys in `en` missing from another locale (including files that
 *   exist only in English). Non-failing per-locale counts.
 * - stale: keys present in a non-en locale but absent from `en`. These fail.
 * - missingReference: there is no `en` catalog to compare against.
 *
 * @param {Map<string, Iterable<string>>} keysByLocale
 * @returns {{
 *   stale: { locale: string, key: string }[],
 *   awaiting: { locale: string, count: number }[],
 *   missingReference: boolean
 * }}
 */
export function diffKeysAgainstEnglish(keysByLocale) {
  const enKeys = keysByLocale.get("en");
  if (!enKeys) {
    return { stale: [], awaiting: [], missingReference: true };
  }

  const enSet = enKeys instanceof Set ? enKeys : new Set(enKeys);
  const stale = [];
  const awaiting = [];
  const locales = [...keysByLocale.keys()]
    .filter((locale) => locale !== "en")
    .sort();

  for (const locale of locales) {
    const raw = keysByLocale.get(locale);
    const keys = raw instanceof Set ? raw : new Set(raw);
    let count = 0;
    for (const key of enSet) {
      if (!keys.has(key)) {
        count++;
      }
    }
    if (count > 0) {
      awaiting.push({ locale, count });
    }

    const extras = [...keys].filter((key) => !enSet.has(key)).sort();
    for (const key of extras) {
      stale.push({ locale, key });
    }
  }

  return { stale, awaiting, missingReference: false };
}

function checkKeyConsistency(localesDir, localeFiles) {
  const errors = [];
  const keysByLocale = new Map();

  for (const locale of localeFiles.keys()) {
    const allKeys = getAllKeysInLocale(localesDir, localeFiles, locale);
    keysByLocale.set(locale, new Set(allKeys.keys()));
  }

  const diff = diffKeysAgainstEnglish(keysByLocale);
  if (diff.missingReference) {
    errors.push(
      new ValidationError(
        "MISSING_REFERENCE_LOCALE",
        'Reference locale "en" was not found. Key checks use English as the source of truth.',
      ),
    );
    return { errors, warnings: [] };
  }

  for (const { locale, key } of diff.stale) {
    errors.push(
      new ValidationError(
        "STALE_KEY",
        `Key "${key}" is present in locale "${locale}" but absent from en. Remove it, or add it to en if it is still used.`,
        { key, locale },
      ),
    );
  }

  return { errors, warnings: diff.awaiting };
}

function validateRootFiles(localesDir, rootFiles) {
  const errors = [];

  for (const file of rootFiles) {
    const data = loadJsonFile(localesDir, file);
    const keys = getAllKeys(data);
    const isConfigFile = file === "common.json" || file === "shared.json";

    errors.push(...checkDuplicateKeys(keys, file));
    errors.push(...checkPrefixCollisions(keys, file));
    errors.push(...checkMessageLeaves(data, file, null, isConfigFile));
  }

  return errors;
}

function validateLocaleFiles(localesDir, localeFiles) {
  const errors = [];

  for (const [_locale, files] of localeFiles.entries()) {
    for (const { original } of files) {
      const data = loadJsonFile(localesDir, original);
      const keys = getAllKeys(data);

      errors.push(...checkDuplicateKeys(keys, original));
      errors.push(...checkMessageLeaves(data, original, _locale, false));
    }
  }

  errors.push(...checkDuplicateKeysInLocale(localesDir, localeFiles));
  errors.push(...checkPrefixCollisionsInLocale(localesDir, localeFiles));
  const consistency = checkKeyConsistency(localesDir, localeFiles);
  errors.push(...consistency.errors);

  return { errors, warnings: consistency.warnings };
}

/**
 * Read and check a locale directory. Does not print or exit.
 * Unreadable or invalid JSON throws {@link ValidationError} with type
 * `FILE_READ_ERROR`.
 *
 * @param {string} localesDir
 * @returns {{
 *   files: string[],
 *   rootFiles: string[],
 *   localeFiles: Map<string, { original: string, fileName: string }[]>,
 *   errors: ValidationError[],
 *   warnings: { locale: string, count: number }[]
 * }}
 */
export function validateLocaleCatalog(localesDir) {
  const files = getAllJsonFiles(localesDir);
  const { rootFiles, localeFiles } = categorizeFiles(files);
  const errors = [];
  errors.push(...validateRootFiles(localesDir, rootFiles));
  const localeResult = validateLocaleFiles(localesDir, localeFiles);
  errors.push(...localeResult.errors);

  return {
    files,
    rootFiles,
    localeFiles,
    errors,
    warnings: localeResult.warnings,
  };
}
