import { describe, expect, it } from "vitest";
import {
  formatLocationLabel,
  formatLocationLabelForGuid,
} from "../locationDisplay";

describe("formatLocationLabel", () => {
  const t = (key: string) =>
    key === "locations.typeOnline" ? "آنلاین" : key;

  it("uses i18n for an Online-typed location regardless of name", () => {
    expect(
      formatLocationLabel(t, { name: "Hall A", locationType: "Online" }),
    ).toBe("آنلاین");
  });

  it("uses the stored name for a paper location even if it is Online", () => {
    expect(
      formatLocationLabel(t, { name: "Online", locationType: "Manual" }),
    ).toBe("Online");
  });

  it("uses the stored name for a paper location", () => {
    expect(
      formatLocationLabel(t, { name: "Main Hall", locationType: "Manual" }),
    ).toBe("Main Hall");
  });
});

describe("formatLocationLabelForGuid", () => {
  const t = (key: string) =>
    key === "locations.typeOnline" ? "آنلاین" : key;

  const locations = [
    {
      locationGuid: "loc-hall",
      name: "Main Hall",
      locationType: "Manual",
    },
    {
      locationGuid: "loc-online",
      name: "Hall A",
      locationType: "Online",
    },
  ];

  it("looks up type by guid and uses i18n for Online", () => {
    expect(
      formatLocationLabelForGuid(t, locations, "loc-online", "Hall A"),
    ).toBe("آنلاین");
  });

  it("looks up a paper location by guid", () => {
    expect(
      formatLocationLabelForGuid(t, locations, "loc-hall", "ignored"),
    ).toBe("Main Hall");
  });

  it("falls back to the supplied name when the guid is unknown", () => {
    expect(
      formatLocationLabelForGuid(t, locations, "missing", "Fallback"),
    ).toBe("Fallback");
  });
});
