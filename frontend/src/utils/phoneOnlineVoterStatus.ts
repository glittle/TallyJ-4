import type { PersonPhoneOnlineVoterDto } from "@/types/Person";

export type PhoneOnlineVoterAuthState =
  | "neverSeen"
  | "notYetUsedForAuth"
  | "firstRegistered";

export type PhoneOnlineVoterSmsState = "unchecked" | "ok" | "blocked";

/** P row missing → never seen; P row with null WhenRegistered → not yet used for auth. */
export function phoneOnlineVoterAuthState(
  status: PersonPhoneOnlineVoterDto,
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
  if (smsStatus == null) {
    return "unchecked";
  }
  if (smsStatus === "OK") {
    return "ok";
  }
  return "blocked";
}
