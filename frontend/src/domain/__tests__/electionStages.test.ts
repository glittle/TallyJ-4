import { describe, it, expect } from "vitest";
import {
  STAGES,
  STAGE_META,
  STAGE_PAGES,
  tallyStatusToStage,
  stageToTallyStatus,
  type ElectionStage,
} from "../electionStages";

describe("electionStages", () => {
  describe("tallyStatusToStage", () => {
    it.each([
      ["Setup", "SettingUp"],
      ["Draft", "SettingUp"],
      ["Voting", "GatheringBallots"],
      ["Counting", "GatheringBallots"],
      ["Gathering", "GatheringBallots"],
      ["Tallying", "ProcessingBallots"],
      ["Finalized", "ProcessingBallots"],
      ["Processing", "ProcessingBallots"],
      ["Archived", "ProcessingBallots"],
    ] as const)("maps legacy %s to %s", (legacy, expected) => {
      expect(tallyStatusToStage(legacy)).toBe(expected);
    });

    it("maps undefined to SettingUp", () => {
      expect(tallyStatusToStage(undefined)).toBe("SettingUp");
    });

    it("maps null to SettingUp", () => {
      expect(tallyStatusToStage(null)).toBe("SettingUp");
    });

    it("maps unknown values to SettingUp", () => {
      expect(tallyStatusToStage("Bogus")).toBe("SettingUp");
    });
  });

  describe("stageToTallyStatus", () => {
    it.each([
      ["SettingUp", "Setup"],
      ["GatheringBallots", "Counting"],
      ["ProcessingBallots", "Finalized"],
    ] as const)("maps stage %s to canonical %s", (stage, expected) => {
      expect(stageToTallyStatus(stage as ElectionStage)).toBe(expected);
    });

    it("round-trips canonical persisted values", () => {
      for (const stage of STAGES) {
        expect(tallyStatusToStage(stageToTallyStatus(stage))).toBe(stage);
      }
    });
  });

  describe("STAGES + STAGE_META", () => {
    it("contains the three canonical stages in order", () => {
      expect(STAGES).toEqual([
        "SettingUp",
        "GatheringBallots",
        "ProcessingBallots",
      ]);
    });

    it("has metadata for every stage", () => {
      for (const stage of STAGES) {
        const meta = STAGE_META[stage];
        expect(meta).toBeDefined();
        expect(meta.key).toBe(stage);
        expect(meta.i18nKey).toMatch(/^elections\.stage\./);
        expect(meta.colorVar).toMatch(/^--color-stage-/);
        expect(meta.bgVar).toMatch(/^--color-stage-.+-bg$/);
        expect(meta.icon).toBeDefined();
      }
    });
  });

  describe("STAGE_PAGES", () => {
    it("has at least one page per stage", () => {
      for (const stage of STAGES) {
        expect(STAGE_PAGES[stage].length).toBeGreaterThan(0);
      }
    });

    it("produces /elections/:guid route paths", () => {
      const guid = "abc-123";
      for (const stage of STAGES) {
        for (const page of STAGE_PAGES[stage]) {
          const path = page.routePath(guid);
          expect(path).toMatch(/^\/elections\/abc-123(\/|$)/);
        }
      }
    });

    it("marks Setting Up pages as admin-only", () => {
      for (const page of STAGE_PAGES.SettingUp) {
        expect(page.adminOnly).toBe(true);
      }
    });

    it("does not mark Front Desk as admin-only (teller-visible)", () => {
      const frontdesk = STAGE_PAGES.GatheringBallots.find(
        (p) => p.key === "frontdesk",
      );
      expect(frontdesk).toBeDefined();
      expect(frontdesk?.adminOnly).toBeFalsy();
    });

    it("does not mark Ballot Management as admin-only (teller-visible)", () => {
      const ballots = STAGE_PAGES.ProcessingBallots.find(
        (p) => p.key === "ballots",
      );
      expect(ballots).toBeDefined();
      expect(ballots?.adminOnly).toBeFalsy();
    });
  });
});
