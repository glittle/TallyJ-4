import fs from "node:fs";
import path from "node:path";
import { isRichEntry, unwrapMessages } from "./src/locales/richEntries.js";
import {
  IGNORED_LOCALE_DIRECTORIES,
  isLocaleDirectoryName,
} from "./src/locales/localeDirectory.js";

export class UnexpectedLocaleFolderError extends Error {
  constructor(folders) {
    const quoted = folders.map((name) => `"${name}"`).join(", ");
    super(
      `Locale folder ${quoted} contains JSON but is not a locale code. Expected a BCP-47 folder such as en, fil, zh-Hans, or pt-BR.`,
    );
    this.name = "UnexpectedLocaleFolderError";
    this.folders = folders;
  }
}

function directoryContainsJson(dir) {
  for (const entry of fs.readdirSync(dir, { withFileTypes: true })) {
    const fullPath = path.join(dir, entry.name);
    if (entry.isDirectory()) {
      if (directoryContainsJson(fullPath)) {
        return true;
      }
      continue;
    }
    if (entry.isFile() && entry.name.endsWith(".json")) {
      return true;
    }
  }
  return false;
}

function deepMerge(target, source) {
  const result = { ...target };

  for (const key in source) {
    if (isRichEntry(source[key])) {
      result[key] = source[key];
    } else if (
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

function assertTextOnly(catalog, locale) {
  for (const [key, value] of Object.entries(catalog)) {
    if (typeof value !== "string") {
      throw new Error(
        `Bundled ${locale} key "${key}" is not text. Production bundles must not contain s/w metadata.`,
      );
    }
  }
}

/**
 * Classify top-level folders. Ignored names are never locales. Any other
 * folder that holds JSON and is not a BCP-47 code is an error so a real
 * locale cannot be dropped just because its name failed the pattern.
 */
export function classifyLocaleFolders(localesDir) {
  const locales = [];
  const unexpected = [];

  for (const entry of fs.readdirSync(localesDir, { withFileTypes: true })) {
    if (!entry.isDirectory()) {
      continue;
    }
    if (IGNORED_LOCALE_DIRECTORIES.has(entry.name)) {
      continue;
    }
    if (isLocaleDirectoryName(entry.name)) {
      locales.push(entry.name);
      continue;
    }
    if (directoryContainsJson(path.join(localesDir, entry.name))) {
      unexpected.push(entry.name);
    }
  }

  unexpected.sort();
  return { locales, unexpected };
}

function clearBundleJson(outputDir) {
  if (!fs.existsSync(outputDir)) {
    fs.mkdirSync(outputDir, { recursive: true });
    return;
  }
  for (const file of fs.readdirSync(outputDir)) {
    if (file.endsWith(".json")) {
      fs.unlinkSync(path.join(outputDir, file));
    }
  }
}

/**
 * Merge each locale folder into outputDir/{locale}.json (text only).
 * Removes existing JSON in outputDir first, so a retired code or a previous
 * __tests__ bundle cannot linger. Throws before that cleanup when a
 * non-ignored folder contains JSON but is not a locale code.
 */
export function mergeLocaleBundles(localesDir, outputDir) {
  const { locales, unexpected } = classifyLocaleFolders(localesDir);
  if (unexpected.length > 0) {
    throw new UnexpectedLocaleFolderError(unexpected);
  }

  clearBundleJson(outputDir);

  for (const locale of locales) {
    const localePath = path.join(localesDir, locale);
    const files = fs
      .readdirSync(localePath)
      .filter((file) => file.endsWith(".json"));

    let merged = {};
    for (const file of files) {
      const content = unwrapMessages(
        JSON.parse(fs.readFileSync(path.join(localePath, file), "utf8")),
      );
      merged = deepMerge(merged, content);
    }

    assertTextOnly(merged, locale);
    const outputPath = path.join(outputDir, `${locale}.json`);
    fs.writeFileSync(outputPath, JSON.stringify(merged, null, 2));
    console.log(`Merged ${locale} locale into ${outputPath}`);
  }
}
