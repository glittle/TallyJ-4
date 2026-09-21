import { describe, expect, it } from "vitest";
import {
  getFlagFilterStyle,
  getMethodFilterStyle,
} from "../frontDeskStyles";

describe("frontDeskStyles theme tokens", () => {
  it("paints inactive method chips with the blank fill, not frozen white", () => {
    const style = getMethodFilterStyle("In Person", false);
    expect(style.backgroundColor).toBe("var(--el-fill-color-blank)");
    expect(style.color).toBe("#10b981");
  });

  it("uses the Front Desk inverse-text token on active method chips", () => {
    const style = getMethodFilterStyle("Mail", true);
    expect(style.color).toBe("var(--color-frontdesk-filter-active-text)");
    expect(style.backgroundColor).toBe("#3b82f6");
  });

  it("applies the same fill tokens to flag chips", () => {
    const flags = ["Youth"];
    expect(getFlagFilterStyle("Youth", flags, false).backgroundColor).toBe(
      "var(--el-fill-color-blank)",
    );
    expect(getFlagFilterStyle("Youth", flags, true).color).toBe(
      "var(--color-frontdesk-filter-active-text)",
    );
  });
});
