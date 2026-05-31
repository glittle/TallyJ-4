import { describe, it, expect } from "vitest";
import { STAGES, STAGE_META, STAGE_PAGES } from "../electionStages";

describe("electionStages", () => {
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
