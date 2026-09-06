import { describe, expect, it } from "vitest";
import {
  CLOSE_SOON_MINUTES,
  closeOnlineVotingNowAt,
  formatCloseRemainingClock,
  getOnlineVotingCloseCountdown,
  isOnlineVotingWindowOpen,
  scheduleOnlineCloseAt,
} from "../onlineVotingCloseCountdown";

describe("onlineVotingCloseCountdown", () => {
  const now = new Date("2026-03-10T12:00:00.000Z");
  const open = new Date("2026-03-10T10:00:00.000Z");

  it("treats missing both dates as closed", () => {
    expect(isOnlineVotingWindowOpen(null, null, now)).toBe(false);
    const view = getOnlineVotingCloseCountdown(null, null, now);
    expect(view.hasCloseTime).toBe(false);
    expect(view.isWindowOpen).toBe(false);
    expect(view.isClosingSoon).toBe(false);
  });

  it("is open when close is in the future", () => {
    const close = new Date("2026-03-10T14:00:00.000Z");
    const view = getOnlineVotingCloseCountdown(open, close, now);
    expect(view.isWindowOpen).toBe(true);
    expect(view.isClosed).toBe(false);
    expect(view.isClosingSoon).toBe(false);
    expect(view.remainingMs).toBe(2 * 60 * 60 * 1000);
  });

  it("is closing soon at exactly 5 minutes remaining", () => {
    const close = new Date(now.getTime() + CLOSE_SOON_MINUTES * 60 * 1000);
    const view = getOnlineVotingCloseCountdown(open, close, now);
    expect(view.isWindowOpen).toBe(true);
    expect(view.isClosingSoon).toBe(true);
    expect(formatCloseRemainingClock(view.remainingMs)).toBe("5:00");
  });

  it("is closing soon under 5 minutes", () => {
    const close = new Date(now.getTime() + 4 * 60 * 1000 + 32 * 1000);
    const view = getOnlineVotingCloseCountdown(open, close, now);
    expect(view.isClosingSoon).toBe(true);
    expect(formatCloseRemainingClock(view.remainingMs)).toBe("4:32");
  });

  it("is not closing soon just after 5 minutes", () => {
    const close = new Date(
      now.getTime() + CLOSE_SOON_MINUTES * 60 * 1000 + 1000,
    );
    const view = getOnlineVotingCloseCountdown(open, close, now);
    expect(view.isClosingSoon).toBe(false);
  });

  it("is closed when close is in the past", () => {
    const close = new Date("2026-03-10T11:55:00.000Z");
    const view = getOnlineVotingCloseCountdown(open, close, now);
    expect(view.isWindowOpen).toBe(false);
    expect(view.isClosed).toBe(true);
    expect(view.isClosingSoon).toBe(false);
    expect(view.remainingMs).toBe(0);
  });

  it("schedules a close N minutes from now", () => {
    expect(scheduleOnlineCloseAt(5, now).toISOString()).toBe(
      "2026-03-10T12:05:00.000Z",
    );
  });

  it("closes now as one second ago", () => {
    expect(closeOnlineVotingNowAt(now).toISOString()).toBe(
      "2026-03-10T11:59:59.000Z",
    );
  });
});
