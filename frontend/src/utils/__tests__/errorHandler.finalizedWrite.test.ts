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
