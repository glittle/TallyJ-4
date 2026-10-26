import { describe, expect, it } from "vitest";
import { expectedPhoneRegionsError } from "../expectedPhoneRegions";

describe("expectedPhoneRegionsError", () => {
  it("accepts empty and comma or space separated ISO codes", () => {
    expect(expectedPhoneRegionsError("")).toBeNull();
    expect(expectedPhoneRegionsError("   ")).toBeNull();
    expect(expectedPhoneRegionsError(null)).toBeNull();
    expect(expectedPhoneRegionsError("CA, US")).toBeNull();
    expect(expectedPhoneRegionsError("CA,US")).toBeNull();
    expect(expectedPhoneRegionsError("ca;us")).toBeNull();
  });

  it("rejects names that are not ISO codes", () => {
    expect(expectedPhoneRegionsError("USA")).toBe("invalid");
    expect(expectedPhoneRegionsError("UK")).toBe("invalid");
    expect(expectedPhoneRegionsError("Canada")).toBe("invalid");
    expect(expectedPhoneRegionsError("CA, USA")).toBe("invalid");
  });

  it("rejects a list longer than 80 characters", () => {
    expect(expectedPhoneRegionsError(`${"CA ".repeat(40)}US`)).toBe("tooLong");
  });
});
