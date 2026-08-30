import { describe, expect, it } from "vitest";
import type { PersonPhoneOnlineVoterDto } from "@/types/Person";
import {
  phoneOnlineVoterAuthState,
  phoneOnlineVoterSmsState,
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
