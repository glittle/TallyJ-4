import { describe, expect, it } from "vitest";
import type { VoteDto } from "@/types/Vote";
import {
  isUnresolvedRawVote,
  namePartsFromRaw,
  nextFindQuery,
  parseOnlineVoteRaw,
  rawVoteDisplayName,
  rawVoteReferenceName,
} from "../onlineVoteRaw";

function vote(overrides: Partial<VoteDto> = {}): VoteDto {
  return {
    rowId: 1,
    ballotGuid: "b",
    positionOnBallot: 1,
    statusCode: "Raw",
    onlineVoteRaw: '{"First":"Jon","Last":"Smyth","OtherInfo":"Jon Smyth"}',
    ...overrides,
  };
}

describe("parseOnlineVoteRaw", () => {
  it("parses v3 PascalCase JSON", () => {
    expect(
      parseOnlineVoteRaw(
        '{"First":"Ada","Last":"Lovelace","OtherInfo":"Ada Lovelace"}',
      ),
    ).toEqual({
      first: "Ada",
      last: "Lovelace",
      otherInfo: "Ada Lovelace",
    });
  });

  it("parses camelCase JSON", () => {
    expect(
      parseOnlineVoteRaw(
        '{"first":"Ada","last":"Lovelace","otherInfo":"note"}',
      ),
    ).toEqual({
      first: "Ada",
      last: "Lovelace",
      otherInfo: "note",
    });
  });

  it("parses a legacy plain name", () => {
    expect(parseOnlineVoteRaw("Jane Marie Doe")).toEqual({
      first: "Jane Marie",
      last: "Doe",
      otherInfo: "Jane Marie Doe",
    });
  });

  it("parses last, first", () => {
    expect(parseOnlineVoteRaw("Doe, Jane")).toEqual({
      first: "Jane",
      last: "Doe",
      otherInfo: "Doe, Jane",
    });
  });

  it("returns null for empty values", () => {
    expect(parseOnlineVoteRaw(null)).toBeNull();
    expect(parseOnlineVoteRaw("")).toBeNull();
  });
});

describe("rawVoteDisplayName", () => {
  it("joins first and last", () => {
    expect(
      rawVoteDisplayName({ first: "Ada", last: "Lovelace", otherInfo: "x" }),
    ).toBe("Ada Lovelace");
  });
});

describe("rawVoteReferenceName", () => {
  it("prefers OtherInfo when present", () => {
    expect(
      rawVoteReferenceName({
        first: "Ada",
        last: "Lovelace",
        otherInfo: "Ada L.",
      }),
    ).toBe("Ada L.");
  });
});

describe("namePartsFromRaw", () => {
  it("uses first and last when stored", () => {
    expect(
      namePartsFromRaw({ first: "Jon", last: "Smyth", otherInfo: "Jon Smyth" }),
    ).toEqual({ first: "Jon", last: "Smyth" });
  });

  it("splits OtherInfo when first and last are empty", () => {
    expect(
      namePartsFromRaw({ first: "", last: "", otherInfo: "Jane Marie Doe" }),
    ).toEqual({ first: "Jane Marie", last: "Doe" });
  });

  it("splits last, first OtherInfo", () => {
    expect(
      namePartsFromRaw({ first: "", last: "", otherInfo: "Doe, Jane" }),
    ).toEqual({ first: "Jane", last: "Doe" });
  });
});

describe("isUnresolvedRawVote", () => {
  it("is true for unresolved OnlineVoteRaw with no person", () => {
    expect(isUnresolvedRawVote(vote())).toBe(true);
  });

  it("is true for an import-style OnlineVoteRaw with no person", () => {
    expect(
      isUnresolvedRawVote(
        vote({
          onlineVoteRaw:
            '{"First":"Cyrus","Last":"Rus","OtherInfo":"cyrus rus"}',
        }),
      ),
    ).toBe(true);
  });

  it("is false after a person is assigned to the raw vote", () => {
    expect(
      isUnresolvedRawVote(
        vote({ personGuid: "p1", statusCode: "ok", personFullName: "Ada" }),
      ),
    ).toBe(false);
  });

  it("is false when onlineVoteRaw is missing", () => {
    expect(
      isUnresolvedRawVote(
        vote({
          onlineVoteRaw: undefined,
          statusCode: "ok",
          personGuid: "p1",
          personFullName: "Ada Lovelace",
        }),
      ),
    ).toBe(false);
  });

  it("is false after the raw line is spoiled without a person", () => {
    expect(
      isUnresolvedRawVote(
        vote({
          statusCode: "Spoiled",
          ineligibleReasonCode: "U01",
        }),
      ),
    ).toBe(false);
  });
});

describe("nextFindQuery", () => {
  const raw = {
    first: "Jonathan",
    last: "Smythe",
    otherInfo: "Jonathan Smythe",
  };

  it("starts with the full first and last names", () => {
    const first = nextFindQuery(raw, "FL", 7, null);
    expect(first.query).toBe("Jonathan Smythe");
    expect(first.state.lastNum).toBe(8);
  });

  it("drops the last letter of each name on the next click", () => {
    const first = nextFindQuery(raw, "FL", 7, null);
    const second = nextFindQuery(raw, "FL", 7, first.state);
    expect(second.query).toBe("Jonatha Smyth");
  });

  it("resets when the names would become empty", () => {
    let state = nextFindQuery(raw, "FL", 7, null).state;
    for (let i = 0; i < 20; i++) {
      state = nextFindQuery(raw, "FL", 7, state).state;
    }
    const reset = nextFindQuery(raw, "FL", 7, state);
    expect(reset.query.length).toBeGreaterThan(0);
  });
});
