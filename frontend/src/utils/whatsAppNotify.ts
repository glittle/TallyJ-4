import {
  canCheckSelectedWhatsApp,
  MAX_WHATSAPP_CHECK_SELECTED,
} from "./whatsAppCheckSelected";

/** Same bound as StartWhatsAppNotifyDto.MaxSelectedPeople. */
export const MAX_WHATSAPP_NOTIFY_SELECTED = MAX_WHATSAPP_CHECK_SELECTED;

export function canNotifySelectedWhatsApp(
  selectedCount: number,
  selectedWithPhoneCount: number,
): boolean {
  return canCheckSelectedWhatsApp(selectedCount, selectedWithPhoneCount);
}

export function whatsAppNotifyOutcomeLabel(
  outcome: string,
  t: (key: string) => string,
): string {
  return t(`people.notifyWhatsAppOutcome.${outcome}`);
}

export function notifyCancelledCount(
  results: { outcome: string }[] | undefined,
): number {
  return (results ?? []).filter((row) => row.outcome === "cancelled").length;
}
