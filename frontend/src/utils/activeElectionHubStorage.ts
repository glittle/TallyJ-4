const ACTIVE_ELECTION_HUB_KEY = "tallyj_active_election_hub";

/** Election GUID for which the main teller keeps a MainHub connection across in-app navigation. */
export function getActiveElectionHubGuid(): string | null {
  try {
    return sessionStorage.getItem(ACTIVE_ELECTION_HUB_KEY);
  } catch {
    return null;
  }
}

export function setActiveElectionHubGuid(guid: string | null): void {
  try {
    if (guid) {
      sessionStorage.setItem(ACTIVE_ELECTION_HUB_KEY, guid);
    } else {
      sessionStorage.removeItem(ACTIVE_ELECTION_HUB_KEY);
    }
  } catch {
    // Ignore storage failures; hub maintenance is best-effort.
  }
}
