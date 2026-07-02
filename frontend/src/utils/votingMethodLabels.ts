import type { ComposerTranslation } from "vue-i18n";

const VOTING_METHOD_KEYS: Record<string, string> = {
  I: "frontDesk.votingMethod.inPerson",
  P: "frontDesk.votingMethod.inPerson",
  M: "frontDesk.votingMethod.mail",
  O: "frontDesk.votingMethod.online",
  C: "frontDesk.votingMethod.callIn",
  K: "people.votingMethod.kiosk",
  D: "people.votingMethod.droppedOff",
  "1": "people.votingMethod.custom1",
  "2": "people.votingMethod.custom2",
  "3": "people.votingMethod.custom3",
};

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

export function electionSupportsKiosk(votingMethods?: string | null): boolean {
  return votingMethods?.toUpperCase().includes("K") ?? false;
}
