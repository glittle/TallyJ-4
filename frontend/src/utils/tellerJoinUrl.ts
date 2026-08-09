/**
 * Guest-teller shareable join links.
 *
 * Prefer a compact path + query form so QR codes stay simple:
 *   /teller-join/{guidWithoutDashes}?c={passcode}
 *
 * The passcode stays in the query string (not the path) so values with
 * reserved characters still round-trip through encodeURIComponent.
 */

const GUID_DASHED =
  /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
const GUID_COMPACT = /^[0-9a-f]{32}$/i;

/** Strip dashes from a GUID for shorter share links. */
export function compactElectionGuid(electionGuid: string): string {
  return electionGuid.replace(/-/g, "").toLowerCase();
}

/**
 * Expand a dashed or compact GUID to the standard 8-4-4-4-12 form.
 * Returns null when the value is not a GUID.
 */
export function expandElectionGuid(
  value: string | undefined | null,
): string | null {
  if (!value) {
    return null;
  }

  const raw = value.trim();
  if (GUID_DASHED.test(raw)) {
    return raw.toLowerCase();
  }

  if (GUID_COMPACT.test(raw)) {
    const hex = raw.toLowerCase();
    return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
  }

  return null;
}

export function isElectionGuid(value: string | undefined | null): boolean {
  return expandElectionGuid(value) !== null;
}

/**
 * Build the short shareable join URL for a guest teller.
 */
export function buildTellerJoinUrl(
  origin: string,
  electionGuid: string,
  passcode: string,
): string {
  const base = origin.replace(/\/$/, "");
  const compact = compactElectionGuid(electionGuid);
  const code = encodeURIComponent(passcode);
  return `${base}/teller-join/${compact}?c=${code}`;
}

/**
 * Read join parameters from a vue-router location-like object.
 * Supports:
 * - current: /teller-join/{guid}?c=... (or ?code=...)
 * - legacy path: /teller-join/{accessCode}/{guid}
 */
export function parseTellerJoinRoute(route: {
  params: Record<string, string | string[] | undefined>;
  query: Record<string, string | string[] | undefined | null>;
}): { electionGuid: string | null; accessCode: string | null } {
  // Route is defined as :accessCode?/:electionGuid? — a single path segment
  // lands in accessCode. Detect whether that segment is a GUID.
  const first = firstParam(route.params.accessCode);
  const second = firstParam(route.params.electionGuid);

  let electionGuid: string | null = null;
  let accessCode: string | null = null;

  if (second && isElectionGuid(second)) {
    // /teller-join/{accessCode}/{guid}
    electionGuid = expandElectionGuid(second);
    accessCode = first;
  } else if (first && isElectionGuid(first)) {
    // /teller-join/{guid}?c=...
    electionGuid = expandElectionGuid(first);
  } else if (first && !second) {
    // /teller-join/{accessCode} only
    accessCode = first;
  }

  const queryCode =
    firstQuery(route.query.c) ?? firstQuery(route.query.code) ?? null;

  if (queryCode) {
    accessCode = queryCode;
  }

  return { electionGuid, accessCode };
}

function firstParam(value: string | string[] | undefined): string | null {
  if (Array.isArray(value)) {
    return value[0] ?? null;
  }
  return value ?? null;
}

function firstQuery(
  value: string | string[] | undefined | null,
): string | null {
  if (value === null || value === undefined) {
    return null;
  }
  if (Array.isArray(value)) {
    return value[0] ?? null;
  }
  return value;
}
