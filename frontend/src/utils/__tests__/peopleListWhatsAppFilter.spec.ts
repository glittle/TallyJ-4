import { describe, expect, it } from "vitest";
import type { PersonListDto, PersonPhoneSmsHintDto } from "@/types/Person";
import { phoneSmsListHint } from "@/utils/phoneOnlineVoterStatus";
import {
  PEOPLE_LIST_WHATSAPP_FILTER_ALL,
  PEOPLE_LIST_WHATSAPP_FILTER_HAS,
  PEOPLE_LIST_WHATSAPP_FILTER_NO,
  PEOPLE_LIST_WHATSAPP_FILTER_UNCHECKED,
  filterPeopleList,
  personHasNoWhatsApp,
  personHasWhatsApp,
  personMatchesWhatsAppFilter,
  personWhatsAppUnchecked,
} from "../peopleListWhatsAppFilter";

function hint(
  overrides: Partial<PersonPhoneSmsHintDto> = {},
): PersonPhoneSmsHintDto {
  return {
    hasPhoneRow: true,
    whenRegistered: null,
    smsStatus: null,
    whatsAppStatus: null,
    ...overrides,
  };
}

function person(
  overrides: Partial<PersonListDto> & { personGuid: string },
): PersonListDto {
  return {
    fullName: overrides.fullName ?? overrides.personGuid,
    email: `${overrides.personGuid}@example.com`,
    phone: "555-0100",
    ...overrides,
  };
}

const ok = person({
  personGuid: "ok",
  fullName: "Ada Lovelace",
  phoneOnlineVoter: hint({ whatsAppStatus: "OK" }),
});
const unchecked = person({
  personGuid: "unchecked",
  fullName: "Alan Turing",
  phoneOnlineVoter: hint({
    whenRegistered: "2026-04-01T12:00:00Z",
    whatsAppStatus: null,
  }),
});
const imported = person({
  personGuid: "imported",
  phoneOnlineVoter: hint({ whenRegistered: null, whatsAppStatus: null }),
});
const noWa = person({
  personGuid: "no-wa",
  phoneOnlineVoter: hint({ whatsAppStatus: "no-wa" }),
});
const checkFailed = person({
  personGuid: "check-failed",
  phoneOnlineVoter: hint({ whatsAppStatus: "check-failed" }),
});
const nonP = person({
  personGuid: "non-p",
  phoneOnlineVoter: hint({
    hasPhoneRow: false,
    whatsAppStatus: null,
    smsStatus: null,
  }),
});
const noPhone = person({
  personGuid: "no-phone",
  phone: undefined,
  phoneOnlineVoter: null,
});
const smsOkWhatsAppNo = person({
  personGuid: "sms-ok-wa-no",
  phoneOnlineVoter: hint({ smsStatus: "OK", whatsAppStatus: "no-wa" }),
});
const smsBlockedWhatsAppOk = person({
  personGuid: "sms-blocked-wa-ok",
  phoneOnlineVoter: hint({ smsStatus: "landline", whatsAppStatus: "OK" }),
});

const roll = [
  ok,
  unchecked,
  imported,
  noWa,
  checkFailed,
  nonP,
  noPhone,
  smsOkWhatsAppNo,
  smsBlockedWhatsAppOk,
];

describe("peopleListWhatsAppFilter", () => {
  it("includes only P-row OK phones in Has WhatsApp", () => {
    const visible = filterPeopleList(
      roll,
      "",
      PEOPLE_LIST_WHATSAPP_FILTER_HAS,
    ).map((row) => row.personGuid);

    expect(visible).toEqual(["ok", "sms-blocked-wa-ok"]);
    expect(personHasWhatsApp(ok)).toBe(true);
    expect(personHasWhatsApp(smsBlockedWhatsAppOk)).toBe(true);
    expect(personHasWhatsApp(unchecked)).toBe(false);
    expect(personHasWhatsApp(imported)).toBe(false);
    expect(personHasWhatsApp(noWa)).toBe(false);
    expect(personHasWhatsApp(checkFailed)).toBe(false);
    expect(personHasWhatsApp(nonP)).toBe(false);
    expect(personHasWhatsApp(noPhone)).toBe(false);
    expect(personHasWhatsApp(smsOkWhatsAppNo)).toBe(false);
  });

  it("does not treat unchecked, no-wa, non-P, or no-phone as Has WhatsApp", () => {
    for (const row of [unchecked, imported, noWa, nonP, noPhone]) {
      expect(
        personMatchesWhatsAppFilter(row, PEOPLE_LIST_WHATSAPP_FILTER_HAS),
      ).toBe(false);
    }
  });

  it("does not treat a non-P occupant as Has WhatsApp even if a status leaked", () => {
    const leaked = person({
      personGuid: "leaked-non-p",
      phoneOnlineVoter: hint({
        hasPhoneRow: false,
        whatsAppStatus: "OK",
      }),
    });
    expect(personHasWhatsApp(leaked)).toBe(false);
    expect(personHasNoWhatsApp(leaked)).toBe(false);
    expect(personWhatsAppUnchecked(leaked)).toBe(false);
  });

  it("groups imported and registered-null status as Unchecked", () => {
    const visible = filterPeopleList(
      roll,
      "",
      PEOPLE_LIST_WHATSAPP_FILTER_UNCHECKED,
    ).map((row) => row.personGuid);

    expect(visible).toEqual(["unchecked", "imported"]);
    expect(personWhatsAppUnchecked(unchecked)).toBe(true);
    expect(personWhatsAppUnchecked(imported)).toBe(true);
    expect(personWhatsAppUnchecked(ok)).toBe(false);
    expect(personWhatsAppUnchecked(noWa)).toBe(false);
    expect(personWhatsAppUnchecked(nonP)).toBe(false);
    expect(personWhatsAppUnchecked(noPhone)).toBe(false);
  });

  it("includes only stored no-wa in No WhatsApp", () => {
    const visible = filterPeopleList(
      roll,
      "",
      PEOPLE_LIST_WHATSAPP_FILTER_NO,
    ).map((row) => row.personGuid);

    expect(visible).toEqual(["no-wa", "sms-ok-wa-no"]);
    expect(personHasNoWhatsApp(noWa)).toBe(true);
    expect(personHasNoWhatsApp(checkFailed)).toBe(false);
    expect(personHasNoWhatsApp(ok)).toBe(false);
    expect(personHasNoWhatsApp(nonP)).toBe(false);
    expect(personHasNoWhatsApp(noPhone)).toBe(false);
  });

  it("leaves All unfiltered and still applies name search", () => {
    expect(
      filterPeopleList(roll, "", PEOPLE_LIST_WHATSAPP_FILTER_ALL).map(
        (row) => row.personGuid,
      ),
    ).toEqual(roll.map((row) => row.personGuid));

    expect(
      filterPeopleList(roll, "ada", PEOPLE_LIST_WHATSAPP_FILTER_HAS).map(
        (row) => row.personGuid,
      ),
    ).toEqual(["ok"]);
    expect(
      filterPeopleList(roll, "ada", PEOPLE_LIST_WHATSAPP_FILTER_ALL).map(
        (row) => row.personGuid,
      ),
    ).toEqual(["ok"]);
  });

  it("does not use SmsStatus for the WhatsApp filter", () => {
    expect(phoneSmsListHint(smsOkWhatsAppNo.phoneOnlineVoter)).toBe("ok");
    expect(phoneSmsListHint(smsBlockedWhatsAppOk.phoneOnlineVoter)).toBe(
      "blocked",
    );
    expect(personHasWhatsApp(smsOkWhatsAppNo)).toBe(false);
    expect(personHasWhatsApp(smsBlockedWhatsAppOk)).toBe(true);
  });
});
