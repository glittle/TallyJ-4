import { describe, expect, it } from "vitest";
import { i18n } from "../../test/setup";
import { isRichEntry, unwrapMessages } from "../richEntries.js";

describe("rich locale entries", () => {
  it("treats { t, s, w } as a leaf and unwraps it to text", () => {
    const entry = {
      t: "Add Ballot",
      s: "source",
      w: "2026-09-23T17:00:00Z",
    };
    expect(isRichEntry(entry)).toBe(true);
    expect(isRichEntry({ nested: { t: "no" } })).toBe(false);
    expect(isRichEntry("Add Ballot")).toBe(false);

    expect(
      unwrapMessages({
        "ballots.addBallot": entry,
        "features.realTimeUpdates": true,
        "features.exportFormats": ["PDF"],
      }),
    ).toEqual({
      "ballots.addBallot": "Add Ballot",
      "features.realTimeUpdates": true,
      "features.exportFormats": ["PDF"],
    });
  });

  it("returns a string from t() after unwrapping English messages", () => {
    const text = i18n.global.t("common.versionDisplay");
    expect(text).toBe("Version 4 Beta");
  });
});
