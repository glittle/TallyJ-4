import { describe, it, expect } from "vitest";
import {
  getAssistantTellerMenuPages,
  getAssistantTellerRedirectPath,
  isAssistantTeller,
  isAssistantTellerRouteAllowed,
} from "../assistantTellerAccess";
import type { ElectionStage } from "../electionStages";

const GUID = "abc-123";

describe("assistantTellerAccess", () => {
  describe("isAssistantTeller", () => {
    it("returns true for passcode teller auth", () => {
      expect(
        isAssistantTeller({ name: "Teller", authMethod: "AccessCode" } as any),
      ).toBe(true);
    });

    it("returns false for full tellers and regular users", () => {
      expect(
        isAssistantTeller({ name: "Alice", authMethod: "Local" } as any),
      ).toBe(false);
      expect(
        isAssistantTeller({ email: "a@b.com", authMethod: "Google" } as any),
      ).toBe(false);
    });
  });

  describe("getAssistantTellerMenuPages", () => {
    it("returns no menu items during SettingUp", () => {
      expect(getAssistantTellerMenuPages("SettingUp", GUID)).toEqual([]);
    });

    it("returns Front Desk during GatheringBallots", () => {
      const pages = getAssistantTellerMenuPages("GatheringBallots", GUID);
      expect(pages).toHaveLength(1);
      expect(pages[0]!.key).toBe("frontdesk");
    });

    it("returns no menu items during ProcessingBallots (entry uses per-ballot URLs)", () => {
      expect(getAssistantTellerMenuPages("ProcessingBallots", GUID)).toEqual(
        [],
      );
    });

    it("returns landing and final results during Finalized", () => {
      const pages = getAssistantTellerMenuPages("Finalized", GUID);
      expect(pages.map((p) => p.key)).toEqual(["landing", "final-results"]);
    });
  });

  describe("isAssistantTellerRouteAllowed", () => {
    const cases: Array<{
      stage: ElectionStage;
      path: string;
      allowed: boolean;
    }> = [
      {
        stage: "SettingUp",
        path: `/elections/${GUID}`,
        allowed: true,
      },
      {
        stage: "SettingUp",
        path: `/elections/${GUID}/frontdesk`,
        allowed: false,
      },
      {
        stage: "GatheringBallots",
        path: `/elections/${GUID}/frontdesk`,
        allowed: true,
      },
      {
        stage: "GatheringBallots",
        path: `/elections/${GUID}/ballots`,
        allowed: false,
      },
      {
        stage: "ProcessingBallots",
        path: `/elections/${GUID}/ballots/some-ballot/entry`,
        allowed: true,
      },
      {
        stage: "ProcessingBallots",
        path: `/elections/${GUID}/ballots`,
        allowed: false,
      },
      {
        stage: "ProcessingBallots",
        path: `/elections/${GUID}/tally`,
        allowed: false,
      },
      {
        stage: "Finalized",
        path: `/elections/${GUID}/results`,
        allowed: true,
      },
      {
        stage: "Finalized",
        path: `/elections/${GUID}/edit`,
        allowed: false,
      },
    ];

    it.each(cases)(
      "$stage allows $path = $allowed",
      ({ stage, path, allowed }) => {
        expect(isAssistantTellerRouteAllowed(path, GUID, stage)).toBe(allowed);
      },
    );
  });

  describe("getAssistantTellerRedirectPath", () => {
    it("redirects GatheringBallots to Front Desk", () => {
      expect(getAssistantTellerRedirectPath(GUID, "GatheringBallots")).toBe(
        `/elections/${GUID}/frontdesk`,
      );
    });

    it("redirects SettingUp to election landing", () => {
      expect(getAssistantTellerRedirectPath(GUID, "SettingUp")).toBe(
        `/elections/${GUID}`,
      );
    });

    it("redirects Finalized to election landing first", () => {
      expect(getAssistantTellerRedirectPath(GUID, "Finalized")).toBe(
        `/elections/${GUID}`,
      );
    });
  });
});