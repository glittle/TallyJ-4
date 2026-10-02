/**
 * Rewrite the production Open Graph origin in a built index.html.
 *
 * Source HTML pins https://v4.tallyj.com so one Vite build is valid for
 * production. The UAT frontend pipeline runs this against dist/index.html
 * before packaging, because crawlers need an absolute URL on the host they
 * can fetch. This does not run in the browser.
 *
 * Usage: node scripts/set-og-origin.mjs <https-origin> <index.html>
 */
import { readFileSync, writeFileSync } from "node:fs";

export const PRODUCTION_OG_ORIGIN = "https://v4.tallyj.com";

export function rewriteOgOrigin(html, origin) {
  if (typeof html !== "string" || typeof origin !== "string") {
    throw new Error("html and origin are required");
  }
  let normalized = origin.trim().replace(/\/+$/, "");
  if (!/^https:\/\/[^/\s]+$/i.test(normalized)) {
    throw new Error(
      `Origin must be an https origin without a path. Received: ${origin}`,
    );
  }
  if (!html.includes(PRODUCTION_OG_ORIGIN)) {
    if (html.includes(normalized)) {
      return html;
    }
    throw new Error(
      `index.html does not contain the production origin ${PRODUCTION_OG_ORIGIN}`,
    );
  }
  if (normalized.toLowerCase() === PRODUCTION_OG_ORIGIN) {
    return html;
  }
  return html.split(PRODUCTION_OG_ORIGIN).join(normalized);
}

const isDirectRun = process.argv[1] &&
  process.argv[1].endsWith("set-og-origin.mjs");

if (isDirectRun) {
  const origin = process.argv[2];
  const file = process.argv[3];
  if (!origin || !file) {
    console.error(
      "Usage: node scripts/set-og-origin.mjs <https-origin> <index.html>",
    );
    process.exit(1);
  }
  const html = readFileSync(file, "utf8");
  writeFileSync(file, rewriteOgOrigin(html, origin), "utf8");
  console.log(`Open Graph origin in ${file} is now ${origin.replace(/\/+$/, "")}`);
}
