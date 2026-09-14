import { describe, expect, it } from "vitest";
import type { PersonPhoneOnlineVoterDto } from "@/types/Person";
import {
  phoneOnlineVoterAuthState,
  phoneOnlineVoterSmsState,
  phoneOnlineVoterWhatsAppState,
  phoneSmsListHint,
  phoneSmsListLabel,
  phoneWhatsAppListHint,
  phoneWhatsAppListLabel,
} from "../phoneOnlineVoterStatus";

function status(
  overrides: Partial<PersonPhoneOnlineVoterDto> = {},
): PersonPhoneOnlineVoterDto {
  return {
    hasPhoneRow: false,
    whenRegistered: null,
    whenLastLogin: null,
    smsStatus: null,
    ...overrides,
  };
}

describe("phoneOnlineVoterAuthState", () => {
  it("is neverSeen when there is no matching P row", () => {
    expect(phoneOnlineVoterAuthState(status({ hasPhoneRow: false }))).toBe(
      "neverSeen",
    );
  });

  it("is notYetUsedForAuth when a P row exists and WhenRegistered is null", () => {
    expect(
      phoneOnlineVoterAuthState(
        status({ hasPhoneRow: true, whenRegistered: null }),
      ),
    ).toBe("notYetUsedForAuth");
  });

  it("is firstRegistered when WhenRegistered is set", () => {
    expect(
      phoneOnlineVoterAuthState(
        status({
          hasPhoneRow: true,
          whenRegistered: "2026-04-01T12:00:00Z",
        }),
      ),
    ).toBe("firstRegistered");
  });
});

describe("phoneOnlineVoterSmsState", () => {
  it("is unchecked when SmsStatus is null", () => {
    expect(phoneOnlineVoterSmsState(null)).toBe("unchecked");
  });

  it("is ok when SmsStatus is OK", () => {
    expect(phoneOnlineVoterSmsState("OK")).toBe("ok");
  });

  it("is blocked for any other stored reason", () => {
    expect(phoneOnlineVoterSmsState("landline")).toBe("blocked");
  });
});

describe("phoneOnlineVoterWhatsAppState", () => {
  it("is unchecked when WhatsAppStatus is null", () => {
    expect(phoneOnlineVoterWhatsAppState(null)).toBe("unchecked");
  });

  it("is ok when WhatsAppStatus is OK", () => {
    expect(phoneOnlineVoterWhatsAppState("OK")).toBe("ok");
  });

  it("treats any other stored reason as not OK", () => {
    expect(phoneOnlineVoterWhatsAppState("no-wa")).toBe("blocked");
    expect(phoneOnlineVoterWhatsAppState("check-failed")).toBe("blocked");
  });
});

describe("phoneSmsListHint", () => {
  it("is none when there is no phone block", () => {
    expect(phoneSmsListHint(null)).toBe("none");
    expect(phoneSmsListHint(undefined)).toBe("none");
  });

  it("is neverSeen when a phone has no matching P row", () => {
    expect(phoneSmsListHint(status({ hasPhoneRow: false }))).toBe("neverSeen");
  });

  it("is imported when a P row exists and has not been used for auth", () => {
    expect(
      phoneSmsListHint(status({ hasPhoneRow: true, whenRegistered: null })),
    ).toBe("imported");
  });

  it("is unchecked when registered but SmsStatus is still null", () => {
    expect(
      phoneSmsListHint(
        status({
          hasPhoneRow: true,
          whenRegistered: "2026-04-01T12:00:00Z",
          smsStatus: null,
        }),
      ),
    ).toBe("unchecked");
  });

  it("is ok or blocked from SmsStatus even when the row is imported-only", () => {
    expect(
      phoneSmsListHint(
        status({ hasPhoneRow: true, whenRegistered: null, smsStatus: "OK" }),
      ),
    ).toBe("ok");
    expect(
      phoneSmsListHint(
        status({
          hasPhoneRow: true,
          whenRegistered: null,
          smsStatus: "landline",
        }),
      ),
    ).toBe("blocked");
  });

  it("does not treat a missing P row as the occupant's block reason", () => {
    // Backend omits a non-P occupant's SmsStatus; the list contract is never-seen.
    expect(
      phoneSmsListHint(
        status({ hasPhoneRow: false, smsStatus: null, whenRegistered: null }),
      ),
    ).toBe("neverSeen");
    expect(
      phoneSmsListLabel(
        status({ hasPhoneRow: false, smsStatus: null }),
        (key) => key,
      ),
    ).toBe("people.phoneOnlineVoter.neverSeen");
  });

  it("stays on SmsStatus when WhatsAppStatus is a different reason", () => {
    expect(
      phoneSmsListHint(
        status({
          hasPhoneRow: true,
          whenRegistered: null,
          smsStatus: "OK",
          whatsAppStatus: "no-wa",
        }),
      ),
    ).toBe("ok");
    expect(
      phoneSmsListLabel(
        status({
          hasPhoneRow: true,
          whenRegistered: null,
          smsStatus: "landline",
          whatsAppStatus: "OK",
        }),
        (key) => key,
      ),
    ).toBe("people.phoneOnlineVoter.smsBlocked");
  });
});

describe("phoneWhatsAppListHint", () => {
  it("is none when there is no phone block", () => {
    expect(phoneWhatsAppListHint(null)).toBe("none");
    expect(phoneWhatsAppListHint(undefined)).toBe("none");
  });

  it("is neverSeen when a phone has no matching P row", () => {
    expect(phoneWhatsAppListHint(status({ hasPhoneRow: false }))).toBe(
      "neverSeen",
    );
  });

  it("is imported when a P row exists and has not been used for auth", () => {
    expect(
      phoneWhatsAppListHint(
        status({ hasPhoneRow: true, whenRegistered: null }),
      ),
    ).toBe("imported");
  });

  it("is unchecked when registered but WhatsAppStatus is still null", () => {
    expect(
      phoneWhatsAppListHint(
        status({
          hasPhoneRow: true,
          whenRegistered: "2026-04-01T12:00:00Z",
          whatsAppStatus: null,
        }),
      ),
    ).toBe("unchecked");
  });

  it("is ok or reason from WhatsAppStatus even when the row is imported-only", () => {
    expect(
      phoneWhatsAppListHint(
        status({
          hasPhoneRow: true,
          whenRegistered: null,
          whatsAppStatus: "OK",
        }),
      ),
    ).toBe("ok");
    expect(
      phoneWhatsAppListHint(
        status({
          hasPhoneRow: true,
          whenRegistered: null,
          whatsAppStatus: "no-wa",
        }),
      ),
    ).toBe("blocked");
    expect(
      phoneWhatsAppListLabel(
        status({
          hasPhoneRow: true,
          whenRegistered: null,
          whatsAppStatus: "check-failed",
        }),
        (key, params) => `${key}:${params?.reason ?? ""}`,
      ),
    ).toBe("people.phoneOnlineVoter.whatsAppReason:check-failed");
  });

  it("does not treat a missing P row as the occupant's WhatsApp status", () => {
    expect(
      phoneWhatsAppListHint(
        status({
          hasPhoneRow: false,
          whatsAppStatus: null,
          whenRegistered: null,
        }),
      ),
    ).toBe("neverSeen");
    expect(
      phoneWhatsAppListLabel(
        status({ hasPhoneRow: false, whatsAppStatus: null }),
        (key) => key,
      ),
    ).toBe("people.phoneOnlineVoter.neverSeen");
  });

  it("does not follow SmsStatus when WhatsAppStatus is unset", () => {
    expect(
      phoneWhatsAppListHint(
        status({
          hasPhoneRow: true,
          whenRegistered: null,
          smsStatus: "OK",
          whatsAppStatus: null,
        }),
      ),
    ).toBe("imported");
  });
});
