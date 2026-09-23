/**
 * Locale source files store each message as `{ t, s, w }`.
 * vue-i18n, tests, and the production bundle must see the text only.
 *
 * A rich entry is a leaf, not a nested catalog. A namespace object that
 * contains other keys is left for the caller to walk.
 */

const RICH_ENTRY_KEYS = new Set(["t", "s", "w"]);

export function isRichEntry(value) {
  if (value === null || typeof value !== "object" || Array.isArray(value)) {
    return false;
  }
  if (typeof value.t !== "string") {
    return false;
  }
  for (const key of Object.keys(value)) {
    if (!RICH_ENTRY_KEYS.has(key)) {
      return false;
    }
  }
  return true;
}

/**
 * Deep-copy a catalog, replacing each rich leaf with its `t` text.
 * Bare strings pass through so a text-only bundle can use the same path.
 * Arrays, numbers, and booleans are returned as-is (root common.json config).
 */
export function unwrapMessages(value) {
  if (isRichEntry(value)) {
    return value.t;
  }
  if (value && typeof value === "object" && !Array.isArray(value)) {
    const result = {};
    for (const [key, child] of Object.entries(value)) {
      result[key] = unwrapMessages(child);
    }
    return result;
  }
  return value;
}
