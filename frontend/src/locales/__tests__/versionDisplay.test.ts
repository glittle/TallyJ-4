import { describe, expect, it } from "vitest";
import faCommon from "../fa/common.json";
import { splitVersionDisplay } from "../versionDisplay";

describe("splitVersionDisplay", () => {
  it("keeps the Latin Beta mark in English and Arabic catalog strings", () => {
    expect(splitVersionDisplay("Version 4 Beta")).toEqual({
      before: "Version 4 ",
      mark: "Beta",
      after: "",
    });
    expect(splitVersionDisplay("الإصدار 4 Beta")).toEqual({
      before: "الإصدار 4 ",
      mark: "Beta",
      after: "",
    });
  });

  it("leaves Persian بتا in the full string (no Latin mark span)", () => {
    expect(faCommon["common.versionDisplay"]).toBe("نسخه 4 بتا");
    expect(splitVersionDisplay(faCommon["common.versionDisplay"])).toEqual({
      before: "نسخه 4 بتا",
      mark: "",
      after: "",
    });
  });

  it("returns the full string when Beta is absent", () => {
    expect(splitVersionDisplay("Version 4")).toEqual({
      before: "Version 4",
      mark: "",
      after: "",
    });
  });
});
