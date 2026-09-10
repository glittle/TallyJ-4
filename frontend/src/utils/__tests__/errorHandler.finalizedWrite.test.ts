import { describe, expect, it } from "vitest";
import {
  resolveUserFacingApiError,
  translateIfPhraseKey,
} from "../errorHandler";

describe("finalized write refusal messages", () => {
  it("translates the Finalized people/ballot write key", () => {
    expect(translateIfPhraseKey("elections.finalizedWriteBlocked")).toBe(
      "This election is finalized. People and ballot data cannot be changed.",
    );
  });

  it("leaves ordinary exception text unchanged", () => {
    expect(translateIfPhraseKey("Person not found")).toBe("Person not found");
  });

  it("translates the Finalized online-submit voter key", () => {
    expect(translateIfPhraseKey("voting.submit.finalized")).toBe(
      "This election is finalized. You cannot submit or change an online ballot.",
    );
  });

  it("surfaces a hey-api 400 body error key for online voter submit", () => {
    expect(
      resolveUserFacingApiError(
        { error: "voting.submit.finalized" },
        "Failed to submit ballot. Please try again.",
      ),
    ).toBe(
      "This election is finalized. You cannot submit or change an online ballot.",
    );
  });

  it("translates the cannot-vote-after-accepted-ballot people key", () => {
    expect(translateIfPhraseKey("people.cannotMarkCannotVoteAfterVoted")).toBe(
      "This person has already voted. Their status cannot be changed to cannot vote.",
    );
  });

  it("surfaces a hey-api 400 body message key for Front Desk and people/ballot forms", () => {
    expect(
      resolveUserFacingApiError(
        { message: "elections.finalizedWriteBlocked" },
        "Failed to check in voter",
      ),
    ).toBe(
      "This election is finalized. People and ballot data cannot be changed.",
    );
  });
});
