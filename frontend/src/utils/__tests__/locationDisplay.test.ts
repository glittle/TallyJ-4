import { describe, expect, it } from "vitest";
import { formatLocationLabel } from "../locationDisplay";

describe("formatLocationLabel", () => {
  const t = (key: string) => (key === "locations.typeOnline" ? "Online" : key);

  it("uses i18n for an Online-typed location regardless of name", () => {
    expect(
      formatLocationLabel(t, { name: "Hall A", locationType: "Online" }),
    ).toBe("Online");
  });

  it("uses the stored name for a paper location", () => {
    expect(
      formatLocationLabel(t, { name: "Main Hall", locationType: "Manual" }),
    ).toBe("Main Hall");
  });
});
