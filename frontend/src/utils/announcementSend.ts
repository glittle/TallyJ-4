import type { PersonListDto } from "@/types/Person";

/** Same bound as StartAnnouncementDto.MaxSelectedPeople. */
export const MAX_ANNOUNCEMENT_SELECTED = 100;

export const ANNOUNCEMENT_CHANNEL_EMAIL = "email";
export const ANNOUNCEMENT_CHANNEL_SMS = "sms";
export const ANNOUNCEMENT_CHANNEL_WHATSAPP = "whatsapp";

export const ANNOUNCEMENT_CHANNELS = [
  ANNOUNCEMENT_CHANNEL_EMAIL,
  ANNOUNCEMENT_CHANNEL_SMS,
  ANNOUNCEMENT_CHANNEL_WHATSAPP,
] as const;

export type AnnouncementChannel = (typeof ANNOUNCEMENT_CHANNELS)[number];

export function personHasEmail(person: Pick<PersonListDto, "email">): boolean {
  return !!person.email?.trim();
}

export function selectedPeopleWithEmail(
  people: PersonListDto[],
  selectedGuids: string[],
): PersonListDto[] {
  const selected = new Set(selectedGuids);
  return people.filter(
    (person) => selected.has(person.personGuid) && personHasEmail(person),
  );
}

export function canSendAnnouncement(
  selectedCount: number,
  withDestinationCount: number,
): boolean {
  return (
    withDestinationCount > 0 &&
    selectedCount > 0 &&
    selectedCount <= MAX_ANNOUNCEMENT_SELECTED
  );
}

function translated(key: string, t: (key: string) => string): string | null {
  const value = t(key);
  return value === key ? null : value;
}

/**
 * Outcome label plus the provider or block error.
 * Known WhatsApp outcomes and block reasons use locale keys.
 * Anything else is the sanitized error text stored for that person.
 */
export function formatSendOutcome(
  outcome: string,
  error: string | null | undefined,
  t: (key: string) => string,
): string {
  const label =
    translated(`people.notifyWhatsAppOutcome.${outcome}`, t) ??
    translated(`people.sendOutcome.${outcome}`, t) ??
    outcome;
  const detail = error?.trim();
  if (!detail) {
    return label;
  }
  const errorLabel =
    translated(`people.announcementError.${detail}`, t) ?? detail;
  return `${label} — ${errorLabel}`;
}

/** Retry of a channel with no failed rows returns an idle empty status. */
export function isEmptyAnnouncementRun(status: {
  queueToken?: string | null;
  running: boolean;
  results?: readonly unknown[] | null;
}): boolean {
  return (
    !status.running && !status.queueToken && (status.results?.length ?? 0) === 0
  );
}
