import type { ColumnMapping } from "@/types";
import { PEOPLE_TARGET_FIELDS } from "@/types";

export function sourceColumnForTarget(
  mappings: ColumnMapping[],
  targetField: string,
): string | null {
  return (
    mappings.find((m) => m.targetField === targetField)?.fileColumn ?? null
  );
}

export function targetForFileColumn(
  mappings: ColumnMapping[],
  fileColumn: string,
): string | null {
  return mappings.find((m) => m.fileColumn === fileColumn)?.targetField ?? null;
}

export function mappingsForHeaders(
  mappings: ColumnMapping[],
  headers: string[],
): ColumnMapping[] {
  return headers.map((header) => {
    const existing = mappings.find((m) => m.fileColumn === header);
    return {
      fileColumn: header,
      targetField: existing?.targetField ?? null,
    };
  });
}

export function assignTargetToFileColumn(
  mappings: ColumnMapping[],
  headers: string[],
  fileColumn: string,
  targetField: string | null,
): ColumnMapping[] {
  const next = mappingsForHeaders(mappings, headers);

  if (targetField) {
    for (const mapping of next) {
      if (
        mapping.targetField === targetField &&
        mapping.fileColumn !== fileColumn
      ) {
        mapping.targetField = null;
      }
    }
  }

  const mapping = next.find((item) => item.fileColumn === fileColumn);
  if (mapping) {
    mapping.targetField = targetField;
  }

  return next;
}

export function requiredFieldStatus(mappings: ColumnMapping[]) {
  return PEOPLE_TARGET_FIELDS.filter((field) => field.required).map(
    (field) => ({
      value: field.value,
      label: field.label,
      mapped: mappings.some((mapping) => mapping.targetField === field.value),
    }),
  );
}

export function isRequiredMappingComplete(mappings: ColumnMapping[]): boolean {
  return (
    mappings.some((m) => m.targetField === "FirstName") &&
    mappings.some((m) => m.targetField === "LastName")
  );
}

export function mappedFieldCount(mappings: ColumnMapping[]): number {
  return mappings.filter((m) => m.targetField).length;
}

/** Samples packed as virtual rows: previewRows[sampleIndex][columnIndex]. */
export function previewValuesForColumn(
  headers: string[],
  previewRows: string[][] | undefined,
  fileColumn: string | null,
  limit = 3,
): string[] {
  if (!fileColumn || !previewRows?.length) {
    return [];
  }
  const index = headers.indexOf(fileColumn);
  if (index < 0) {
    return [];
  }
  return previewRows
    .slice(0, limit)
    .map((row) => row[index] ?? "")
    .map((value) => value.trim())
    .filter((value) => value.length > 0);
}
