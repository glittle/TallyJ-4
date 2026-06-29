import { describe, expect, it, beforeEach } from "vitest";
import {
  getComputerCode,
  isValidComputerCode,
  setComputerCode,
} from "../computerCodeStorage";

const electionGuid = "test-election-guid";

describe("computerCodeStorage", () => {
  beforeEach(() => {
    localStorage.clear();
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

  it("validates one- and two-character letter-only codes", () => {
    expect(isValidComputerCode("A")).toBe(true);
    expect(isValidComputerCode("AA")).toBe(true);
    expect(isValidComputerCode("A1")).toBe(false);
    expect(isValidComputerCode("ABC")).toBe(false);
  });
});