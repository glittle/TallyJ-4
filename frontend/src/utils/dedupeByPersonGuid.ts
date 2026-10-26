/** Keep the first row for each person. Search must not list the same person twice. */
export function dedupeByPersonGuid<T extends { personGuid: string }>(
  people: T[],
): T[] {
  const seen = new Set<string>();
  const unique: T[] = [];
  for (const person of people) {
    const key = person.personGuid.toLowerCase();
    if (!key || seen.has(key)) {
      continue;
    }
    seen.add(key);
    unique.push(person);
  }
  return unique;
}
