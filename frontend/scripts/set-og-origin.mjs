/**
 * Rewrite production Open Graph and Twitter absolute URLs in a built index.html.
 *
 * Source HTML pins https://v4.tallyj.com so one Vite build is valid for
 * production. The UAT frontend pipeline runs this against dist/index.html
 * before packaging, because crawlers need an absolute URL on the host they
 * can fetch. This does not run in the browser.
 *
 * Only these attributes are rewritten, and only when the content URL's
 * parsed origin is exactly https://v4.tallyj.com (or already the target):
 * property=og:url, property=og:image, name=twitter:url, name=twitter:image.
 * Comments, other meta tags, and lookalike hosts are left unchanged.
 *
 * Usage: node scripts/set-og-origin.mjs <https-origin> <index.html>
 */
import { readFileSync, writeFileSync } from "node:fs";
import { basename } from "node:path";

export const PRODUCTION_OG_ORIGIN = "https://v4.tallyj.com";

const PRODUCTION_ORIGIN = new URL(PRODUCTION_OG_ORIGIN).origin;

const SOCIAL_URL_KEYS = new Set([
  "property:og:url",
  "property:og:image",
  "name:twitter:url",
  "name:twitter:image",
]);

const META_TAG = /<meta\b([^>]*)>/gi;

function httpsOrigin(value) {
  let parsed;
  try {
    parsed = new URL(value.trim());
  } catch {
    parsed = null;
  }
  const pathless =
    parsed !== null &&
    parsed.protocol === "https:" &&
    parsed.username === "" &&
    parsed.password === "" &&
    parsed.pathname === "/" &&
    parsed.search === "" &&
    parsed.hash === "";
  if (!pathless) {
    throw new Error(
      `Origin must be an https origin without a path. Received: ${value}`,
    );
  }
  return parsed.origin;
}

function quotedAttr(attrs, name) {
  const match = new RegExp(
    `\\b${name}\\s*=\\s*(["'])([\\s\\S]*?)\\1`,
    "i",
  ).exec(attrs);
  return match ? match[2] : null;
}

function socialUrlKey(attrs) {
  const property = quotedAttr(attrs, "property");
  if (property !== null) {
    return `property:${property.toLowerCase()}`;
  }
  const name = quotedAttr(attrs, "name");
  if (name !== null) {
    return `name:${name.toLowerCase()}`;
  }
  return null;
}

function replaceContent(tag, previous, nextValue) {
  return tag.replace(
    /(\bcontent\s*=\s*)(["'])([\s\S]*?)\2/i,
    (full, prefix, quote, value) => {
      if (value !== previous) {
        return full;
      }
      return `${prefix}${quote}${nextValue}${quote}`;
    },
  );
}

export function rewriteOgOrigin(html, origin) {
  if (typeof html !== "string" || typeof origin !== "string") {
    throw new Error("html and origin are required");
  }
  const targetOrigin = httpsOrigin(origin);
  let rewritten = 0;
  let alreadyTarget = 0;

  const next = html.replace(META_TAG, (tag, attrs) => {
    const key = socialUrlKey(attrs);
    if (key === null || !SOCIAL_URL_KEYS.has(key)) {
      return tag;
    }
    const content = quotedAttr(attrs, "content");
    if (content === null) {
      return tag;
    }
    let contentUrl;
    try {
      contentUrl = new URL(content);
    } catch {
      return tag;
    }
    if (contentUrl.origin === targetOrigin) {
      alreadyTarget += 1;
      return tag;
    }
    if (contentUrl.origin !== PRODUCTION_ORIGIN) {
      return tag;
    }
    const retargeted = new URL(
      `${contentUrl.pathname}${contentUrl.search}${contentUrl.hash}`,
      targetOrigin,
    ).href;
    rewritten += 1;
    return replaceContent(tag, content, retargeted);
  });

  if (rewritten === 0 && alreadyTarget === 0) {
    throw new Error(
      `index.html has no og:url, og:image, twitter:url, or twitter:image whose origin is ${PRODUCTION_ORIGIN} or ${targetOrigin}`,
    );
  }
  return next;
}

const isDirectRun = basename(process.argv[1] ?? "") === "set-og-origin.mjs";

if (isDirectRun) {
  const origin = process.argv[2];
  const file = process.argv[3];
  if (!origin || !file) {
    console.error(
      "Usage: node scripts/set-og-origin.mjs <https-origin> <index.html>",
    );
    process.exit(1);
  }
  try {
    const html = readFileSync(file, "utf8");
    writeFileSync(file, rewriteOgOrigin(html, origin), "utf8");
    console.log(`Open Graph origin in ${file} is now ${httpsOrigin(origin)}`);
  } catch (error) {
    console.error(error instanceof Error ? error.message : error);
    process.exit(1);
  }
}
