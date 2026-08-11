import { DataAnalysis, Document } from "@element-plus/icons-vue";
import {
  secureTokenService,
  type AuthCookieData,
} from "@/services/secureTokenService";
import type { ElectionStage, NavPageDef } from "./electionStages";
import { STAGE_PAGES } from "./electionStages";

/**
 * GuestTellers authenticate via election passcode (AccessCode), not as FullTellers
 * or election officials. FullTellers and admins are unaffected by these rules.
 */
export function isGuestTeller(authData?: AuthCookieData): boolean {
  const data = authData ?? secureTokenService.getAuthData();
  return data.name === "Teller" && data.authMethod === "AccessCode";
}

/** Authenticated users with full logged-in access (not GuestTellers). */
export function isFullTeller(authData?: AuthCookieData): boolean {
  if (authData === undefined && !secureTokenService.isAuthenticated()) {
    return false;
  }
  return !isGuestTeller(authData);
}

/** Route patterns allowed per stage (in addition to menu page paths). */
const GUEST_TELLER_ROUTE_PATTERNS: Record<ElectionStage, RegExp[]> = {
  SettingUp: [/^\/elections\/[^/]+$/],
  GatheringBallots: [/^\/elections\/[^/]+\/frontdesk$/],
  ProcessingBallots: [
    // Enter Ballots list, open-ballot deep link, and full-page entry
    /^\/elections\/[^/]+\/ballots$/,
    /^\/elections\/[^/]+\/ballot\/[^/]+$/,
    /^\/elections\/[^/]+\/ballots\/[^/]+\/entry$/,
  ],
  Finalized: [
    /^\/elections\/[^/]+$/,
    /^\/elections\/[^/]+\/results$/,
    /^\/elections\/[^/]+\/presentation$/,
  ],
};

function landingPage(): NavPageDef {
  return {
    key: "landing",
    i18nKey: "elections.details",
    icon: Document,
    routePath: (g) => `/elections/${g}`,
  };
}

function showFinalResultsPage(): NavPageDef {
  return {
    key: "final-results",
    i18nKey: "results.showFinalResults",
    icon: DataAnalysis,
    routePath: (g) => `/elections/${g}/results`,
  };
}

/**
 * Sidebar menu pages visible to GuestTellers for the current election stage.
 */
export function getGuestTellerMenuPages(
  stage: ElectionStage,
  _electionGuid: string,
): NavPageDef[] {
  switch (stage) {
    case "SettingUp":
      return [];
    case "GatheringBallots":
      return STAGE_PAGES.GatheringBallots.filter((p) => !p.adminOnly);
    case "ProcessingBallots":
      // Enter Ballots is Guest-visible; tally/monitor/results remain admin-only.
      return STAGE_PAGES.ProcessingBallots.filter((p) => !p.adminOnly);
    case "Finalized":
      return [landingPage(), showFinalResultsPage()];
  }
}

export function isGuestTellerRouteAllowed(
  path: string,
  electionGuid: string,
  stage: ElectionStage,
): boolean {
  const electionPathMatch = path.match(/^\/elections\/([^/]+)(.*)$/i);
  if (
    !electionPathMatch ||
    electionPathMatch[1]!.toLowerCase() !== electionGuid.toLowerCase()
  ) {
    return false;
  }

  // Canonicalize GUID casing so menu paths and route patterns match the URL.
  const canonicalPath = `/elections/${electionGuid}${electionPathMatch[2] ?? ""}`;

  const menuPaths = getGuestTellerMenuPages(stage, electionGuid).map((p) =>
    p.routePath(electionGuid),
  );
  if (menuPaths.includes(canonicalPath)) {
    return true;
  }

  return GUEST_TELLER_ROUTE_PATTERNS[stage].some((pattern) =>
    pattern.test(canonicalPath),
  );
}

/** First safe destination when a GuestTeller hits a restricted route. */
export function getGuestTellerRedirectPath(
  electionGuid: string,
  stage: ElectionStage,
): string {
  const menuPages = getGuestTellerMenuPages(stage, electionGuid);
  if (menuPages.length > 0) {
    return menuPages[0]!.routePath(electionGuid);
  }
  return `/elections/${electionGuid}`;
}
