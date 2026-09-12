import type { TieCountDto, TieDetailsDto, TiePersonDto } from "../types";

/** True when no count has been entered. Explicit 0 is entered. */
export function isTieBreakCountUnset(
  tieBreakCount: number | null | undefined,
): boolean {
  return tieBreakCount === null || tieBreakCount === undefined;
}

/**
 * Collect entered tie-break counts, including explicit 0.
 * Unset (null/undefined) people are omitted so the server leaves them null.
 */
export function collectTieBreakCounts(ties: TieDetailsDto[]): TieCountDto[] {
  const counts: TieCountDto[] = [];

  for (const tie of ties) {
    for (const person of tie.people) {
      const count = person.tieBreakCount;
      if (count !== null && count !== undefined) {
        counts.push({
          personGuid: person.personGuid,
          tieBreakCount: count,
        });
      }
    }
  }

  return counts;
}

/** Elected-section ties still have someone with no entered count (0 is entered). */
export function electedTieMissingCounts(tie: TieDetailsDto): boolean {
  if (tie.section !== "E") {
    return false;
  }

  return tie.people.some((person) =>
    isTieBreakCountUnset(person.tieBreakCount),
  );
}

/** Clear writes an explicit 0 so the server overwrites a previous count. */
export function clearedTieBreakCount(): number {
  return 0;
}

export function setClearedTieBreakCount(person: TiePersonDto): void {
  person.tieBreakCount = clearedTieBreakCount();
}
