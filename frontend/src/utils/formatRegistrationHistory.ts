import type { RegistrationHistoryEntryDto } from "@/types/FrontDesk";

export interface FormatRegistrationHistoryOptions {
  t: (key: string, params?: Record<string, unknown>) => string;
  getVotingMethodLabel?: (method?: string) => string;
}

export function formatRegistrationHistoryDetails(
  entry: RegistrationHistoryEntryDto,
  options: FormatRegistrationHistoryOptions,
): string {
  const { t, getVotingMethodLabel } = options;
  const items: string[] = [];

  if (entry.action === "CheckedIn") {
    const method = getVotingMethodLabel
      ? getVotingMethodLabel(entry.votingMethod)
      : (entry.votingMethod ?? t("frontDesk.common.dash"));
    items.push(t("frontDesk.history.checkedIn", { method }));
  } else if (entry.action) {
    items.push(entry.action);
  }

  if (entry.teller1) {
    items.push(t("frontDesk.history.teller1", { name: entry.teller1 }));
  }
  if (entry.teller2) {
    items.push(t("frontDesk.history.teller2", { name: entry.teller2 }));
  }
  if (entry.locationName) {
    items.push(t("frontDesk.history.location", { name: entry.locationName }));
  }
  if (entry.envNum) {
    items.push(t("frontDesk.history.envelope", { num: entry.envNum }));
  }

  if (entry.performedBy?.trim()) {
    items.push(
      t("frontDesk.history.reason", { reason: entry.performedBy.trim() }),
    );
  }

  return items.join(", ");
}

export function sortRegistrationHistoryNewestFirst(
  entries: RegistrationHistoryEntryDto[],
): RegistrationHistoryEntryDto[] {
  return [...entries].sort((a, b) => {
    const aTime = a.timestamp ? new Date(a.timestamp).getTime() : 0;
    const bTime = b.timestamp ? new Date(b.timestamp).getTime() : 0;
    return bTime - aTime;
  });
}

export function formatRegistrationHistoryTime(
  timestamp?: string | Date | null,
): string {
  if (!timestamp) {
    return "";
  }
  return new Date(timestamp).toLocaleString();
}
