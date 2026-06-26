import { DataAnalysis, Document } from "@element-plus/icons-vue";
import {
  secureTokenService,
  type AuthCookieData,
} from "@/services/secureTokenService";
import type { ElectionStage, NavPageDef } from "./electionStages";
import { STAGE_PAGES } from "./electionStages";

/**
 * Assistant tellers authenticate via election passcode (AccessCode), not as full
 * election officials. Full tellers and admins are unaffected by these rules.
 */
export function isAssistantTeller(authData?: AuthCookieData): boolean {
  const data = authData ?? secureTokenService.getAuthData();
  return data.name === "Teller" && data.authMethod === "AccessCode";
}

/** Route patterns allowed per stage (in addition to menu page paths). */
const ASSISTANT_TELLER_ROUTE_PATTERNS: Record<ElectionStage, RegExp[]> = {
  SettingUp: [/^\/elections\/[^/]+$/],
  GatheringBallots: [/^\/elections\/[^/]+\/frontdesk$/],
  ProcessingBallots: [
    /^\/elections\/[^/]+\/ballots\/[^/]+\/entry$/,
    // Fallback landing while awaiting a ballot entry URL from coordinators
    /^\/elections\/[^/]+$/,
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
 * Sidebar menu pages visible to assistant tellers for the current election stage.
 */
export function getAssistantTellerMenuPages(
  stage: ElectionStage,
  _electionGuid: string,
): NavPageDef[] {
  switch (stage) {
    case "SettingUp":
      return [];
    case "GatheringBallots":
      return STAGE_PAGES.GatheringBallots.filter((p) => !p.adminOnly);
    case "ProcessingBallots":
      // Ballot entry uses per-ballot URLs; no fixed menu destination.
      return [];
    case "Finalized":
      return [landingPage(), showFinalResultsPage()];
  }
}

export function isAssistantTellerRouteAllowed(
  path: string,
  electionGuid: string,
  stage: ElectionStage,
): boolean {
  const prefix = `/elections/${electionGuid}`;
  if (!path.startsWith(prefix)) {
    return false;
  }

  const menuPaths = getAssistantTellerMenuPages(stage, electionGuid).map((p) =>
    p.routePath(electionGuid),
  );
  if (menuPaths.includes(path)) {
    return true;
  }

  return ASSISTANT_TELLER_ROUTE_PATTERNS[stage].some((pattern) =>
    pattern.test(path),
  );
}

/** First safe destination when an assistant teller hits a restricted route. */
export function getAssistantTellerRedirectPath(
  electionGuid: string,
  stage: ElectionStage,
): string {
  const menuPages = getAssistantTellerMenuPages(stage, electionGuid);
  if (menuPages.length > 0) {
    return menuPages[0]!.routePath(electionGuid);
  }
  return `/elections/${electionGuid}`;
}
