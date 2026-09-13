import { describe, expect, it } from "vitest";
import type { PersonListDto } from "@/types/Person";
import {
  canCheckSelectedWhatsApp,
  isWhatsAppCheckAbortError,
  MAX_WHATSAPP_CHECK_SELECTED,
  nextSelectedGuidsAfterWhatsAppCheck,
  selectedPeopleWithPhone,
  whatsAppCheckOutcomeLabel,
} from "../whatsAppCheckSelected";

const withPhone: PersonListDto = {
  personGuid: "p-1",
  fullName: "Pat Smith",
  phone: "+14168972671",
};

const noPhone: PersonListDto = {
  personGuid: "p-2",
  fullName: "No Phone",
};

describe("whatsAppCheckSelected", () => {
  it("selects only chosen people who have a phone", () => {
    expect(
      selectedPeopleWithPhone(
        [withPhone, noPhone],
        [withPhone.personGuid, noPhone.personGuid],
      ),
    ).toEqual([withPhone]);
  });

  it("enables check when some selected people have phones and the list is in bound", () => {
    expect(canCheckSelectedWhatsApp(2, 1)).toBe(true);
    expect(canCheckSelectedWhatsApp(0, 0)).toBe(false);
    expect(canCheckSelectedWhatsApp(1, 0)).toBe(false);
    expect(canCheckSelectedWhatsApp(MAX_WHATSAPP_CHECK_SELECTED + 1, 1)).toBe(
      false,
    );
  });

  it("maps outcomes to i18n keys", () => {
    expect(whatsAppCheckOutcomeLabel("OK", (key) => key)).toBe(
      "people.checkWhatsAppSelectedOutcome.OK",
    );
    expect(whatsAppCheckOutcomeLabel("skipped-no-phone", (key) => key)).toBe(
      "people.checkWhatsAppSelectedOutcome.skipped-no-phone",
    );
  });

  it("treats AbortError as the UI cancel path, not result.cancelled", () => {
    expect(
      isWhatsAppCheckAbortError(new DOMException("Aborted", "AbortError")),
    ).toBe(true);
    expect(isWhatsAppCheckAbortError({ name: "AbortError" })).toBe(true);
    expect(isWhatsAppCheckAbortError({ cancelled: true })).toBe(false);
  });

  it("clears selection after a completed check or AbortError cancel", () => {
    const selected = ["p-1", "p-2"];
    expect(nextSelectedGuidsAfterWhatsAppCheck(selected, "completed")).toEqual(
      [],
    );
    expect(nextSelectedGuidsAfterWhatsAppCheck(selected, "aborted")).toEqual(
      [],
    );
    expect(nextSelectedGuidsAfterWhatsAppCheck(selected, "failed")).toEqual(
      selected,
    );
  });
});
