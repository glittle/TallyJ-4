import { describe, expect, it, beforeEach } from "vitest";
import {
  getComputerCode,
  getComputerCodesState,
  isValidComputerCode,
  resetComputerCodeCache,
  setComputerCode,
} from "../computerCodeStorage";

const electionGuid = "test-election-guid";

describe("computerCodeStorage", () => {
  beforeEach(() => {
    localStorage.clear();
    resetComputerCodeCache();
  });

  it("normalizes and persists a computer code per election", () => {
    setComputerCode(electionGuid, "aa");
    expect(getComputerCode(electionGuid)).toBe("AA");
  });

  it("clears the stored code when empty", () => {
    setComputerCode(electionGuid, "AB");
    setComputerCode(electionGuid, "");
    expect(getComputerCode(electionGuid)).toBe("");
  });

  it("updates the reactive map when a code is assigned", () => {
    setComputerCode(electionGuid, "B");
    expect(getComputerCodesState().value[electionGuid]).toBe("B");
  });

  it("validates one- and two-character letter-only codes", () => {
    expect(isValidComputerCode("A")).toBe(true);
    expect(isValidComputerCode("AA")).toBe(true);
    expect(isValidComputerCode("A1")).toBe(false);
    expect(isValidComputerCode("ABC")).toBe(false);
  });
});
