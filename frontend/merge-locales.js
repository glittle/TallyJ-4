#!/usr/bin/env node

import path from "node:path";
import { fileURLToPath } from "node:url";
import { mergeLocaleBundles } from "./mergeLocaleBundles.js";

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const localesDir = process.argv[2]
  ? path.resolve(process.argv[2])
  : path.join(__dirname, "src", "locales");
const outputDir = process.argv[3]
  ? path.resolve(process.argv[3])
  : path.join(localesDir, "bundled");

try {
  mergeLocaleBundles(localesDir, outputDir);
  console.log("Locale bundling complete!");
} catch (error) {
  console.error(error.message);
  process.exit(1);
}
