/**
 * Where the ballot page finds its election after a hard reload.
 * The route param is authoritative when it is present. A kiosk session
 * also carries the election inside the voter id (`{letters}.{guid:N}`),
 * which survives reload via the httpOnly voter cookie. sessionStorage is
 * only a fallback when the address has no election id.
 */

const LAST_ELECTION_KEY = "tallyj.voterElectionGuid";

export function readRouteElectionId(value: unknown): string {
  const raw = Array.isArray(value) ? value[0] : value;
  if (typeof raw !== "string") {
    return "";
  }
  const trimmed = raw.trim();
  if (!trimmed || trimmed === "undefined" || trimmed === "null") {
    return "";
  }
  return trimmed;
}

/**
 * Election GUID embedded in a kiosk OnlineVoter id.
 * Same shape as server `KioskCodeLifetime.TryParseVoterId`: letters, a dot,
 * then 32 hex digits (`Guid` format "N").
 */
export function electionGuidFromKioskVoterId(
  voterId: string | null | undefined,
): string {
  if (!voterId) {
    return "";
  }
  const dot = voterId.lastIndexOf(".");
  if (dot <= 0 || voterId.length - dot !== 33) {
    return "";
  }
  const hex = voterId.slice(dot + 1);
  if (!/^[0-9a-f]{32}$/i.test(hex)) {
    return "";
  }
  const h = hex.toLowerCase();
  return `${h.slice(0, 8)}-${h.slice(8, 12)}-${h.slice(12, 16)}-${h.slice(16, 20)}-${h.slice(20)}`;
}

export function rememberVoterElection(electionGuid: string): void {
  if (!electionGuid) {
    return;
  }
  try {
    sessionStorage.setItem(LAST_ELECTION_KEY, electionGuid);
  } catch {
    // Private mode / quota — the route or kiosk id can still restore.
  }
}

export function readRememberedVoterElection(): string {
  try {
    return readRouteElectionId(sessionStorage.getItem(LAST_ELECTION_KEY));
  } catch {
    return "";
  }
}
