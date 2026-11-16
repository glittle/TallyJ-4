import type { RegistrationFilter } from "@/composables/useFrontDeskVoters";
import { formatNumber } from "@/utils/formatNumber";

export type FrontDeskEmptyInput = {
  registrationFilter: RegistrationFilter;
  registeredCount: number;
  notRegisteredCount: number;
  searchQuery: string;
  /** Method, flag, and Ballot Not Received labels already in display text. */
  extraFilterLabels: string[];
};

type Translate = (
  key: string,
  params?: Record<string, string | number>,
) => string;

/**
 * Copy for an empty Front Desk list. Names the filter that matched nobody.
 */
export function frontDeskEmptyMessage(
  input: FrontDeskEmptyInput,
  translate: Translate,
): string {
  const query = input.searchQuery.trim();
  const names: string[] = [];
  if (input.registrationFilter === "notRegistered") {
    names.push(translate("frontDesk.filters.nameNotRegistered"));
  } else if (input.registrationFilter === "registered") {
    names.push(translate("frontDesk.filters.nameRegistered"));
  }
  for (const label of input.extraFilterLabels) {
    if (label.trim()) {
      names.push(label.trim());
    }
  }

  if (query && names.length > 0) {
    return translate("frontDesk.empty.searchUnder", {
      query,
      filter: names.join(", "),
    });
  }
  if (query) {
    return translate("frontDesk.empty.search", { query });
  }
  if (names.length === 1 && input.registrationFilter === "notRegistered") {
    return peopleSentence(
      "frontDesk.empty.notRegistered",
      input.registeredCount,
      translate,
    );
  }
  if (names.length === 1 && input.registrationFilter === "registered") {
    return peopleSentence(
      "frontDesk.empty.registered",
      input.notRegisteredCount,
      translate,
    );
  }
  if (names.length > 0) {
    return translate("frontDesk.empty.named", {
      filter: names.join(", "),
    });
  }
  return translate("frontDesk.empty.everyone");
}

function peopleSentence(
  baseKey: string,
  count: number,
  translate: Translate,
): string {
  if (count === 1) {
    return translate(`${baseKey}.one`);
  }
  return translate(`${baseKey}.other`, { count: formatNumber(count) });
}
