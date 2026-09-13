import type { PersonListDto } from "@/types/Person";

/** Same bound as CheckSelectedWhatsAppDto.MaxSelectedPeople. */
export const MAX_WHATSAPP_CHECK_SELECTED = 100;

export function personHasPhone(person: Pick<PersonListDto, "phone">): boolean {
  return !!person.phone?.trim();
}

export function selectedPeopleWithPhone(
  people: PersonListDto[],
  selectedGuids: string[],
): PersonListDto[] {
  const selected = new Set(selectedGuids);
  return people.filter(
    (person) => selected.has(person.personGuid) && personHasPhone(person),
  );
}

export function canCheckSelectedWhatsApp(
  selectedCount: number,
  selectedWithPhoneCount: number,
): boolean {
  return (
    selectedWithPhoneCount > 0 &&
    selectedCount > 0 &&
    selectedCount <= MAX_WHATSAPP_CHECK_SELECTED
  );
}

export function whatsAppCheckOutcomeLabel(
  outcome: string,
  t: (key: string) => string,
): string {
  return t(`people.checkWhatsAppSelectedOutcome.${outcome}`);
}

/**
 * People list Cancel aborts the fetch. The client gets AbortError and never
 * reads result.cancelled. That AbortError is the UI cancel path.
 */
export function isWhatsAppCheckAbortError(error: unknown): boolean {
  return (
    (error instanceof DOMException && error.name === "AbortError") ||
    (typeof error === "object" &&
      error !== null &&
      "name" in error &&
      (error as { name?: string }).name === "AbortError")
  );
}

export type WhatsAppCheckUiFinish = "completed" | "aborted" | "failed";

/**
 * Success and AbortError both clear the People list selection.
 * Other errors leave it so the teller can retry.
 */
export function nextSelectedGuidsAfterWhatsAppCheck(
  current: string[],
  finish: WhatsAppCheckUiFinish,
): string[] {
  if (finish === "failed") {
    return current;
  }
  return [];
}
