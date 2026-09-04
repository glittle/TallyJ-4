import { afterEach, describe, expect, it } from "vitest";
import {
  applyDocumentLocale,
  isRtlLocale,
  localeTextDirection,
} from "../localeDirection";

describe("localeDirection", () => {
  afterEach(() => {
    document.documentElement.lang = "en";
    document.documentElement.dir = "ltr";
  });

  it("treats Arabic and Persian as RTL", () => {
    expect(isRtlLocale("ar")).toBe(true);
    expect(isRtlLocale("fa")).toBe(true);
    expect(localeTextDirection("ar")).toBe("rtl");
    expect(localeTextDirection("fa")).toBe("rtl");
  });

  it("treats other supported locales as LTR", () => {
    expect(isRtlLocale("en")).toBe(false);
    expect(isRtlLocale("zh")).toBe(false);
    expect(localeTextDirection("en")).toBe("ltr");
    expect(localeTextDirection("fr")).toBe("ltr");
  });

  it("sets html lang and dir together", () => {
    applyDocumentLocale("fa");
    expect(document.documentElement.lang).toBe("fa");
    expect(document.documentElement.dir).toBe("rtl");

    applyDocumentLocale("en");
    expect(document.documentElement.lang).toBe("en");
    expect(document.documentElement.dir).toBe("ltr");
  });
});
