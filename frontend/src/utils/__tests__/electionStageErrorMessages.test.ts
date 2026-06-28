import { describe, it, expect } from "vitest";
import { createI18n } from "vue-i18n";
import { translateElectionStageChangeError } from "../electionStageErrorMessages";

const i18n = createI18n({
  legacy: false,
  locale: "en",
  messages: {
    en: {
      "elections.stageChangeError.generic": "Failed to change election stage",
      "elections.stageChangeError.analysisNotReady":
        "Election analysis is not complete or ready for finalization",
      "elections.stageChangeError.ballotsNeedReview":
        "{count} ballot(s) still need review",
      "elections.stageChangeError.unresolvedTies":
        "Unresolved ties must be broken before finalizing",
    },
  },
});

describe("translateElectionStageChangeError", () => {
  const t = i18n.global.t;

  it("translates a server phrase key", () => {
    expect(
      translateElectionStageChangeError(
        "elections.stageChangeError.analysisNotReady",
        t,
      ),
    ).toBe("Election analysis is not complete or ready for finalization");
  });

  it("translates phrase keys with parameters", () => {
    expect(
      translateElectionStageChangeError(
        "elections.stageChangeError.ballotsNeedReview|count=3",
        t,
      ),
    ).toBe("3 ballot(s) still need review");
  });

  it("translates multiple phrase keys separated by semicolons", () => {
    expect(
      translateElectionStageChangeError(
        "elections.stageChangeError.ballotsNeedReview|count=3; elections.stageChangeError.unresolvedTies",
        t,
      ),
    ).toBe(
      "3 ballot(s) still need review; Unresolved ties must be broken before finalizing",
    );
  });

  it("falls back to generic message for unknown keys", () => {
    expect(translateElectionStageChangeError("some.unknown.key", t)).toBe(
      "Failed to change election stage",
    );
  });
});
