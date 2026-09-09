/** Matches backend OnlineVotingWindow.IsCurrentlyOpen / voter submit. */
export function isOnlineVotingCurrentlyOpen(
  election: {
    useOnlineVoting?: boolean | null;
    onlineWhenOpen?: Date | string | null;
    onlineWhenClose?: Date | string | null;
  } | null | undefined,
  nowMs: number = Date.now(),
): boolean {
  if (!election?.useOnlineVoting) {
    return false;
  }

  const openMs = toTimeMs(election.onlineWhenOpen);
  const closeMs = toTimeMs(election.onlineWhenClose);
  if (openMs !== null && nowMs < openMs) {
    return false;
  }
  if (closeMs !== null && nowMs >= closeMs) {
    return false;
  }
  return true;
}

function toTimeMs(value: Date | string | null | undefined): number | null {
  if (value === null || value === undefined || value === "") {
    return null;
  }
  const ms = value instanceof Date ? value.getTime() : new Date(value).getTime();
  return Number.isNaN(ms) ? null : ms;
}
