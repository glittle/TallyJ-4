import { describe, it, expect } from "vitest";
import {
  getGuestTellerMenuPages,
  getGuestTellerRedirectPath,
  isFullTeller,
  isGuestTeller,
  isGuestTellerRouteAllowed,
} from "../guestTellerAccess";
import type { ElectionStage } from "../electionStages";

const GUID = "abc-123";

describe("guestTellerAccess", () => {
  describe("isGuestTeller", () => {
    it("returns true for passcode GuestTeller auth", () => {
      expect(
        isGuestTeller({ name: "Teller", authMethod: "AccessCode" } as any),
      ).toBe(true);
    });

    it("returns false for FullTellers and regular users", () => {
      expect(isGuestTeller({ name: "Alice", authMethod: "Local" } as any)).toBe(
        false,
      );
      expect(
        isGuestTeller({ email: "a@b.com", authMethod: "Google" } as any),
      ).toBe(false);
    });
  });

  describe("isFullTeller", () => {
    it("returns false for GuestTeller auth", () => {
      expect(
        isFullTeller({ name: "Teller", authMethod: "AccessCode" } as any),
      ).toBe(false);
    });

    it("returns true for FullTeller and officer auth", () => {
      expect(isFullTeller({ name: "Alice", authMethod: "Local" } as any)).toBe(
        true,
      );
      expect(
        isFullTeller({ email: "a@b.com", authMethod: "Google" } as any),
      ).toBe(true);
    });
  });

  describe("getGuestTellerMenuPages", () => {
    it("returns no menu items during SettingUp", () => {
      expect(getGuestTellerMenuPages("SettingUp", GUID)).toEqual([]);
    });

    it("returns Front Desk during GatheringBallots", () => {
      const pages = getGuestTellerMenuPages("GatheringBallots", GUID);
      expect(pages).toHaveLength(1);
      expect(pages[0]!.key).toBe("frontdesk");
    });

    it("returns no menu items during ProcessingBallots (entry uses per-ballot URLs)", () => {
      expect(getGuestTellerMenuPages("ProcessingBallots", GUID)).toEqual([]);
    });

    it("returns landing and final results during Finalized", () => {
      const pages = getGuestTellerMenuPages("Finalized", GUID);
      expect(pages.map((p) => p.key)).toEqual(["landing", "final-results"]);
    });
  });

  describe("isGuestTellerRouteAllowed", () => {
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
        expect(isGuestTellerRouteAllowed(path, GUID, stage)).toBe(allowed);
      },
    );
  });

  describe("getGuestTellerRedirectPath", () => {
    it("redirects GatheringBallots to Front Desk", () => {
      expect(getGuestTellerRedirectPath(GUID, "GatheringBallots")).toBe(
        `/elections/${GUID}/frontdesk`,
      );
    });

    it("redirects SettingUp to election landing", () => {
      expect(getGuestTellerRedirectPath(GUID, "SettingUp")).toBe(
        `/elections/${GUID}`,
      );
    });

    it("redirects Finalized to election landing first", () => {
      expect(getGuestTellerRedirectPath(GUID, "Finalized")).toBe(
        `/elections/${GUID}`,
      );
    });
  });
});
