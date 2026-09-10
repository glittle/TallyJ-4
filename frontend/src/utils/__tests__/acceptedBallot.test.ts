import { describe, expect, it } from "vitest";
import {
  applyBallotNotReceivedFilter,
  hasAcceptedBallot,
  isCannotVoteReasonDisabled,
  voterHasReceivedBallot,
} from "../acceptedBallot";

describe("acceptedBallot", () => {
  it("treats a voting method or accepted online ballot as already voted", () => {
    expect(hasAcceptedBallot({ votingMethod: "P" })).toBe(true);
    expect(hasAcceptedBallot({ hasAcceptedBallot: true })).toBe(true);
    expect(
      hasAcceptedBallot({ votingMethod: " ", hasAcceptedBallot: false }),
    ).toBe(false);
    expect(hasAcceptedBallot({})).toBe(false);
  });

  it("treats a voting method as ballot received (v3 VM-)", () => {
    expect(voterHasReceivedBallot({ votingMethod: "M" })).toBe(true);
    expect(voterHasReceivedBallot({ votingMethod: "" })).toBe(false);
    expect(voterHasReceivedBallot({})).toBe(false);
  });

  it("hides people whose ballot has been received when the checkbox is on", () => {
    const voters = [
      { personGuid: "a", votingMethod: "P", flags: "Mailed" },
      { personGuid: "b", votingMethod: undefined, flags: "Mailed" },
      { personGuid: "c", votingMethod: "O" },
    ];

    expect(applyBallotNotReceivedFilter(voters, false)).toEqual(voters);
    expect(
      applyBallotNotReceivedFilter(voters, true).map((v) => v.personGuid),
    ).toEqual(["b"]);
  });

  it("disables cannot-vote reasons only after a ballot is accepted", () => {
    expect(isCannotVoteReasonDisabled({ canVote: false }, true)).toBe(true);
    expect(isCannotVoteReasonDisabled({ canVote: true }, true)).toBe(false);
    expect(isCannotVoteReasonDisabled({ canVote: false }, false)).toBe(false);
  });
});
