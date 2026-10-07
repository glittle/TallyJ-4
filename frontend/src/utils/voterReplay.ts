/**
 * Online-voter routes where Session Replay must not record.
 * Kiosk voting uses the same pages (`?kiosk=1`), not a separate route.
 * Vote status is shown on the elections and confirmation pages.
 */
const VOTER_ROUTE_NAMES = new Set([
  "voter-auth",
  "voter-elections",
  "voter-ballot",
  "voter-confirmation",
]);

export type VoterRouteLike = {
  name?: string | symbol | null;
  path?: string | null;
};

export type ReplayRouteAction =
  | "ignore"
  | "stop"
  | "install"
  | "keep"
  | "session"
  | "buffer";

export type ReplayControls = {
  stop: (options?: { flush?: boolean }) => Promise<void> | void;
  start: () => void;
  startBuffering: () => void;
};

function normalizePath(path: string): string {
  const withoutQuery = path.split("?")[0]?.split("#")[0] ?? "";
  if (withoutQuery.length > 1 && withoutQuery.endsWith("/")) {
    return withoutQuery.slice(0, -1);
  }
  return withoutQuery;
}

function isVoterPath(path: string): boolean {
  const normalized = normalizePath(path);
  if (
    normalized === "/voter-auth" ||
    normalized.startsWith("/voter-auth/") ||
    normalized === "/voter-elections" ||
    normalized.startsWith("/voter-elections/") ||
    normalized === "/vote-confirmation" ||
    normalized.startsWith("/vote-confirmation/")
  ) {
    return true;
  }
  // Ballot page only. `/vote-confirmation` must not match this prefix.
  return normalized === "/vote" || normalized.startsWith("/vote/");
}

export function isVoterRoute(route: VoterRouteLike): boolean {
  if (typeof route.name === "string" && VOTER_ROUTE_NAMES.has(route.name)) {
    return true;
  }
  return isVoterPath(route.path ?? "");
}

/** Same rates main used before replay became route-gated. */
export function replaySampleRates(env: string | undefined): {
  sessionSampleRate: number;
  errorSampleRate: number;
} {
  return {
    sessionSampleRate: env === "development" ? 1 : 0.1,
    errorSampleRate: 1,
  };
}

/**
 * `install` lets the SDK apply session vs on-error sampling itself.
 * `session` / `buffer` are only for a replay that was stopped on a voter route.
 * `stop` discards the pending segment so a teller buffer cannot continue onto the ballot.
 */
export function replayActionForRoute(input: {
  route: VoterRouteLike;
  replayInstalled: boolean;
  replayActive: boolean;
  random: number;
  sessionSampleRate: number;
  errorSampleRate: number;
}): ReplayRouteAction {
  if (isVoterRoute(input.route)) {
    return input.replayActive ? "stop" : "ignore";
  }
  if (!input.replayInstalled) {
    return "install";
  }
  if (input.replayActive) {
    return "keep";
  }
  if (input.random < input.sessionSampleRate) {
    return "session";
  }
  if (input.errorSampleRate > 0) {
    return "buffer";
  }
  return "keep";
}

export async function applyReplayRouteAction(
  action: ReplayRouteAction,
  replay: ReplayControls | undefined,
  install: () => void,
): Promise<void> {
  switch (action) {
    case "stop":
      await replay?.stop({ flush: false });
      return;
    case "session":
      replay?.start();
      return;
    case "buffer":
      replay?.startBuffering();
      return;
    case "install":
      install();
      return;
    default:
      return;
  }
}
