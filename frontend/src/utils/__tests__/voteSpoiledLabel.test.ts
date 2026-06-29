import { describe, it, expect } from "vitest";
import { getVoteSpoiledLabel } from "../voteSpoiledLabel";
import type { VoteDto } from "@/types/Vote";

describe("getVoteSpoiledLabel", () => {
  const t = (key: string) => {
    const labels: Record<string, string> = {
      "eligibility.X01": "Deceased",
    };
    return labels[key] || key;
  };

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
