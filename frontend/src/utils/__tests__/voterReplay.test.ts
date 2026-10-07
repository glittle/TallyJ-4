import { describe, expect, it, vi } from "vitest";

import {
  applyReplayRouteAction,
  isVoterRoute,
  replayActionForRoute,
  replaySampleRates,
} from "../voterReplay";

function controls() {
  return {
    stop: vi.fn(async () => undefined),
    start: vi.fn(),
    startBuffering: vi.fn(),
  };
}

describe("isVoterRoute", () => {
  it.each([
    ["voter sign-in", { name: "voter-auth", path: "/voter-auth" }],
    ["election picker", { name: "voter-elections", path: "/voter-elections" }],
    ["ballot", { name: "voter-ballot", path: "/vote/election-guid" }],
    [
      "vote confirmation",
      { name: "voter-confirmation", path: "/vote-confirmation" },
    ],
    ["kiosk ballot path", { path: "/vote/election-guid" }],
    ["trailing slash", { path: "/voter-auth/" }],
    ["confirmation with extra segment", { path: "/vote-confirmation/done" }],
  ])("matches %s", (_label, route) => {
    expect(isVoterRoute(route)).toBe(true);
  });

  it("matches a voter route name even when the path is not the public URL", () => {
    expect(isVoterRoute({ name: "voter-ballot", path: "/internal" })).toBe(
      true,
    );
  });

  it.each([
    ["landing", { name: "landing", path: "/" }],
    ["teller login", { path: "/login" }],
    ["register", { path: "/register" }],
    ["dashboard", { path: "/dashboard" }],
    ["teller join", { name: "teller-join", path: "/teller-join/code" }],
    ["front desk", { path: "/elections/elec/frontdesk" }],
    ["teller ballot list", { path: "/elections/elec/ballots" }],
    ["teller ballot drawer", { path: "/elections/elec/ballot/ballot-guid" }],
    [
      "teller ballot entry",
      { path: "/elections/elec/ballots/ballot-guid/entry" },
    ],
    ["monitor", { path: "/elections/elec/monitor" }],
    ["super admin", { name: "super-admin", path: "/super-admin" }],
    ["lookalike confirmation", { path: "/vote-confirmation-extra" }],
    ["lookalike votes", { path: "/votes" }],
    ["lookalike voter prefix", { path: "/voter-auth-extra" }],
  ])("does not match %s", (_label, route) => {
    expect(isVoterRoute(route)).toBe(false);
  });
});

describe("replayActionForRoute", () => {
  const teller = { path: "/dashboard" };
  const voter = { name: "voter-ballot", path: "/vote/election-guid" };

  it("does not install or record on a voter route when replay is off", () => {
    expect(
      replayActionForRoute({
        route: voter,
        replayInstalled: false,
        replayActive: false,
        random: 0,
        sessionSampleRate: 1,
        errorSampleRate: 1,
      }),
    ).toBe("ignore");
  });

  it("stops an active replay on a voter route", () => {
    expect(
      replayActionForRoute({
        route: voter,
        replayInstalled: true,
        replayActive: true,
        random: 0,
        sessionSampleRate: 0.1,
        errorSampleRate: 1,
      }),
    ).toBe("stop");
  });

  it("does not restart replay while still on a voter route", () => {
    expect(
      replayActionForRoute({
        route: voter,
        replayInstalled: true,
        replayActive: false,
        random: 0,
        sessionSampleRate: 1,
        errorSampleRate: 1,
      }),
    ).toBe("ignore");
  });

  it("installs replay on the first teller route so the SDK can sample", () => {
    expect(
      replayActionForRoute({
        route: teller,
        replayInstalled: false,
        replayActive: false,
        random: 0.5,
        sessionSampleRate: 0.1,
        errorSampleRate: 1,
      }),
    ).toBe("install");
  });

  it("keeps a replay that is already recording on a teller route", () => {
    expect(
      replayActionForRoute({
        route: teller,
        replayInstalled: true,
        replayActive: true,
        random: 0.99,
        sessionSampleRate: 0.1,
        errorSampleRate: 1,
      }),
    ).toBe("keep");
  });

  it("starts a full session when the session sample hits after a voter route", () => {
    expect(
      replayActionForRoute({
        route: teller,
        replayInstalled: true,
        replayActive: false,
        random: 0,
        sessionSampleRate: 0.1,
        errorSampleRate: 1,
      }),
    ).toBe("session");
  });

  it("buffers for on-error replay when the session sample misses", () => {
    expect(
      replayActionForRoute({
        route: teller,
        replayInstalled: true,
        replayActive: false,
        random: 0.1,
        sessionSampleRate: 0.1,
        errorSampleRate: 1,
      }),
    ).toBe("buffer");
  });

  it("uses a full session sample in development", () => {
    expect(replaySampleRates("development")).toEqual({
      sessionSampleRate: 1,
      errorSampleRate: 1,
    });
    expect(replaySampleRates("production").sessionSampleRate).toBe(0.1);
  });
});

describe("applyReplayRouteAction", () => {
  it("discards the pending segment when stopping for a voter route", async () => {
    const replay = controls();
    const install = vi.fn();

    await applyReplayRouteAction("stop", replay, install);

    expect(replay.stop).toHaveBeenCalledTimes(1);
    expect(replay.stop).toHaveBeenCalledWith({ flush: false });
    expect(replay.start).not.toHaveBeenCalled();
    expect(replay.startBuffering).not.toHaveBeenCalled();
    expect(install).not.toHaveBeenCalled();
  });

  it("does not start replay for ignore or keep", async () => {
    const replay = controls();
    const install = vi.fn();

    await applyReplayRouteAction("ignore", replay, install);
    await applyReplayRouteAction("keep", undefined, install);

    expect(replay.stop).not.toHaveBeenCalled();
    expect(replay.start).not.toHaveBeenCalled();
    expect(install).not.toHaveBeenCalled();
  });

  it("starts session or buffer mode without flushing a stopped replay", async () => {
    const replay = controls();
    const install = vi.fn();

    await applyReplayRouteAction("session", replay, install);
    await applyReplayRouteAction("buffer", replay, install);
    await applyReplayRouteAction("install", undefined, install);

    expect(replay.start).toHaveBeenCalledTimes(1);
    expect(replay.startBuffering).toHaveBeenCalledTimes(1);
    expect(replay.stop).not.toHaveBeenCalled();
    expect(install).toHaveBeenCalledTimes(1);
  });
});
