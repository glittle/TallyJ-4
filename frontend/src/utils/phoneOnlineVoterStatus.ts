import type { PersonPhoneSmsHintDto } from "@/types/Person";

export type PhoneOnlineVoterAuthState =
  | "neverSeen"
  | "notYetUsedForAuth"
  | "firstRegistered";

export type PhoneOnlineVoterSmsState = "unchecked" | "ok" | "blocked";

/** Same vocabulary as SMS: null / OK / other short reason. */
export type PhoneOnlineVoterWhatsAppState = PhoneOnlineVoterSmsState;

/** Compact list/Front Desk cell: channel status first, then never-seen / imported / unchecked. */
export type PhoneSmsListHint =
  | "none"
  | "neverSeen"
  | "imported"
  | "unchecked"
  | "ok"
  | "blocked";

/** Same compact states as SMS; reason codes use the stored WhatsAppStatus string. */
export type PhoneWhatsAppListHint = PhoneSmsListHint;

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

/** null WhatsAppStatus → unchecked; "OK" → ok; any other stored value → reason. */
export function phoneOnlineVoterWhatsAppState(
  whatsAppStatus: string | null | undefined,
): PhoneOnlineVoterWhatsAppState {
  return phoneOnlineVoterSmsState(whatsAppStatus);
}

/**
 * Compact people-list / Front Desk hint for one stored channel status.
 * OK / reason win over imported vs never-seen. No phone → none (caller hides the cell).
 */
function phoneChannelListHint(
  status: PersonPhoneSmsHintDto | null | undefined,
  channelStatus: string | null | undefined,
): PhoneSmsListHint {
  if (!status) {
    return "none";
  }
  const channel = phoneOnlineVoterSmsState(channelStatus);
  if (channel === "ok") {
    return "ok";
  }
  if (channel === "blocked") {
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

/**
 * Compact people-list / Front Desk SMS hint.
 * Blocked / OK win over imported vs never-seen. No phone → none (caller hides the cell).
 */
export function phoneSmsListHint(
  status: PersonPhoneSmsHintDto | null | undefined,
): PhoneSmsListHint {
  return phoneChannelListHint(status, status?.smsStatus);
}

/**
 * Compact people-list / Front Desk WhatsApp hint. Independent of SmsStatus.
 * OK / reason (no-wa, check-failed, …) win over imported vs never-seen.
 */
export function phoneWhatsAppListHint(
  status: PersonPhoneSmsHintDto | null | undefined,
): PhoneWhatsAppListHint {
  return phoneChannelListHint(status, status?.whatsAppStatus);
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

export function phoneWhatsAppListTagType(
  hint: PhoneWhatsAppListHint,
): "info" | "success" | "danger" | undefined {
  return phoneSmsListTagType(hint);
}

export function phoneWhatsAppListLabel(
  status: PersonPhoneSmsHintDto | null | undefined,
  t: (key: string, params?: Record<string, unknown>) => string,
): string {
  const hint = phoneWhatsAppListHint(status);
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
    return t("people.phoneOnlineVoter.whatsAppUnchecked");
  }
  if (hint === "ok") {
    return t("people.phoneOnlineVoter.whatsAppOk");
  }
  return t("people.phoneOnlineVoter.whatsAppReason", {
    reason: status?.whatsAppStatus,
  });
}
