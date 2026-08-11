import { describe, expect, it } from "vitest";
import {
  ballotGuidFromRouteParams,
  electionBallotEntryPath,
  electionBallotsPath,
} from "../ballotRoutes";

describe("ballotRoutes", () => {
  it("builds management path without a ballot", () => {
    expect(electionBallotsPath("elec-1")).toBe("/elections/elec-1/ballots");
  });

  it("builds management path that opens a specific ballot", () => {
    expect(electionBallotsPath("elec-1", "ballot-guid")).toBe(
      "/elections/elec-1/ballot/ballot-guid",
    );
  });

  it("builds full-page entry path", () => {
    expect(electionBallotEntryPath("elec-1", "ballot-guid")).toBe(
      "/elections/elec-1/ballots/ballot-guid/entry",
    );
  });

  it("reads ballot guid from route params", () => {
    expect(ballotGuidFromRouteParams({ ballotId: "bg-1" })).toBe("bg-1");
    expect(ballotGuidFromRouteParams({ ballotId: ["bg-2"] })).toBe("bg-2");
    expect(ballotGuidFromRouteParams({})).toBeNull();
    expect(ballotGuidFromRouteParams({ ballotId: "  " })).toBeNull();
  });
});
