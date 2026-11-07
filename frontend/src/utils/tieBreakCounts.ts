import type { TieCountDto, TieDetailsDto, TiePersonDto } from "../types";

/** True when no count has been entered. Explicit 0 is entered. */
export function isTieBreakCountUnset(
  tieBreakCount: number | null | undefined,
): boolean {
  return tieBreakCount === null || tieBreakCount === undefined;
}

/**
 * Collect tie-break counts for everyone in the tie list.
 * Explicit 0 is a runoff result. Null clears a previous count.
 * People left out of this list keep whatever is already stored.
 */
export function collectTieBreakCounts(ties: TieDetailsDto[]): TieCountDto[] {
  const counts: TieCountDto[] = [];

  for (const tie of ties) {
    for (const person of tie.people) {
      counts.push({
        personGuid: person.personGuid,
        tieBreakCount: person.tieBreakCount ?? null,
      });
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

/** Clear unsets the count. 0 stays available as an explicit runoff result. */
export function clearedTieBreakCount(): number | null {
  return null;
}

export function setClearedTieBreakCount(person: TiePersonDto): void {
  person.tieBreakCount = clearedTieBreakCount();
}

/**
 * Votes reports show `/ count` whenever a runoff result was entered,
 * including after the tie is resolved. Unset (null/undefined) omits the suffix.
 * Explicit 0 is an entered result.
 */
export function formatVoteCountWithTieBreak(
  voteCount: number,
  tieBreakCount: number | null | undefined,
): string {
  if (isTieBreakCountUnset(tieBreakCount)) {
    return String(voteCount);
  }

  return `${voteCount} / ${tieBreakCount}`;
}
