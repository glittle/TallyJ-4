import type { PersonListDto, PersonPhoneSmsHintDto } from "@/types/Person";
import { phoneWhatsAppListHint } from "@/utils/phoneOnlineVoterStatus";

/** People list WhatsApp filter. Default is All; Has WhatsApp is the notify leftover. */
export const PEOPLE_LIST_WHATSAPP_FILTER_ALL = "all";
export const PEOPLE_LIST_WHATSAPP_FILTER_HAS = "hasWhatsApp";
export const PEOPLE_LIST_WHATSAPP_FILTER_UNCHECKED = "unchecked";
export const PEOPLE_LIST_WHATSAPP_FILTER_NO = "noWhatsApp";

export type PeopleListWhatsAppFilter =
  | typeof PEOPLE_LIST_WHATSAPP_FILTER_ALL
  | typeof PEOPLE_LIST_WHATSAPP_FILTER_HAS
  | typeof PEOPLE_LIST_WHATSAPP_FILTER_UNCHECKED
  | typeof PEOPLE_LIST_WHATSAPP_FILTER_NO;

export const PEOPLE_LIST_WHATSAPP_FILTERS: PeopleListWhatsAppFilter[] = [
  PEOPLE_LIST_WHATSAPP_FILTER_ALL,
  PEOPLE_LIST_WHATSAPP_FILTER_HAS,
  PEOPLE_LIST_WHATSAPP_FILTER_UNCHECKED,
  PEOPLE_LIST_WHATSAPP_FILTER_NO,
];

function phoneHint(
  person: Pick<PersonListDto, "phoneOnlineVoter">,
): PersonPhoneSmsHintDto | null | undefined {
  return person.phoneOnlineVoter;
}

function isPhonePRow(
  hint: PersonPhoneSmsHintDto | null | undefined,
): hint is PersonPhoneSmsHintDto {
  return hint?.hasPhoneRow === true;
}

/**
 * Has WhatsApp is a P-row with WhatsAppStatus == "OK" (same list hint as the column).
 * Non-P (hasPhoneRow false) and no-phone are never Has WhatsApp.
 */
export function personHasWhatsApp(
  person: Pick<PersonListDto, "phoneOnlineVoter">,
): boolean {
  const hint = phoneHint(person);
  return isPhonePRow(hint) && phoneWhatsAppListHint(hint) === "ok";
}

/**
 * Unchecked is a P-row that has not been checked yet (null WhatsAppStatus).
 * Covers the column's imported and unchecked hints. Non-P is not unchecked.
 */
export function personWhatsAppUnchecked(
  person: Pick<PersonListDto, "phoneOnlineVoter">,
): boolean {
  const hint = phoneHint(person);
  if (!isPhonePRow(hint)) {
    return false;
  }
  const listHint = phoneWhatsAppListHint(hint);
  return listHint === "unchecked" || listHint === "imported";
}

/** No WhatsApp is a P-row whose stored reason is no-wa. Other reasons stay on All. */
export function personHasNoWhatsApp(
  person: Pick<PersonListDto, "phoneOnlineVoter">,
): boolean {
  const hint = phoneHint(person);
  return (
    isPhonePRow(hint) &&
    phoneWhatsAppListHint(hint) === "blocked" &&
    hint.whatsAppStatus === "no-wa"
  );
}

export function personMatchesWhatsAppFilter(
  person: Pick<PersonListDto, "phoneOnlineVoter">,
  filter: PeopleListWhatsAppFilter,
): boolean {
  if (filter === PEOPLE_LIST_WHATSAPP_FILTER_ALL) {
    return true;
  }
  if (filter === PEOPLE_LIST_WHATSAPP_FILTER_HAS) {
    return personHasWhatsApp(person);
  }
  if (filter === PEOPLE_LIST_WHATSAPP_FILTER_UNCHECKED) {
    return personWhatsAppUnchecked(person);
  }
  if (filter === PEOPLE_LIST_WHATSAPP_FILTER_NO) {
    return personHasNoWhatsApp(person);
  }
  return true;
}

export function personMatchesPeopleSearch(
  person: Pick<PersonListDto, "fullName" | "email">,
  searchQuery: string,
): boolean {
  if (!searchQuery) {
    return true;
  }
  const query = searchQuery.toLowerCase();
  return (
    (person.fullName?.toLowerCase().includes(query) ?? false) ||
    (person.email?.toLowerCase().includes(query) ?? false)
  );
}

export function filterPeopleList(
  people: PersonListDto[],
  searchQuery: string,
  whatsAppFilter: PeopleListWhatsAppFilter,
): PersonListDto[] {
  return people.filter(
    (person) =>
      personMatchesPeopleSearch(person, searchQuery) &&
      personMatchesWhatsAppFilter(person, whatsAppFilter),
  );
}
