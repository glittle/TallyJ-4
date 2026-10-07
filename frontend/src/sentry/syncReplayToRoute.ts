import * as Sentry from "@sentry/vue";

import {
  applyReplayRouteAction,
  isVoterRoute,
  replayActionForRoute,
  replaySampleRates,
  type ReplayControls,
  type VoterRouteLike,
} from "@/utils/voterReplay";

let replayInstalled = false;

function currentReplay():
  | (ReplayControls & { getReplayId: () => string | undefined })
  | undefined {
  return Sentry.getReplay() ?? undefined;
}

/**
 * Replay stays out of Sentry.init so a voter URL never auto-starts a session
 * or an error buffer. The first teller/admin navigation installs it and lets
 * the SDK sample. Leaving for a voter route stops recording and drops the
 * pending segment (`flush: false`) before that page renders.
 */
function installReplay(env: string | undefined): void {
  if (replayInstalled || currentReplay()) {
    replayInstalled = true;
    return;
  }

  const client = Sentry.getClient();
  if (!client) {
    return;
  }

  const rates = replaySampleRates(env);
  // getOptions() is the live client options object. replayIntegration reads
  // replaysSessionSampleRate and replaysOnErrorSampleRate from it when added,
  // so these assignments must happen before addIntegration.
  const options = client.getOptions() as {
    replaysSessionSampleRate?: number;
    replaysOnErrorSampleRate?: number;
  };
  options.replaysSessionSampleRate = rates.sessionSampleRate;
  options.replaysOnErrorSampleRate = rates.errorSampleRate;

  client.addIntegration(
    Sentry.replayIntegration({
      maskAllText: true,
      maskAllInputs: true,
      blockAllMedia: true,
      beforeErrorSampling: () =>
        !isVoterRoute({ path: window.location.pathname }),
    }),
  );
  replayInstalled = true;
}

export async function syncReplayToRoute(
  route: VoterRouteLike,
  env: string | undefined,
): Promise<void> {
  const replay = currentReplay();
  const rates = replaySampleRates(env);
  const action = replayActionForRoute({
    route,
    replayInstalled: replayInstalled || replay !== undefined,
    replayActive: replay?.getReplayId() !== undefined,
    random: Math.random(),
    sessionSampleRate: rates.sessionSampleRate,
    errorSampleRate: rates.errorSampleRate,
  });

  try {
    await applyReplayRouteAction(action, replay, () => installReplay(env));
  } catch (error) {
    console.error("Session Replay route sync failed:", error);
  }
}
