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

describe("voter auth verify and rate-limit keys", () => {
  it("surfaces expired, used-or-missing, and too-many verify keys", () => {
    expect(
      resolveUserFacingApiError(
        { error: "voting.auth.verify.codeExpired" },
        "Could not verify that code. Please check it and try again.",
      ),
    ).toBe("Verification code has expired. Please request a new code.");

    expect(
      resolveUserFacingApiError(
        { error: "voting.auth.verify.noCodeFound" },
        "Could not verify that code. Please check it and try again.",
      ),
    ).toBe("No verification code found. Please request a new code.");

    expect(
      resolveUserFacingApiError(
        { error: "voting.auth.verify.tooManyAttempts" },
        "Could not verify that code. Please check it and try again.",
      ),
    ).toBe("Too many failed attempts. Please request a new code.");
  });

  it("surfaces the 429 i18n key instead of raw English", () => {
    expect(
      resolveUserFacingApiError(
        { error: "error.tooManyRequests" },
        "Something went wrong",
      ),
    ).toBe("Too many requests. Please try again later.");
  });

  it("surfaces the 413 i18n key instead of raw English", () => {
    expect(
      resolveUserFacingApiError(
        { error: "error.payloadTooLarge" },
        "Something went wrong",
      ),
    ).toBe("This request is too large.");
  });
});
