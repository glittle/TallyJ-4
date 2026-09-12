import type { ComposerTranslation } from "vue-i18n";

export const VOTING_METHOD_IN_PERSON = "P";
export const VOTING_METHOD_MAILED = "M";
export const VOTING_METHOD_DROPPED_OFF = "D";
export const VOTING_METHOD_CALLED_IN = "C";
export const VOTING_METHOD_ONLINE = "O";
export const VOTING_METHOD_KIOSK = "K";
export const VOTING_METHOD_IMPORTED = "I";

const VOTING_METHOD_KEYS: Record<string, string> = {
  P: "frontDesk.votingMethod.inPerson",
  M: "frontDesk.votingMethod.mail",
  D: "people.votingMethod.droppedOff",
  C: "frontDesk.votingMethod.callIn",
  O: "frontDesk.votingMethod.online",
  K: "people.votingMethod.kiosk",
  I: "frontDesk.votingMethod.imported",
  "1": "people.votingMethod.custom1",
  "2": "people.votingMethod.custom2",
  "3": "people.votingMethod.custom3",
};

const ELECTION_METHOD_ALIASES: Record<string, string> = {
  IP: VOTING_METHOD_IN_PERSON,
  IN: VOTING_METHOD_IN_PERSON,
  OL: VOTING_METHOD_ONLINE,
  ON: VOTING_METHOD_ONLINE,
  IM: VOTING_METHOD_IMPORTED,
  KI: VOTING_METHOD_KIOSK,
  MA: VOTING_METHOD_MAILED,
  DO: VOTING_METHOD_DROPPED_OFF,
  DR: VOTING_METHOD_DROPPED_OFF,
  CA: VOTING_METHOD_CALLED_IN,
};

const KNOWN_CODES = new Set([
  VOTING_METHOD_IN_PERSON,
  VOTING_METHOD_MAILED,
  VOTING_METHOD_DROPPED_OFF,
  VOTING_METHOD_CALLED_IN,
  VOTING_METHOD_ONLINE,
  VOTING_METHOD_KIOSK,
  VOTING_METHOD_IMPORTED,
  "1",
  "2",
  "3",
]);

const DEFAULT_FRONT_DESK_METHODS = [
  VOTING_METHOD_IN_PERSON,
  VOTING_METHOD_MAILED,
  VOTING_METHOD_DROPPED_OFF,
];

/** Front Desk methods that supersede a pending online ballot. */
const RECORDED_OTHER_THAN_ONLINE = new Set([
  VOTING_METHOD_IN_PERSON,
  VOTING_METHOD_MAILED,
  VOTING_METHOD_DROPPED_OFF,
  VOTING_METHOD_CALLED_IN,
  VOTING_METHOD_IMPORTED,
  VOTING_METHOD_KIOSK,
  "1",
  "2",
  "3",
]);

export function getVotingMethodLabel(
  method: string | undefined | null,
  t: ComposerTranslation,
): string {
  if (!method) {
    return t("frontDesk.common.dash");
  }

  const key = VOTING_METHOD_KEYS[method];
  return key ? t(key) : method;
}

export function getVotingMethodTagType(
  method: string,
): "success" | "info" | "primary" | "warning" {
  switch (method) {
    case VOTING_METHOD_IN_PERSON:
      return "success";
    case VOTING_METHOD_MAILED:
    case VOTING_METHOD_DROPPED_OFF:
      return "info";
    case VOTING_METHOD_ONLINE:
    case VOTING_METHOD_KIOSK:
      return "primary";
    case VOTING_METHOD_CALLED_IN:
      return "warning";
    default:
      return "info";
  }
}

export function electionSupportsKiosk(votingMethods?: string | null): boolean {
  return parseElectionVotingMethods(votingMethods).includes(
    VOTING_METHOD_KIOSK,
  );
}

/** Adds or removes kiosk (`K`) without rewriting the other stored method tokens. */
export function setElectionKioskEnabled(
  votingMethods: string | null | undefined,
  enabled: boolean,
): string {
  const current = votingMethods ?? "";
  const has = electionSupportsKiosk(current);
  if (enabled === has) {
    return current;
  }

  if (enabled) {
    if (!current.trim()) {
      return VOTING_METHOD_KIOSK;
    }
    if (current.includes(",")) {
      return `${current.replace(/,\s*$/, "")},K`;
    }
    return `${current}${VOTING_METHOD_KIOSK}`;
  }

  if (current.includes(",")) {
    return current
      .split(",")
      .map((token) => token.trim())
      .filter((token) => {
        const upper = token.toUpperCase();
        return upper !== VOTING_METHOD_KIOSK && upper !== "KI";
      })
      .join(",");
  }

  const trimmed = current.trim();
  if (
    trimmed.length === 2 &&
    ELECTION_METHOD_ALIASES[trimmed.toUpperCase()] === VOTING_METHOD_KIOSK
  ) {
    return "";
  }

  return current.replace(/K/gi, "");
}

export function isRecordedOtherThanOnline(method?: string | null): boolean {
  return Boolean(method && RECORDED_OTHER_THAN_ONLINE.has(method));
}

/**
 * Parse Election.VotingMethods into Person.VotingMethod letters.
 * Accepts concatenated letters (`PMD`) or comma-separated aliases (`IP,OL`).
 */
export function parseElectionVotingMethods(
  votingMethods?: string | null,
): string[] {
  if (!votingMethods?.trim()) {
    return [...DEFAULT_FRONT_DESK_METHODS];
  }

  const seen = new Set<string>();
  const result: string[] = [];

  const add = (code: string) => {
    if (!seen.has(code)) {
      seen.add(code);
      result.push(code);
    }
  };

  const addToken = (token: string) => {
    const alias = ELECTION_METHOD_ALIASES[token.toUpperCase()];
    if (alias) {
      add(alias);
      return;
    }
    if (token.length !== 1) {
      return;
    }
    const code = /\d/.test(token) ? token : token.toUpperCase();
    if (KNOWN_CODES.has(code)) {
      add(code);
    }
  };

  const trimmed = votingMethods.trim();
  if (trimmed.includes(",")) {
    trimmed
      .split(",")
      .map((part) => part.trim())
      .filter(Boolean)
      .forEach(addToken);
  } else if (
    trimmed.length === 2 &&
    ELECTION_METHOD_ALIASES[trimmed.toUpperCase()]
  ) {
    add(ELECTION_METHOD_ALIASES[trimmed.toUpperCase()]!);
  } else {
    for (const ch of trimmed) {
      addToken(ch);
    }
  }

  return result.length > 0 ? result : [...DEFAULT_FRONT_DESK_METHODS];
}

export function isPendingOnlineBallotStatus(status?: string | null): boolean {
  return status === "Submitted" || status === "Processing";
}

export function isAcceptedOnlineBallotStatus(status?: string | null): boolean {
  return status === "Processed";
}

/** Draft or Submitted — Front Desk check-in with another method withdraws these. */
export function isWithdrawableOnlineBallotStatus(
  status?: string | null,
): boolean {
  return status === "Draft" || status === "Submitted";
}

/**
 * Methods a teller may record at Front Desk. Online (`O`) is voter-initiated
 * only — tellers do not check anyone in as Online.
 */
export function parseFrontDeskCheckInMethods(
  votingMethods?: string | null,
): string[] {
  return parseElectionVotingMethods(votingMethods).filter(
    (code) => code !== VOTING_METHOD_ONLINE,
  );
}
