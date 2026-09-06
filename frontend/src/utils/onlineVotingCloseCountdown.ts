/** Last minutes before a firm close when the monitor treats the window as "soon". */
export const CLOSE_SOON_MINUTES = 5;

export type OnlineVotingCloseCountdown = {
  hasCloseTime: boolean;
  isWindowOpen: boolean;
  isClosed: boolean;
  /** Open now and remaining close time is at most {@link CLOSE_SOON_MINUTES}. */
  isClosingSoon: boolean;
  remainingMs: number;
};

function toMs(value: Date | string | null | undefined): number | null {
  if (value === null || value === undefined || value === "") {
    return null;
  }
  const ms =
    value instanceof Date ? value.getTime() : new Date(value).getTime();
  return Number.isNaN(ms) ? null : ms;
}

/**
 * Same open/close rules as the header Online Voting control: missing both
 * dates is closed; a close in the past is closed; otherwise open.
 */
export function isOnlineVotingWindowOpen(
  openValue: Date | string | null | undefined,
  closeValue: Date | string | null | undefined,
  now: Date = new Date(),
): boolean {
  const nowMs = now.getTime();
  const openMs = toMs(openValue);
  const closeMs = toMs(closeValue);
  if (openMs !== null && nowMs < openMs) {
    return false;
  }
  if (closeMs !== null && nowMs >= closeMs) {
    return false;
  }
  if (openMs === null && closeMs === null) {
    return false;
  }
  return true;
}

export function getOnlineVotingCloseCountdown(
  openValue: Date | string | null | undefined,
  closeValue: Date | string | null | undefined,
  now: Date = new Date(),
): OnlineVotingCloseCountdown {
  const closeMs = toMs(closeValue);
  const remainingMs = closeMs === null ? 0 : closeMs - now.getTime();
  const isWindowOpen = isOnlineVotingWindowOpen(openValue, closeValue, now);
  const isClosed = closeMs !== null && remainingMs <= 0;
  const isClosingSoon =
    isWindowOpen &&
    closeMs !== null &&
    remainingMs > 0 &&
    remainingMs <= CLOSE_SOON_MINUTES * 60 * 1000;

  return {
    hasCloseTime: closeMs !== null,
    isWindowOpen,
    isClosed,
    isClosingSoon,
    remainingMs: Math.max(0, remainingMs),
  };
}

/** `m:ss` remaining (e.g. `4:32`) for the last-five-minute clock. */
export function formatCloseRemainingClock(remainingMs: number): string {
  const totalSeconds = Math.max(0, Math.floor(remainingMs / 1000));
  const minutes = Math.floor(totalSeconds / 60);
  const seconds = totalSeconds % 60;
  return `${minutes}:${seconds.toString().padStart(2, "0")}`;
}

export function scheduleOnlineCloseAt(
  minutesFromNow: number,
  now: Date = new Date(),
): Date {
  return new Date(now.getTime() + minutesFromNow * 60 * 1000);
}

/** v3 `closeOnline()` with no minutes: one second ago so the window is closed. */
export function closeOnlineVotingNowAt(now: Date = new Date()): Date {
  return new Date(now.getTime() - 1000);
}
