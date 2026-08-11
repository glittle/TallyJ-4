/**
 * Paths for ballot list / entry navigation.
 * Use these so monitor, results, and other pages open a specific ballot consistently.
 */

/** Ballots management page; optional ballotGuid opens that ballot in the entry drawer. */
export function electionBallotsPath(
  electionGuid: string,
  ballotGuid?: string | null,
): string {
  if (ballotGuid) {
    return `/elections/${electionGuid}/ballot/${ballotGuid}`;
  }
  return `/elections/${electionGuid}/ballots`;
}

/** Full-page ballot entry (bookmarkable dedicated route). */
export function electionBallotEntryPath(
  electionGuid: string,
  ballotGuid: string,
): string {
  return `/elections/${electionGuid}/ballots/${ballotGuid}/entry`;
}

export function ballotGuidFromRouteParams(params: {
  ballotId?: string | string[];
  [key: string]: unknown;
}): string | null {
  const value = params.ballotId;
  if (typeof value === "string" && value.trim()) {
    return value.trim();
  }
  if (Array.isArray(value) && typeof value[0] === "string" && value[0].trim()) {
    return value[0].trim();
  }
  return null;
}
