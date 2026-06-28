import { describe, expect, it } from "vitest";
import type { BallotSummaryDto } from "@/utils/ballotSummary";
import {
  ALL_BALLOTS_FILTER,
  buildBallotViewFilterGroups,
  computerFilterValue,
  defaultBallotViewFilter,
  filterBallotsByView,
  locationFilterValue,
  parseBallotViewFilter,
} from "../ballotViewFilter";

function ballot(
  code: string,
  locationGuid: string,
  computerCode: string,
): BallotSummaryDto {
  return {
    ballotGuid: `${locationGuid}-${computerCode}-${code}`,
    ballotCode: code,
    locationGuid,
    locationName: locationGuid,
    ballotNumAtComputer: 1,
    computerCode,
    statusCode: "Ok",
    voteCount: 1,
  };
}

describe("ballotViewFilter", () => {
  const ballots = [
    ballot("A1", "loc-main", "AA"),
    ballot("A2", "loc-main", "BB"),
    ballot("B1", "loc-side", "AA"),
  ];

  it("filters ballots by location", () => {
    const filtered = filterBallotsByView(
      ballots,
      locationFilterValue("loc-main"),
    );
    expect(filtered.map((item) => item.ballotCode)).toEqual(["A1", "A2"]);
  });

  it("filters ballots by computer at a location", () => {
    const filtered = filterBallotsByView(
      ballots,
      computerFilterValue("loc-side", "AA"),
    );
    expect(filtered).toHaveLength(1);
    expect(filtered[0].ballotCode).toBe("B1");
  });

  it("filters ballots by computer across all locations", () => {
    const filtered = filterBallotsByView(
      ballots,
      computerFilterValue(null, "AA"),
    );
    expect(filtered.map((item) => item.ballotCode)).toEqual(["A1", "B1"]);
  });

  it("builds grouped computer codes per location", () => {
    const groups = buildBallotViewFilterGroups(
      [
        {
          locationGuid: "loc-main",
          electionGuid: "election-1",
          name: "Main Hall",
          sortOrder: 1,
        },
        {
          locationGuid: "loc-side",
          electionGuid: "election-1",
          name: "Side Room",
          sortOrder: 2,
        },
      ],
      ballots,
      {
        "loc-main": [
          {
            computerGuid: "c1",
            electionGuid: "election-1",
            locationGuid: "loc-main",
            computerCode: "CC",
          },
        ],
      },
    );

    expect(groups).toHaveLength(2);
    expect(groups[0].computerCodes).toEqual(["AA", "BB", "CC"]);
    expect(groups[1].computerCodes).toEqual(["AA"]);
  });

  it("defaults to the current computer at the selected location", () => {
    expect(defaultBallotViewFilter("AA", "loc-main")).toBe(
      computerFilterValue("loc-main", "AA"),
    );
    expect(defaultBallotViewFilter("AA", null)).toBe(
      computerFilterValue(null, "AA"),
    );
    expect(defaultBallotViewFilter("", "loc-main")).toBe(ALL_BALLOTS_FILTER);
  });

  it("round-trips parsed filter values", () => {
    const value = computerFilterValue("loc-main", "AA");
    expect(parseBallotViewFilter(value)).toEqual({
      type: "computer",
      locationGuid: "loc-main",
      computerCode: "AA",
    });
  });
});
