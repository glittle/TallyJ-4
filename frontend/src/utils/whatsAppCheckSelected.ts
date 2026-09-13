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
