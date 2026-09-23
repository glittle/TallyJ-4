import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { isRichEntry, unwrapMessages } from "./src/locales/richEntries.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const localesDir = path.join(__dirname, "src", "locales");
const outputDir = path.join(__dirname, "src", "locales", "bundled");

// Utility function to deep merge objects.
// Rich `{ t, s, w }` leaves are single values. unwrapMessages runs first so
// the written bundle is text only.
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

// Ensure output directory exists
if (!fs.existsSync(outputDir)) {
  fs.mkdirSync(outputDir, { recursive: true });
}

// Get all locale directories (exclude bundled directory)
const localeDirs = fs
  .readdirSync(localesDir, { withFileTypes: true })
  .filter((dirent) => dirent.isDirectory() && dirent.name !== "bundled")
  .map((dirent) => dirent.name);

// Process each locale directory
for (const locale of localeDirs) {
  const localePath = path.join(localesDir, locale);
  const files = fs
    .readdirSync(localePath)
    .filter((file) => file.endsWith(".json"));

  let merged = {};

  // Merge all JSON files in the locale directory
  for (const file of files) {
    const filePath = path.join(localePath, file);
    const content = unwrapMessages(
      JSON.parse(fs.readFileSync(filePath, "utf8")),
    );
    merged = deepMerge(merged, content);
  }

  assertTextOnly(merged, locale);

  // Write the merged file. Text only: provenance stays in the source JSON.
  const outputPath = path.join(outputDir, `${locale}.json`);
  fs.writeFileSync(outputPath, JSON.stringify(merged, null, 2));
  console.log(`Merged ${locale} locale into ${outputPath}`);
}

console.log("Locale bundling complete!");
