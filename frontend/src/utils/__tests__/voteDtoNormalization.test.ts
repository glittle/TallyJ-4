import { describe, expect, it } from "vitest";
import {
  isVoteDtoSpoiled,
  isVoteSpoiled,
  normalizeVoteDto,
  normalizeVoteStatusCode,
  resolveVoteStatus,
} from "../voteDtoNormalization";

describe("normalizeVoteStatusCode", () => {
  it("maps API Ok voteStatus to lowercase ok for the UI", () => {
    expect(normalizeVoteStatusCode("Ok")).toBe("ok");
    expect(normalizeVoteStatusCode("ok")).toBe("ok");
  });

  it("preserves non-ok status codes", () => {
    expect(normalizeVoteStatusCode("X01")).toBe("X01");
    expect(normalizeVoteStatusCode("Spoiled")).toBe("Spoiled");
  });
});

describe("normalizeVoteDto", () => {
  it("maps voteStatus from delete/create responses onto statusCode", () => {
    const vote = normalizeVoteDto({
      rowId: 1,
      ballotGuid: "ballot-1",
      positionOnBallot: 1,
      voteStatus: "Ok",
      statusCode: undefined as unknown as string,
    });

    expect(vote.statusCode).toBe("ok");
    expect(isVoteSpoiled(vote.statusCode)).toBe(false);
  });
});

describe("resolveVoteStatus", () => {
  it("reads voteStatus when statusCode is missing", () => {
    expect(
      resolveVoteStatus({
        voteStatus: "Ok",
      }),
    ).toBe("ok");
    expect(isVoteDtoSpoiled({ voteStatus: "Ok" })).toBe(false);
    expect(isVoteDtoSpoiled({ statusCode: "Ok" })).toBe(false);
    expect(isVoteDtoSpoiled({ statusCode: "X01" })).toBe(true);
    expect(isVoteDtoSpoiled({ voteStatus: "Spoiled" })).toBe(true);
    expect(
      isVoteDtoSpoiled({
        voteStatus: "Spoiled",
        ineligibleReasonCode: "V06",
      }),
    ).toBe(true);
  });
});