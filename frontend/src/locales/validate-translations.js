#!/usr/bin/env node

import { dirname, resolve } from "path";
import { fileURLToPath } from "url";
import {
  ValidationError,
  validateLocaleCatalog,
} from "./validateLocaleCatalog.js";

const DEFAULT_LOCALES_DIR = dirname(fileURLToPath(import.meta.url));

function printWarnings(warnings) {
  if (!warnings || warnings.length === 0) {
    return;
  }

  console.log(
    "\nKeys awaiting translation (en is the reference; these do not fail the check):",
  );
  for (const { locale, count } of warnings) {
    console.log(`  ${locale}: ${count} keys awaiting translation`);
  }
}

function printResults(errors, warnings = []) {
  printWarnings(warnings);

  if (errors.length === 0) {
    console.log("\n✅ All translation files are valid!");
    return true;
  }

  console.log(`\n❌ Found ${errors.length} validation error(s):\n`);

  const errorsByType = new Map();
  for (const error of errors) {
    if (!errorsByType.has(error.type)) {
      errorsByType.set(error.type, []);
    }
    errorsByType.get(error.type).push(error);
  }

  for (const [type, typeErrors] of errorsByType.entries()) {
    console.log(`\n${type} (${typeErrors.length}):`);
    for (const error of typeErrors) {
      console.log(`  - ${error.message}`);
    }
  }

  return false;
}

function printInventory(report) {
  console.log(`Found ${report.files.length} JSON file(s)\n`);

  if (report.rootFiles.length > 0) {
    console.log(`Root files: ${report.rootFiles.join(", ")}`);
  }

  if (report.localeFiles.size > 0) {
    console.log(`Locales: ${Array.from(report.localeFiles.keys()).join(", ")}`);
    for (const [locale, files] of report.localeFiles.entries()) {
      console.log(`  ${locale}: ${files.map((f) => f.fileName).join(", ")}`);
    }
  }
}

function localesDirFromArgv() {
  const requested = process.argv[2];
  if (!requested) {
    return DEFAULT_LOCALES_DIR;
  }
  return resolve(requested);
}

function main() {
  console.log("🔍 Validating translation files...\n");

  try {
    const report = validateLocaleCatalog(localesDirFromArgv());
    printInventory(report);
    const isValid = printResults(report.errors, report.warnings);
    process.exit(isValid ? 0 : 1);
  } catch (error) {
    if (error instanceof ValidationError) {
      console.error(`\n❌ ${error.message}`);
      process.exit(1);
    }
    throw error;
  }
}

main();
