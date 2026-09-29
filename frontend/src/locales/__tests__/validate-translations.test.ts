import { describe, expect, it } from "vitest";
import { diffKeysAgainstEnglish } from "../validate-translations.js";

describe("locale key consistency", () => {
  it("uses en as the reference: untranslated keys warn, keys absent from en are stale", () => {
    const diff = diffKeysAgainstEnglish(
      new Map([
        ["en", ["common.keep", "frontDesk.onlyEnglish", "common.alsoNew"]],
        ["fr", ["common.keep", "elections.stageChangeError"]],
        ["es", ["common.keep", "common.alsoNew", "frontDesk.onlyEnglish"]],
      ]),
    );

    expect(diff.missingReference).toBe(false);
    expect(diff.awaiting).toEqual([{ locale: "fr", count: 2 }]);
    expect(diff.stale).toEqual([
      { locale: "fr", key: "elections.stageChangeError" },
    ]);
  });

  it("reports no awaiting or stale keys when a locale matches en", () => {
    const diff = diffKeysAgainstEnglish(
      new Map([
        ["en", ["common.keep"]],
        ["fr", ["common.keep"]],
      ]),
    );

    expect(diff.awaiting).toEqual([]);
    expect(diff.stale).toEqual([]);
  });
});
