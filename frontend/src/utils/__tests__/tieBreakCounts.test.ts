import { describe, expect, it } from "vitest";
import type { TieDetailsDto, TiePersonDto } from "../../types";
import {
  clearedTieBreakCount,
  collectTieBreakCounts,
  electedTieMissingCounts,
  isTieBreakCountUnset,
  setClearedTieBreakCount,
} from "../tieBreakCounts";

function person(guid: string, tieBreakCount?: number | null): TiePersonDto {
  return {
    personGuid: guid,
    fullName: guid,
    voteCount: 1,
    tieBreakCount,
  };
}

function tie(
  section: string,
  people: TiePersonDto[],
  group = 1,
): TieDetailsDto {
  return {
    tieBreakGroup: group,
    section,
    people,
    instructions: "",
  };
}

describe("tieBreakCounts", () => {
  it("treats null and undefined as unset, and 0 as entered", () => {
    expect(isTieBreakCountUnset(undefined)).toBe(true);
    expect(isTieBreakCountUnset(null)).toBe(true);
    expect(isTieBreakCountUnset(0)).toBe(false);
    expect(isTieBreakCountUnset(5)).toBe(false);
  });

  it("sends explicit 0 counts and omits unset people", () => {
    expect(
      collectTieBreakCounts([
        tie("E", [
          person("a", 5),
          person("b", 0),
          person("c", null),
          person("d"),
        ]),
      ]),
    ).toEqual([
      { personGuid: "a", tieBreakCount: 5 },
      { personGuid: "b", tieBreakCount: 0 },
    ]);
  });

  it("flags elected ties only when a count is missing, not when it is 0", () => {
    expect(
      electedTieMissingCounts(tie("E", [person("a", 0), person("b", 1)])),
    ).toBe(false);
    expect(
      electedTieMissingCounts(tie("E", [person("a", 0), person("b", null)])),
    ).toBe(true);
    expect(
      electedTieMissingCounts(tie("X", [person("a", null), person("b", null)])),
    ).toBe(false);
  });

  it("clears to an explicit 0 so the server overwrites a previous count", () => {
    const row = person("a", 5);
    setClearedTieBreakCount(row);
    expect(clearedTieBreakCount()).toBe(0);
    expect(row.tieBreakCount).toBe(0);
    expect(collectTieBreakCounts([tie("E", [row])])).toEqual([
      { personGuid: "a", tieBreakCount: 0 },
    ]);
  });
});
