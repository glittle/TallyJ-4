import { describe, expect, it } from "vitest";
import { formatNumber } from "../formatNumber";

describe("formatNumber", () => {
  it("formats integers with en thousands separators", () => {
    expect(formatNumber(4365, undefined, "en")).toBe("4,365");
    expect(formatNumber(1_000_000, undefined, "en")).toBe("1,000,000");
  });

  it("formats integers with de thousands separators", () => {
    expect(formatNumber(4365, undefined, "de")).toBe("4.365");
  });

  it("formats small numbers without separators", () => {
    expect(formatNumber(0, undefined, "en")).toBe("0");
    expect(formatNumber(42, undefined, "en")).toBe("42");
  });

  it("respects NumberFormat options", () => {
    expect(
      formatNumber(0.125, { style: "percent", maximumFractionDigits: 1 }, "en"),
    ).toBe("12.5%");
  });

  it("returns a string for non-finite values", () => {
    expect(formatNumber(Number.NaN)).toBe("NaN");
    expect(formatNumber(Number.POSITIVE_INFINITY)).toBe("Infinity");
  });

  it("uses the current i18n locale when none is provided", () => {
    // Default test setup uses English
    const result = formatNumber(4365);
    expect(result).toBe("4,365");
  });
});
