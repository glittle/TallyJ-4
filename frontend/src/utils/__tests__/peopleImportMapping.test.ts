import { describe, expect, it } from "vitest";
import type { ColumnMapping } from "@/types";
import {
  assignTargetToFileColumn,
  isRequiredMappingComplete,
  mappedFieldCount,
  mappingsForHeaders,
  previewValuesForColumn,
  requiredFieldStatus,
  sourceColumnForTarget,
  targetForFileColumn,
} from "../peopleImportMapping";

const headers = ["First Name", "Last Name", "Id"];

function mappings(entries: Array<[string, string | null]>): ColumnMapping[] {
  return entries.map(([fileColumn, targetField]) => ({
    fileColumn,
    targetField,
  }));
}

describe("peopleImportMapping", () => {
  it("finds the target for a file column", () => {
    const current = mappings([
      ["First Name", "FirstName"],
      ["Last Name", "LastName"],
      ["Id", null],
    ]);
    expect(targetForFileColumn(current, "First Name")).toBe("FirstName");
    expect(targetForFileColumn(current, "Id")).toBeNull();
    expect(sourceColumnForTarget(current, "LastName")).toBe("Last Name");
  });

  it("keeps one mapping row per file header", () => {
    const current = mappings([["First Name", "FirstName"]]);
    expect(mappingsForHeaders(current, headers)).toEqual([
      { fileColumn: "First Name", targetField: "FirstName" },
      { fileColumn: "Last Name", targetField: null },
      { fileColumn: "Id", targetField: null },
    ]);
  });

  it("assigns a TallyJ field and clears it from the previous column", () => {
    const current = mappings([
      ["First Name", "FirstName"],
      ["Last Name", "LastName"],
      ["Id", null],
    ]);
    const next = assignTargetToFileColumn(current, headers, "Id", "FirstName");
    expect(targetForFileColumn(next, "Id")).toBe("FirstName");
    expect(targetForFileColumn(next, "First Name")).toBeNull();
  });

  it("clears a file column when unmapped", () => {
    const current = mappings([
      ["First Name", "FirstName"],
      ["Last Name", "LastName"],
    ]);
    const next = assignTargetToFileColumn(current, headers, "First Name", null);
    expect(isRequiredMappingComplete(next)).toBe(false);
    expect(mappedFieldCount(next)).toBe(1);
  });

  it("tracks required first and last name", () => {
    const missingLast = mappings([
      ["First Name", "FirstName"],
      ["Last Name", null],
    ]);
    expect(isRequiredMappingComplete(missingLast)).toBe(false);
    expect(requiredFieldStatus(missingLast)).toEqual([
      { value: "FirstName", label: "First Name", mapped: true },
      { value: "LastName", label: "Last Name", mapped: false },
    ]);
    expect(
      isRequiredMappingComplete(
        mappings([
          ["First Name", "FirstName"],
          ["Last Name", "LastName"],
        ]),
      ),
    ).toBe(true);
  });

  it("returns preview values for a column", () => {
    expect(
      previewValuesForColumn(
        headers,
        [
          ["Minnie", "Mouse", "T-124"],
          ["  ", "Duck", "T-125"],
        ],
        "First Name",
      ),
    ).toEqual(["Minnie"]);
    expect(previewValuesForColumn(headers, [["Minnie"]], null)).toEqual([]);
  });
});
