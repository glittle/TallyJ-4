import { describe, expect, it } from "vitest";
import { splitVersionDisplay } from "../versionDisplay";

describe("splitVersionDisplay", () => {
  it("keeps the Latin Beta mark in English and RTL catalog strings", () => {
    expect(splitVersionDisplay("Version 4 Beta")).toEqual({
      before: "Version 4 ",
      mark: "Beta",
      after: "",
    });
    expect(splitVersionDisplay("نسخه 4 Beta")).toEqual({
      before: "نسخه 4 ",
      mark: "Beta",
      after: "",
    });
    expect(splitVersionDisplay("الإصدار 4 Beta")).toEqual({
      before: "الإصدار 4 ",
      mark: "Beta",
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
