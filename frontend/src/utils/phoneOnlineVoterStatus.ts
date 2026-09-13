import type { PersonPhoneSmsHintDto } from "@/types/Person";

export type PhoneOnlineVoterAuthState =
  | "neverSeen"
  | "notYetUsedForAuth"
  | "firstRegistered";

export type PhoneOnlineVoterSmsState = "unchecked" | "ok" | "blocked";

/** Compact list/Front Desk cell: SMS first, then never-seen / imported / unchecked. */
export type PhoneSmsListHint =
  | "none"
  | "neverSeen"
  | "imported"
  | "unchecked"
  | "ok"
  | "blocked";

/** P row missing → never seen; P row with null WhenRegistered → not yet used for auth. */
export function phoneOnlineVoterAuthState(
  status: PersonPhoneSmsHintDto,
): PhoneOnlineVoterAuthState {
  if (!status.hasPhoneRow) {
    return "neverSeen";
  }
  if (!status.whenRegistered) {
    return "notYetUsedForAuth";
  }
  return "firstRegistered";
}

/** null SmsStatus → unchecked; "OK" → ok; any other stored value → blocked (reason is that value). */
export function phoneOnlineVoterSmsState(
  smsStatus: string | null | undefined,
): PhoneOnlineVoterSmsState {
  if (smsStatus === null || smsStatus === undefined) {
    return "unchecked";
  }
  if (smsStatus === "OK") {
    return "ok";
  }
  return "blocked";
}

/**
 * Compact people-list / Front Desk hint.
 * Blocked / OK win over imported vs never-seen. No phone → none (caller hides the cell).
 */
export function phoneSmsListHint(
  status: PersonPhoneSmsHintDto | null | undefined,
): PhoneSmsListHint {
  if (!status) {
    return "none";
  }
  const sms = phoneOnlineVoterSmsState(status.smsStatus);
  if (sms === "ok") {
    return "ok";
  }
  if (sms === "blocked") {
    return "blocked";
  }
  if (!status.hasPhoneRow) {
    return "neverSeen";
  }
  if (!status.whenRegistered) {
    return "imported";
  }
  return "unchecked";
}

export function phoneSmsListTagType(
  hint: PhoneSmsListHint,
): "info" | "success" | "danger" | undefined {
  if (hint === "ok") {
    return "success";
  }
  if (hint === "blocked") {
    return "danger";
  }
  if (hint === "none") {
    return undefined;
  }
  return "info";
}

export function phoneSmsListLabel(
  status: PersonPhoneSmsHintDto | null | undefined,
  t: (key: string, params?: Record<string, unknown>) => string,
): string {
  const hint = phoneSmsListHint(status);
  if (hint === "none") {
    return "";
  }
  if (hint === "neverSeen") {
    return t("people.phoneOnlineVoter.neverSeen");
  }
  if (hint === "imported") {
    return t("people.phoneOnlineVoter.imported");
  }
  if (hint === "unchecked") {
    return t("people.phoneOnlineVoter.smsUnchecked");
  }
  if (hint === "ok") {
    return t("people.phoneOnlineVoter.smsOk");
  }
  return t("people.phoneOnlineVoter.smsBlocked", {
    reason: status?.smsStatus,
  });
}
