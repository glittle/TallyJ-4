export function isEmptyImportFile(
  file: { size?: number } | null | undefined,
): boolean {
  return !file || (file.size ?? 0) === 0;
}

/** Client-side key for a zero-byte picker selection. The API uses the same key. */
export function peopleImportEmptyFileKey(
  file: { size?: number } | null | undefined,
): "people.import.noFileProvided" | null {
  return isEmptyImportFile(file) ? "people.import.noFileProvided" : null;
}
