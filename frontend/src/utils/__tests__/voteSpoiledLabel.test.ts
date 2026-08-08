import { describe, it, expect } from "vitest";
import {
  getIneligibleReasonLabel,
  getVoteSpoiledLabel,
} from "../voteSpoiledLabel";
import type { VoteDto } from "@/types/Vote";

const t = (key: string) => {
  const labels: Record<string, string> = {
    "eligibility.X01": "Deceased",
    "ballots.spoiled": "Spoiled",
    "ballots.ineligible": "Ineligible",
  };
  return labels[key] || key;
};

describe("getIneligibleReasonLabel", () => {
  it("defaults empty code to spoiled label (vote context)", () => {
    expect(getIneligibleReasonLabel(t, null)).toBe("Spoiled");
    expect(getIneligibleReasonLabel(t, undefined)).toBe("Spoiled");
    expect(getIneligibleReasonLabel(t, "  ")).toBe("Spoiled");
  });

  it("accepts a custom empty fallback for person search badges", () => {
    expect(
      getIneligibleReasonLabel(t, null, "ballots.ineligible"),
    ).toBe("Ineligible");
  });

  it("returns translated eligibility label when code is present", () => {
    expect(getIneligibleReasonLabel(t, "X01")).toBe("Deceased");
  });
});

describe("getVoteSpoiledLabel", () => {

  it("returns translated eligibility label from ineligibleReasonCode", () => {
    const vote: VoteDto = {
      rowId: 1,
      ballotGuid: "ballot-1",
      positionOnBallot: 1,
      statusCode: "Spoiled",
      ineligibleReasonCode: "X01",
    };

    expect(getVoteSpoiledLabel(t, vote)).toBe("Deceased");
  });

  it("falls back to legacy statusCode reason codes", () => {
    const vote: VoteDto = {
      rowId: 1,
      ballotGuid: "ballot-1",
      positionOnBallot: 1,
      statusCode: "X01",
    };

    expect(getVoteSpoiledLabel(t, vote)).toBe("Deceased");
  });

  it("falls back to the status code when no translation exists", () => {
    const vote: VoteDto = {
      rowId: 1,
      ballotGuid: "ballot-1",
      positionOnBallot: 1,
      statusCode: "Spoiled",
      ineligibleReasonCode: "ZZ9",
    };

    expect(getVoteSpoiledLabel(t, vote)).toBe("ZZ9");
  });
});
