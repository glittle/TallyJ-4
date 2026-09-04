import { describe, expect, it } from "vitest";
import faCommon from "../fa/common.json";

describe("Persian version display", () => {
  it("uses بتا instead of Latin Beta", () => {
    expect(faCommon["common.versionDisplay"]).toBe("نسخه 4 بتا");
    expect(faCommon["common.versionDisplay"]).not.toContain("Beta");
  });
});
