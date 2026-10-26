import { describe, expect, it } from "vitest";
import {
  isEmptyImportFile,
  peopleImportEmptyFileKey,
} from "../peopleImportUpload";

describe("peopleImportEmptyFileKey", () => {
  it("uses the no-file key for a missing or zero-byte file", () => {
    expect(peopleImportEmptyFileKey(null)).toBe("people.import.noFileProvided");
    expect(peopleImportEmptyFileKey(undefined)).toBe(
      "people.import.noFileProvided",
    );
    expect(peopleImportEmptyFileKey({ size: 0 })).toBe(
      "people.import.noFileProvided",
    );
    expect(isEmptyImportFile({ size: 0 })).toBe(true);
  });

  it("does not flag a file that has bytes", () => {
    expect(peopleImportEmptyFileKey({ size: 12 })).toBeNull();
  });
});
