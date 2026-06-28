import { describe, expect, it, beforeEach } from "vitest";
import {
  getComputerCode,
  isValidComputerCode,
  setComputerCode,
} from "../computerCodeStorage";

describe("computerCodeStorage", () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it("normalizes and persists a computer code", () => {
    setComputerCode("aa");
    expect(getComputerCode()).toBe("AA");
  });

  it("clears the stored code when empty", () => {
    setComputerCode("AB");
    setComputerCode("");
    expect(getComputerCode()).toBe("");
  });

  it("validates two-character alphanumeric codes", () => {
    expect(isValidComputerCode("AA")).toBe(true);
    expect(isValidComputerCode("A1")).toBe(true);
    expect(isValidComputerCode("A")).toBe(false);
    expect(isValidComputerCode("ABC")).toBe(false);
  });
});
