import { describe, expect, it } from "vitest";
import { tellerPasscodeLengthError } from "../tellerPasscode";

describe("tellerPasscodeLengthError", () => {
  it("allows an empty passcode", () => {
    expect(tellerPasscodeLengthError("")).toBeNull();
    expect(tellerPasscodeLengthError(undefined)).toBeNull();
  });

  it("rejects a new passcode shorter than 6 characters", () => {
    expect(tellerPasscodeLengthError("short")).toBe("too-short");
    expect(tellerPasscodeLengthError("abc12")).toBe("too-short");
  });

  it("allows a new passcode of 6 characters", () => {
    expect(tellerPasscodeLengthError("secret")).toBeNull();
  });

  it("allows saving the same shorter passcode already stored", () => {
    expect(tellerPasscodeLengthError("abc", "abc")).toBeNull();
  });

  it("rejects changing a stored passcode to a different short value", () => {
    expect(tellerPasscodeLengthError("no", "abc")).toBe("too-short");
  });
});
