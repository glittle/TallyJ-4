import { describe, it, expect } from "vitest";
import { createI18n } from "vue-i18n";
import {
  translateAnalysisBlocker,
  translateElectionStageChangeError,
} from "../electionStageErrorMessages";

const i18n = createI18n({
  legacy: false,
  locale: "en",
  messages: {
    en: {
      "elections.stageChangeError.generic": "Failed to change election stage",
      "elections.stageChangeError.analysisNotReady":
        "Election analysis is not complete or ready for finalization",
      "elections.stageChangeError.ballotsNeedReview":
        "{count} ballot still needs review | {count} ballots still need review",
      "elections.stageChangeError.ballotsOutstanding":
        "{count} ballot has outstanding issues | {count} ballots have outstanding issues",
      "elections.stageChangeError.unresolvedTies":
        "Unresolved ties must be broken before finalizing",
      "elections.stageChangeError.countsDoNotReconcile":
        "{count} count-reconciliation mismatch must be resolved before analyzing or finalizing | {count} count-reconciliation mismatches must be resolved before analyzing or finalizing",
      "elections.stageChangeError.onlineVotingStillOpen":
        "Close the online voting window before finalizing this election.",
      "tally.reconciliation.blocked":
        "{count} mismatch must be resolved before Analyze or Finalize. | {count} mismatches must be resolved before Analyze or Finalize.",
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
    ).toBe("3 ballots still need review");
  });

  it("translates ballotsOutstanding with a count parameter", () => {
    expect(
      translateElectionStageChangeError(
        "elections.stageChangeError.ballotsOutstanding|count=3",
        t,
      ),
    ).toBe("3 ballots have outstanding issues");
  });

  it("uses the singular when one ballot is outstanding", () => {
    expect(
      translateElectionStageChangeError(
        "elections.stageChangeError.ballotsOutstanding|count=1",
        t,
      ),
    ).toBe("1 ballot has outstanding issues");
  });

  it("translates multiple phrase keys separated by semicolons", () => {
    expect(
      translateElectionStageChangeError(
        "elections.stageChangeError.ballotsNeedReview|count=3; elections.stageChangeError.unresolvedTies",
        t,
      ),
    ).toBe(
      "3 ballots still need review; Unresolved ties must be broken before finalizing",
    );
  });

  it("translates count-reconciliation phrase keys", () => {
    expect(
      translateElectionStageChangeError(
        "elections.stageChangeError.countsDoNotReconcile|count=4",
        t,
      ),
    ).toBe(
      "4 count-reconciliation mismatches must be resolved before analyzing or finalizing",
    );
  });

  it("translates the online-window-still-open finalize key", () => {
    expect(
      translateElectionStageChangeError(
        "elections.stageChangeError.onlineVotingStillOpen",
        t,
      ),
    ).toBe("Close the online voting window before finalizing this election.");
  });

  it("falls back to generic message for unknown keys", () => {
    expect(translateElectionStageChangeError("some.unknown.key", t)).toBe(
      "Failed to change election stage",
    );
  });
});

describe("translateAnalysisBlocker", () => {
  const t = i18n.global.t;

  it("uses the Calculate mismatch sentence for count reconciliation", () => {
    expect(
      translateAnalysisBlocker(
        "elections.stageChangeError.countsDoNotReconcile|count=1",
        t,
      ),
    ).toBe("1 mismatch must be resolved before Analyze or Finalize.");
  });

  it("uses the stage phrase for outstanding ballots", () => {
    expect(
      translateAnalysisBlocker(
        "elections.stageChangeError.ballotsOutstanding|count=3",
        t,
      ),
    ).toBe("3 ballots have outstanding issues");
  });

  it("returns an empty string when Analyze is not blocked", () => {
    expect(translateAnalysisBlocker(null, t)).toBe("");
    expect(translateAnalysisBlocker("", t)).toBe("");
  });
});
